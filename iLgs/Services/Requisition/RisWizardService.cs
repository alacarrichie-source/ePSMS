using iLgs.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Requisition
{
    public partial class RisService
    {
        // Shared by final wizard submission, repost, unpost and delete. The lock is
        // transaction-owned, including number generation and the last quantity check.
        private void LockRisTransactions()
        {
            _db.Database.ExecuteSqlCommand("DECLARE @r int; EXEC @r = sys.sp_getapplock " +
                "@Resource=N'ePSMS.RIS.Posting', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=10000; " +
                "IF @r < 0 RAISERROR('Another RIS operation is in progress. Please try again.',16,1);");
        }

        private async Task<IQueryable<OrderRequest>> AccessibleOrderRequests(string userId)
        {
            var admin = await _userService.IsAdminAsync(userId);
            return _db.OrderRequests.AsNoTracking().Where(x => admin ||
                x.Request.Codextn.DepartmentUsers.Any(u => u.UserId == userId));
        }

        private IQueryable<OrderItemRequest> SupplyAllocations(Guid orderId, Guid? prId)
        {
            return _db.OrderItemRequests.AsNoTracking().Where(x =>
                x.OrderItem.OrderId == orderId && x.RequestItem.PrId == prId &&
                x.OrderItem.ItemCode.ItemType.Category == "S");
        }

        private IQueryable<RisAllocationVM> AllocationQuery(Guid orderId, Guid? prId)
        {
            return SupplyAllocations(orderId, prId).Select(x => new RisAllocationVM
            {
                Id = x.Id, ItemNo = x.OrderItem.ItemNo, ItemCode = x.OrderItem.PpmpCode,
                Description = x.OrderItem.Description, Unit = x.OrderItem.Unit,
                AllocatedQty = x.QtyApplied ?? 0,
                PreviousQty = x.RisItems.Where(r => r.RISs.PostedDt != null).Sum(r => r.QtyIssue) ?? 0,
                RemainingQty = (x.QtyApplied ?? 0) - (x.RisItems.Where(r => r.RISs.PostedDt != null).Sum(r => r.QtyIssue) ?? 0)
            });
        }

        public async Task<List<string>> WizardDepartmentsAsync(string userId)
        {
            var sources = await AccessibleOrderRequests(userId);
            return await sources.Where(x => x.Request.Department != null && x.Request.Department != "" &&
                x.Order.PostedDt != null && x.Order.AIRs.Any(a => a.PostedDt != null) &&
                x.Order.OrderItems.Any(i => i.ItemCode.ItemType.Category == "S" &&
                    i.OrderItemRequests.Any(a => a.RequestItem.PrId == x.PrId && (a.QtyApplied ?? 0) >
                        (a.RisItems.Where(r => r.RISs.PostedDt != null).Sum(r => r.QtyIssue) ?? 0))))
                .Select(x => x.Request.Department).Distinct().OrderBy(x => x).ToListAsync();
        }

        public async Task<IQueryable<RisCandidateVM>> WizardCandidatesAsync(string department, string userId)
        {
            var sources = (await AccessibleOrderRequests(userId)).Where(x => x.Request.Department == department);
            // One grid row per PO; the selected department scopes its PR groups.
            var orderIds = sources.Select(x => x.OrderId);
            var allocations = _db.OrderItemRequests.AsNoTracking().Where(x =>
                x.OrderItem.ItemCode.ItemType.Category == "S" &&
                x.RequestItem.Request.Department == department &&
                sources.Any(s => s.OrderId == x.OrderItem.OrderId && s.PrId == x.RequestItem.PrId));
            return _db.Orders.AsNoTracking().Where(o => orderIds.Contains(o.Id) && o.PostedDt != null &&
                o.AIRs.Any(a => a.PostedDt != null) && allocations.Any(x => x.OrderItem.OrderId == o.Id &&
                    (x.QtyApplied ?? 0) > (x.RisItems.Where(r => r.RISs.PostedDt != null).Sum(r => r.QtyIssue) ?? 0)))
                .Select(o => new RisCandidateVM
                {
                    Id = o.Id, PoNo = o.PoNo, PoDate = o.PoDate, Supplier = o.SupName,
                    PrNo = o.PrNo, Department = department,
                    ItemCount = allocations.Count(x => x.OrderItem.OrderId == o.Id),
                    AllocatedQty = allocations.Where(x => x.OrderItem.OrderId == o.Id).Sum(x => (decimal?)x.QtyApplied) ?? 0,
                    PreviousQty = allocations.Where(x => x.OrderItem.OrderId == o.Id).SelectMany(x => x.RisItems)
                        .Where(r => r.RISs.PostedDt != null).Sum(r => r.QtyIssue) ?? 0,
                    RemainingQty = (allocations.Where(x => x.OrderItem.OrderId == o.Id).Sum(x => (decimal?)x.QtyApplied) ?? 0) -
                        (allocations.Where(x => x.OrderItem.OrderId == o.Id).SelectMany(x => x.RisItems)
                        .Where(r => r.RISs.PostedDt != null).Sum(r => r.QtyIssue) ?? 0),
                    Status = allocations.Where(x => x.OrderItem.OrderId == o.Id).SelectMany(x => x.RisItems)
                        .Any(r => r.RISs.PostedDt != null && r.QtyIssue > 0) ? "Partial" : "Available"
                });
        }

        public async Task<RisWizardVM> LoadWizardAsync(Guid orderId, string department, string userId)
        {
            if (!await (await WizardCandidatesAsync(department, userId)).AnyAsync(x => x.Id == orderId))
                throw new InvalidOperationException("This PO has no available supply allocations for your selected department.");
            var sources = await (await AccessibleOrderRequests(userId)).Where(x =>
                x.OrderId == orderId && x.Request.Department == department).Include(x => x.Request).Include(x => x.Order).ToListAsync();
            var issuer = await _db.Codextns.AsNoTracking().Where(x => x.CodeMast.Code == "ISSUED-BY")
                .OrderByDescending(x => x.Code).FirstOrDefaultAsync();
            var result = new RisWizardVM { OrderId = orderId, Department = department, PoNo = sources.First().Order.PoNo,
                Groups = new List<RisWizardGroupVM>() };
            foreach (var source in sources)
            {
                var items = await AllocationQuery(orderId, source.PrId).Where(x => x.RemainingQty > 0).OrderBy(x => x.ItemNo).ToListAsync();
                if (items.Count == 0) continue;
                result.Groups.Add(new RisWizardGroupVM
                {
                    Header = new RIS_VM
                    {
                        OrderRequestId = source.Id, PoNo = source.Order.PoNo, PoDate = source.Order.PoDate,
                        PrNo = source.Request.PrNo, Office = source.Request.Department, Fund = source.Request.Fund,
                        RisDate = DateTime.Today, Purpose = source.Request.Purpose,
                        RequestedBy = source.Request.RequestedBy, RequestedByDesignation = source.Request.RequestedDesig,
                        RequestedDate = DateTime.Today, ApprovedBy = source.Request.ApprovedBy,
                        ApprovedByDesignation = source.Request.ApprovedDesig,
                        IssuedBy = issuer == null ? null : issuer.Description,
                        IssuedByDesignation = issuer == null ? null : issuer.Desc2
                    }, Items = items
                });
            }
            return result;
        }

        public async Task<List<Guid>> SubmitWizardAsync(RisWizardVM payload, string userId, string user)
        {
            if (payload == null || payload.Groups == null || payload.Groups.Count == 0)
                throw new InvalidOperationException("Select a PO and enter at least one quantity.");
            using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                LockRisTransactions();
                var latest = await LoadWizardAsync(payload.OrderId, payload.Department, userId);
                var ids = new List<Guid>();
                var seen = new HashSet<Guid>();
                foreach (var group in payload.Groups)
                {
                    if (group == null || group.Header == null || !group.Header.OrderRequestId.HasValue || group.Items == null)
                        throw new InvalidOperationException("Invalid PR group.");
                    if (!seen.Add(group.Header.OrderRequestId.Value)) throw new InvalidOperationException("Duplicate PR group.");
                    if (group.Items.Any(x => x == null)) throw new InvalidOperationException("Invalid allocation.");
                    var rows = group.Items.Where(x => x.QtyRequest != 0 || x.QtyIssue != 0).ToList();
                    if (rows.Count == 0) continue;
                    var source = latest.Groups.SingleOrDefault(x => x.Header.OrderRequestId == group.Header.OrderRequestId);
                    if (source == null) throw new InvalidOperationException("The PR is no longer eligible. Reload the PO.");
                    if (rows.Select(x => x.Id).Distinct().Count() != rows.Count) throw new InvalidOperationException("Duplicate allocation.");
                    foreach (var row in rows)
                    {
                        var current = source.Items.SingleOrDefault(x => x.Id == row.Id);
                        if (current == null) throw new InvalidOperationException("An allocation is unavailable or belongs to another PO/PR.");
                        ValidateQuantities(row.QtyRequest, row.QtyIssue, current.RemainingQty, current.ItemNo);
                    }
                    if (!rows.Any(x => x.QtyIssue > 0)) throw new InvalidOperationException("Each participating PR must issue a positive quantity.");
                    var header = group.Header;
                    header.Id = Guid.NewGuid(); header.RisNo = null; header.CtrlNo = NextCtrlNo(DateTime.Today);
                    header.PoDate = source.Header.PoDate; header.IssuanceSw = false;
                    header.InsertedBy = user; header.UpdatedBy = user;
                    header.InsertedDt = DateTime.Now; header.UpdatedDt = header.InsertedDt;
                    ValidateHeader(header);
                    _validator.ValidateOnCreate(header);
                    var entity = new RISs();
                    MapModelToEntityFields(entity, header, iLgs.Models.Enums.Mode.ADD);
                    foreach (var row in rows) entity.RisItems.Add(new RisItem
                    {
                        Id = Guid.NewGuid(), RisId = entity.Id, OrderItemRequestId = row.Id,
                        QtyRequest = row.QtyRequest, QtyIssue = row.QtyIssue,
                        InsertedBy = user, UpdatedBy = user, InsertedDt = header.InsertedDt, UpdatedDt = header.UpdatedDt
                    });
                    _db.RISses.Add(entity);
                    await _db.SaveChangesAsync();
                    await ValidatePostingQuantities(entity.Id);
                    await PostCoreAsync(entity.Id, user, DateTime.Now);
                    ids.Add(entity.Id);
                }
                if (ids.Count == 0) throw new InvalidOperationException("Enter at least one positive issued quantity.");
                tx.Commit();
                return ids;
            }
        }

        private static void ValidateQuantities(decimal requested, decimal issued, decimal remaining, string item)
        {
            if (requested < 0 || issued < 0 || issued > requested || requested > remaining || issued > remaining ||
                decimal.Round(requested, 2) != requested || decimal.Round(issued, 2) != issued)
                throw new InvalidOperationException("Item " + item + ": quantities must have at most two decimals and satisfy 0 <= issued <= requested <= remaining (" + remaining + "). Reload the PO if availability changed.");
        }

        private static void ValidateHeader(RIS_VM header)
        {
            Validator.ValidateObject(header, new ValidationContext(header), true);
            if (header.RisDate.HasValue && header.RisDate.Value.Date > DateTime.Today)
                throw new InvalidOperationException("RIS date cannot be in the future.");
            if (string.IsNullOrWhiteSpace(header.Purpose) || string.IsNullOrWhiteSpace(header.ApprovedBy) ||
                string.IsNullOrWhiteSpace(header.ApprovedByDesignation))
                throw new InvalidOperationException("Purpose, approving officer and designation are required.");
        }

        private async Task ValidatePostingQuantities(Guid id)
        {
            var header = await _db.RISses.FindAsync(id);
            if (header == null) throw new InvalidOperationException("RIS not found.");
            await _db.Entry(header).ReloadAsync();
            if (header.PostedDt.HasValue || !string.IsNullOrWhiteSpace(header.PostedBy))
                throw new InvalidOperationException("RIS is already posted.");
            var source = await _db.OrderRequests.AsNoTracking().Include(x => x.Order).SingleOrDefaultAsync(x => x.Id == header.OrderRequestId);
            if (source == null || !source.OrderId.HasValue || source.Order.PostedDt == null ||
                !await _db.AIRs.AnyAsync(x => x.OrderId == source.OrderId && x.PostedDt != null))
                throw new InvalidOperationException("A posted PO and AIR are required.");
            var model = await GetByIdAsync(id); ValidateHeader(model);
            var allocations = await AllocationQuery(source.OrderId.Value, source.PrId).ToListAsync();
            var rows = await _db.RisItems.AsNoTracking().Where(x => x.RisId == id).ToListAsync();
            if (rows.Count == 0 || !rows.Any(x => x.QtyIssue > 0)) throw new InvalidOperationException("RIS has no issued quantities.");
            if (rows.GroupBy(x => x.OrderItemRequestId).Any(g => g.Count() != 1)) throw new InvalidOperationException("Duplicate allocation in RIS.");
            foreach (var row in rows)
            {
                var current = allocations.SingleOrDefault(x => x.Id == row.OrderItemRequestId);
                if (current == null) throw new InvalidOperationException("RIS contains an invalid supply allocation or mismatched PO/PR.");
                ValidateQuantities(row.QtyRequest ?? 0, row.QtyIssue ?? 0, current.RemainingQty, current.ItemNo);
            }
        }

        public async ValueTask<RISs> PostAsync(Guid id, string user, DateTime date)
        {
            using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                LockRisTransactions(); await ValidatePostingQuantities(id);
                var result = await PostCoreAsync(id, user, date); tx.Commit(); return result;
            }
        }

        public async ValueTask<RISs> UnpostAsync(Guid id, string user, DateTime date)
        {
            using (var tx = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                LockRisTransactions();
                var entity = await _db.RISses.FindAsync(id);
                if (entity == null) throw new InvalidOperationException("RIS not found.");
                await _db.Entry(entity).ReloadAsync();
                if (!entity.PostedDt.HasValue) throw new InvalidOperationException("Only posted RIS can be unposted.");
                if (!string.IsNullOrWhiteSpace(entity.RisNo) &&
                    await _db.RSMIItems.AnyAsync(x => x.RisNo == entity.RisNo))
                    throw new InvalidOperationException("This RIS is referenced by an RSMI. Resolve that dependency before unposting.");
                // The current PostCore creates only posting audit fields. No RIS-owned
                // stock transaction is created by the existing service to reverse.
                var result = await UnpostCoreAsync(id, user, date); tx.Commit(); return result;
            }
        }

        public ValueTask<RIS_VM> DeleteAsync(RIS_VM model, string user, DateTime date)
        {
            return DeleteCoreAsync(model, user, date);
        }
    }
}
