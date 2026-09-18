using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Ai.Services;
using iLgs.Models;

namespace iLgs.Services.PurchaseRequest
{
    public class PurchaseRequestLifecycleService
    {
        private readonly AppManEntities _db;

        public PurchaseRequestLifecycleService(AppManEntities db)
        {
            _db = db;
        }

        public async Task<string> GetDependencyMessageAsync(Guid requestId)
        {
            var pr = await _db.Requests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == requestId);
            var prNo = (pr != null && !string.IsNullOrWhiteSpace(pr.PrNo))
                ? pr.PrNo.Trim()
                : ((pr != null && !string.IsNullOrWhiteSpace(pr.CtrlNo)) ? pr.CtrlNo.Trim() : "N/A");

            // Inspect all request items and their OrderItemRequest allocations
            var itemAllocations = await (
                from ri in _db.RequestItems
                where ri.PrId == requestId
                from oir in _db.OrderItemRequests
                where oir.RequestItemId == ri.Id
                from oi in _db.OrderItems
                where oi.Id == oir.OrderItemId
                from o in _db.Orders
                where o.Id == oi.OrderId
                select new
                {
                    OrderId = o.Id,
                    PoNo = o.PoNo,
                    CtrlNo = o.CtrlNo,
                    PostedDt = o.PostedDt,
                    PostedBy = o.PostedBy
                }
            ).ToListAsync();

            // Also inspect header-level OrderRequest allocations
            var headerAllocations = await (
                from or in _db.OrderRequests
                where or.PrId == requestId
                from o in _db.Orders
                where o.Id == or.OrderId
                select new
                {
                    OrderId = o.Id,
                    PoNo = o.PoNo,
                    CtrlNo = o.CtrlNo,
                    PostedDt = o.PostedDt,
                    PostedBy = o.PostedBy
                }
            ).ToListAsync();

            var allOrders = itemAllocations
                .Concat(headerAllocations)
                .GroupBy(x => x.OrderId)
                .Select(g => g.First())
                .ToList();

            if (!allOrders.Any())
            {
                return null;
            }

            // Check if any referenced PO is posted
            var postedOrders = allOrders
                .Where(o => o.PostedDt.HasValue || !string.IsNullOrWhiteSpace(o.PostedBy))
                .ToList();

            if (postedOrders.Any())
            {
                var poNos = string.Join(", ", postedOrders
                    .Select(o => !string.IsNullOrWhiteSpace(o.PoNo) ? o.PoNo.Trim() : (!string.IsNullOrWhiteSpace(o.CtrlNo) ? o.CtrlNo.Trim() : "Unnumbered PO"))
                    .Distinct());

                return string.Format("Cannot unpost Purchase Request. PR No. {0} contains item(s) already included in posted Purchase Order {1}. Resolve the dependent Purchase Order first.", prNo, poNos);
            }

            // Only draft / unposted PO dependencies exist
            var draftOrders = allOrders
                .Where(o => !o.PostedDt.HasValue && string.IsNullOrWhiteSpace(o.PostedBy))
                .ToList();

            if (draftOrders.Any())
            {
                var poNos = string.Join(", ", draftOrders
                    .Select(o => !string.IsNullOrWhiteSpace(o.PoNo) ? o.PoNo.Trim() : (!string.IsNullOrWhiteSpace(o.CtrlNo) ? o.CtrlNo.Trim() : "Unnumbered PO"))
                    .Distinct());

                return string.Format("Cannot unpost Purchase Request. PR No. {0} still has item allocation(s) in Purchase Order {1}. Remove or resolve the dependent Purchase Order allocation first.", prNo, poNos);
            }

            return null;
        }

        public async Task EnsureEditableAsync(Guid requestId, bool allowAdminEdit = false)
        {
            var pr = await _db.Requests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == requestId);
            if (pr == null) throw new InvalidOperationException("Purchase Request not found.");
            var dependency = await GetDependencyMessageAsync(requestId);
            if (dependency != null) throw new InvalidOperationException(dependency);
            if (pr.PostedDt.HasValue || !string.IsNullOrWhiteSpace(pr.PostedBy))
                throw new InvalidOperationException("Unpost this Purchase Request before editing or deleting it.");

