using System;
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

        public async Task EnsureEditableAsync(Guid requestId)
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

            if (string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This Purchase Request is currently in review. Return it for revision before modifying it.");
        }

        public void EnsureEditable(Guid requestId)
        {
            var pr = _db.Requests.AsNoTracking().FirstOrDefault(x => x.Id == requestId);
            if (pr == null) throw new InvalidOperationException("Purchase Request not found.");
            var dependency = Task.Run(() => GetDependencyMessageAsync(requestId)).GetAwaiter().GetResult();
            if (dependency != null) throw new InvalidOperationException(dependency);
            if (pr.PostedDt.HasValue || !string.IsNullOrWhiteSpace(pr.PostedBy))
                throw new InvalidOperationException("Unpost this Purchase Request before editing or deleting it.");

            var historyStatus = _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == requestId)
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus)
                .FirstOrDefault();

            if (string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This Purchase Request is currently in review. Return it for revision before modifying it.");
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

                // Return to Submitted / For Review state
                new DocumentHistoryService(_db).AddStatusHistory(
                    DocumentTypes.PurchaseRequest,
                    pr.Id,
                    prDocNo,
                    PrStatuses.Posted,
                    PrStatuses.Submitted,
                    "Unpost",
                    "Purchase Request unposted and returned to review stage.",
                    user);

                await _db.SaveChangesAsync();
                transaction.Commit();
                return pr;
            }
        }
    }
}