            var historyStatus = await _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId)
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus)
                .FirstOrDefaultAsync();

            if (allowAdminEdit)
            {
                if (!string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(historyStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Admin edit is only permitted for Submitted or Unposted Purchase Requests.");
                }
            }
            else
            {
                if (string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This Purchase Request is currently in review. Return it for revision before modifying it.");
                if (string.Equals(historyStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("This Purchase Request is unposted. Return it for revision or edit via posting administration.");
            }
        }

        public async Task<Request> LockAsync(Guid id)
        {
            var ids = await _db.Database.SqlQuery<Guid>(
                "SELECT Id FROM dbo.Requests WITH (UPDLOCK, HOLDLOCK) WHERE Id = @p0", id).ToListAsync();
            if (ids.Count == 0) throw new InvalidOperationException("Purchase Request not found.");
            var pr = await _db.Requests.FindAsync(id);
            await _db.Entry(pr).ReloadAsync();
            return pr;
        }

        private bool IsDuplicateKeyException(Exception ex)
        {
            while (ex != null)
            {
                var sqlEx = ex as System.Data.SqlClient.SqlException;
                if (sqlEx != null)
                {
                    if (sqlEx.Number == 2601 || sqlEx.Number == 2627)
                    {
                        return true;
                    }
                }
                ex = ex.InnerException;
            }
            return false;
        }

        public async Task<Request> PostAsync(Guid id, string prNumber, DateTime prDate, string user, DateTime date)
        {
            if (prDate == default(DateTime))
            {
                throw new InvalidOperationException("PR Date is required.");
            }
            if (prDate.Date > DateTime.Today)
            {
                throw new InvalidOperationException("Future date is not allowed.");
            }

            var isManualPrNumber = !string.IsNullOrWhiteSpace(prNumber);
            var manualPrNumber = isManualPrNumber ? prNumber.Trim() : null;

            var requestService = new RequestService(_db);

            if (isManualPrNumber)
            {
                var validation = requestService.ValidatePrNoAndDate(manualPrNumber, prDate);
                if (validation != null && validation.Count > 0)
                {
                    var errorList = new List<string>();
                    foreach (DictionaryEntry entry in validation)
                    {
                        var msgs = entry.Value as IEnumerable<string>;
                        if (msgs != null) errorList.AddRange(msgs);
                        else if (entry.Value != null) errorList.Add(entry.Value.ToString());
                    }
                    if (errorList.Any())
                    {
                        throw new InvalidOperationException(string.Join(" ", errorList));
                    }
                }
            }

            int maxAttempts = isManualPrNumber ? 1 : 3;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
                {
                    var pr = await LockAsync(id);

                    var isPosted = pr.PostedDt.HasValue || !string.IsNullOrWhiteSpace(pr.PostedBy);
                    var historyStatus = await _db.DocumentStatusHistories
                        .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == id)
                        .OrderByDescending(h => h.ChangedDt).ThenByDescending(h => h.Id)
                        .Select(h => h.ToStatus).FirstOrDefaultAsync();

                    var isPostable = string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) ||
                                     string.Equals(historyStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase);

                    if (isPosted || !isPostable)
                    {
                        throw new InvalidOperationException("Only a Submitted or Unposted Purchase Request can be posted. Another reviewer may have already updated this record.");
                    }

                    if (string.IsNullOrWhiteSpace(pr.Availability) || string.IsNullOrWhiteSpace(pr.ApprovedBy))
                    {
                        throw new InvalidOperationException("Cash availability and Approved By are required before posting.");
                    }

                    string resolvedPrNumber;
                    DateTime resolvedPrDate;

                    if (isManualPrNumber)
                    {
                        if (await _db.Requests.AnyAsync(x => x.Id != pr.Id && x.PrNo == manualPrNumber))
                        {
                            throw new InvalidOperationException("PR Number already exists.");
                        }
                        resolvedPrNumber = manualPrNumber;
                        resolvedPrDate = prDate.Date;
                    }
                    else if (!string.IsNullOrWhiteSpace(pr.PrNo))
                    {
                        // Reposting an unposted / revised PR that already has an assigned official PR Number:
                        // Preserve the existing official PR Number and official PR Date
                        var existingPrNo = pr.PrNo.Trim();
                        if (await _db.Requests.AnyAsync(x => x.Id != pr.Id && x.PrNo == existingPrNo))
                        {
                            throw new InvalidOperationException("The existing PR Number is already used by another record.");
                        }
                        resolvedPrNumber = existingPrNo;
                        resolvedPrDate = pr.PrDate.HasValue ? pr.PrDate.Value.Date : prDate.Date;
                    }
                    else
                    {
                        // PR has never received an official PR Number:
                        // Acquire app lock for serialization:
                        try
                        {
                            await _db.Database.ExecuteSqlCommandAsync("EXEC sp_getapplock @Resource = 'PR_NUMBER_GENERATOR', @LockMode = 'Exclusive', @LockOwner = 'Transaction'");
                        }
                        catch
                        {
                            // sp_getapplock is an optimization; proceed even if permission restricted
                        }

                        var generatedPrNo = requestService.NextPrNo(prDate);
                        if (string.IsNullOrWhiteSpace(generatedPrNo))
                        {
                            throw new InvalidOperationException("Failed to generate a PR Number. Please try again.");
                        }

                        var genValidation = requestService.ValidatePrNoAndDate(generatedPrNo, prDate);
                        if (genValidation != null && genValidation.Count > 0)
                        {
                            var genErrors = new List<string>();
                            foreach (DictionaryEntry entry in genValidation)
                            {
                                var msgs = entry.Value as IEnumerable<string>;
                                if (msgs != null) genErrors.AddRange(msgs);
                                else if (entry.Value != null) genErrors.Add(entry.Value.ToString());
                            }
                            if (genErrors.Any())
                            {
                                throw new InvalidOperationException(string.Join(" ", genErrors));
                            }
                        }

                        if (await _db.Requests.AnyAsync(x => x.Id != pr.Id && x.PrNo == generatedPrNo))
                        {
                            if (attempt == maxAttempts)
                            {
                                throw new InvalidOperationException("PR Number already exists. Please try posting again.");
                            }
                            continue;
                        }

                        resolvedPrNumber = generatedPrNo;
                        resolvedPrDate = prDate.Date;
                    }

                    pr.PrNo = resolvedPrNumber;
                    pr.PrDate = resolvedPrDate;
                    pr.PostedBy = user;
                    pr.PostedDt = date;
                    pr.UpdatedBy = user;
                    pr.UpdatedDt = date;

                    var usages = await _db.PPMPItemUsages.Where(x => x.PrId == pr.Id && x.Type != "PR").ToListAsync();
                    foreach (var usage in usages)
                    {
                        usage.Type = "PR";
                        usage.Reference = resolvedPrNumber;
                    }

                    new DocumentHistoryService(_db).AddStatusHistory(
                        DocumentTypes.PurchaseRequest,
                        pr.Id,
                        resolvedPrNumber,
                        historyStatus ?? PrStatuses.Submitted,
                        PrStatuses.Posted,
                        "Post",
                        "Purchase Request reviewed and posted.",
                        user);

                    try
                    {
                        await _db.SaveChangesAsync();
                        transaction.Commit();
                        return pr;
                    }
                    catch (Exception dbEx)
                    {
                        transaction.Rollback();
                        if (IsDuplicateKeyException(dbEx))
                        {
                            if (isManualPrNumber)
                            {
                                throw new InvalidOperationException("PR Number already exists.");
                            }
                            if (attempt == maxAttempts)
                            {
                                throw new InvalidOperationException("Unable to generate a unique PR Number due to a concurrent conflict. Please try again.");
                            }
                            continue;
                        }
                        throw;
                    }
                }
            }

            throw new InvalidOperationException("Failed to post Purchase Request. Please try again.");
        }

        public async Task<Request> ReturnForRevisionAsync(Guid id, string reviewComment, string user, DateTime date)
        {
            if (string.IsNullOrWhiteSpace(reviewComment))
            {
                throw new InvalidOperationException("A review comment is required.");
            }
            reviewComment = reviewComment.Trim();

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var pr = await LockAsync(id);

                var isPosted = pr.PostedDt.HasValue || !string.IsNullOrWhiteSpace(pr.PostedBy);
                var historyStatus = await _db.DocumentStatusHistories
                    .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == id)
                    .OrderByDescending(h => h.ChangedDt).ThenByDescending(h => h.Id)
                    .Select(h => h.ToStatus).FirstOrDefaultAsync();

                var isReturnable = string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) ||
                                   string.Equals(historyStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase);

                if (isPosted || !isReturnable)
                {
                    throw new InvalidOperationException("Only a Submitted or Unposted Purchase Request can be returned for revision. Another reviewer may have already updated this record.");
                }

                var dependency = await GetDependencyMessageAsync(id);
                if (dependency != null)
                {
                    throw new InvalidOperationException(dependency);
                }

                if (await _db.RequestItems.AnyAsync(ri => ri.PrId == id && ri.OrderItemRequests.Any()))
                {
                    throw new InvalidOperationException("This Purchase Request is already used by a Purchase Order.");
                }

                pr.UpdatedBy = user;
                pr.UpdatedDt = date;

                var docNo = !string.IsNullOrWhiteSpace(pr.PrNo)
                    ? pr.PrNo.Trim()
                    : (!string.IsNullOrWhiteSpace(pr.CtrlNo) ? pr.CtrlNo.Trim() : "PR");

                new DocumentHistoryService(_db).AddStatusHistory(
                    DocumentTypes.PurchaseRequest,
                    pr.Id,
                    docNo,
                    historyStatus ?? PrStatuses.Submitted,
                    PrStatuses.Returned,
                    "Return",
                    reviewComment,
                    user);

                await _db.SaveChangesAsync();
                transaction.Commit();
                return pr;
            }
        }

        public async Task<Request> UnpostAsync(Guid id, string user, DateTime date)
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var pr = await LockAsync(id);

                var isPosted = pr.PostedDt.HasValue || !string.IsNullOrWhiteSpace(pr.PostedBy);
                var historyStatus = await _db.DocumentStatusHistories
                    .Where(h => h.DocumentType == DocumentTypes.PurchaseRequest && h.DocumentId == id)
                    .OrderByDescending(h => h.ChangedDt).ThenByDescending(h => h.Id)
                    .Select(h => h.ToStatus).FirstOrDefaultAsync();

                if (!isPosted && !string.Equals(historyStatus, PrStatuses.Posted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This Purchase Request is already unposted or not yet posted.");
                }

                var dependency = await GetDependencyMessageAsync(id);
                if (dependency != null)
                {
                    throw new InvalidOperationException(dependency);
                }

                // Clear posting state
                pr.PostedBy = null;
                pr.PostedDt = null;
                pr.UpdatedBy = user;
                pr.UpdatedDt = date;

                // Ensure submitted audit info is preserved
                if (string.IsNullOrWhiteSpace(pr.SubmittedBy))
                {
                    pr.SubmittedBy = !string.IsNullOrWhiteSpace(pr.InsertedBy) ? pr.InsertedBy : user;
                }
                if (!pr.SubmittedDt.HasValue)
                {
                    pr.SubmittedDt = pr.InsertedDt.HasValue ? pr.InsertedDt : date;
                }

                var prDocNo = (pr != null && !string.IsNullOrWhiteSpace(pr.PrNo))
                    ? pr.PrNo.Trim()
                    : ((pr != null && !string.IsNullOrWhiteSpace(pr.CtrlNo)) ? pr.CtrlNo.Trim() : "PR");

                // Transition to Unposted state
                new DocumentHistoryService(_db).AddStatusHistory(
                    DocumentTypes.PurchaseRequest,
                    pr.Id,
                    prDocNo,
                    PrStatuses.Posted,
                    PrStatuses.Unposted,
                    "Unpost",
                    "Purchase Request unposted.",
                    user);

                await _db.SaveChangesAsync();
                transaction.Commit();
                return pr;
            }
        }
    }
}
