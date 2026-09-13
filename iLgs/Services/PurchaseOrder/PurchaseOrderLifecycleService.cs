using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Models;

namespace iLgs.Services.PurchaseOrder
{
    // Shared by the current wizard and legacy Orders entry points.
    public class PurchaseOrderLifecycleService
    {
        private readonly AppManEntities _db;
        public const string PostedAirMessage = "This Purchase Order cannot be changed because it already has a posted Acceptance and Inspection Report. Posted downstream transactions must remain consistent with the Purchase Order.";
        public const string DraftAirMessage = "This Purchase Order has an unposted AIR. Remove or cancel the dependent AIR in the AIR module first, then try again.";

        public PurchaseOrderLifecycleService(AppManEntities db) { _db = db; }

        public IQueryable<AIR> DependentAirs(Guid orderId)
        {
            return _db.AIRs.Where(a => a.OrderId == orderId ||
                a.AIRItems.Any(i => i.OrderItemRequest.OrderItem.OrderId == orderId ||
                    i.AIRItemAllocations.Any(x => x.OrderItemRequest.OrderItem.OrderId == orderId)));
        }

        public static string DependencyMessage(bool hasPostedAir, bool hasDraftAir)
        {
            return hasPostedAir ? PostedAirMessage : hasDraftAir ? DraftAirMessage : null;
        }

        public async Task<string> GetDependencyMessageAsync(Guid orderId)
        {
            var airs = DependentAirs(orderId);
            if (await airs.AnyAsync(a => a.PostedDt != null)) return PostedAirMessage;
            if (await airs.AnyAsync()) return DraftAirMessage;
            return null;
        }

        public async Task EnsureEditableAsync(Guid orderId)
        {
            var po = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.Id == orderId);
            if (po == null) throw new InvalidOperationException("Purchase Order not found.");
            var dependency = await GetDependencyMessageAsync(orderId);
            if (dependency != null) throw new InvalidOperationException(dependency);
            if (po.PostedDt.HasValue || !String.IsNullOrWhiteSpace(po.PostedBy))
                throw new InvalidOperationException("Unpost this Purchase Order before editing or deleting it.");
            if (await _db.PARs.AnyAsync(p => p.OrderId == orderId))
                throw new InvalidOperationException("This Purchase Order already has a PAR and cannot be changed.");
        }

        public void EnsureEditable(Guid id)
        {
            var po = _db.Orders.AsNoTracking().FirstOrDefault(x => x.Id == id);
            if (po == null) throw new InvalidOperationException("Purchase Order not found.");
            var airs = DependentAirs(id);
            var message = DependencyMessage(airs.Any(x => x.PostedDt != null), airs.Any());
            if (message != null) throw new InvalidOperationException(message);
            if (po.PostedDt.HasValue || !String.IsNullOrWhiteSpace(po.PostedBy))
                throw new InvalidOperationException("Unpost this Purchase Order before editing or deleting it.");
            if (_db.PARs.Any(x => x.OrderId == id))
                throw new InvalidOperationException("This Purchase Order already has a PAR and cannot be changed.");
        }

        // Must be called within a transaction. Serializes PO lifecycle changes, then refreshes tracked state.
        public async Task<Order> LockAsync(Guid id)
        {
            var ids = await _db.Database.SqlQuery<Guid>(
                "SELECT Id FROM dbo.Orders WITH (UPDLOCK, HOLDLOCK) WHERE Id = @p0", id).ToListAsync();
            if (ids.Count == 0) throw new InvalidOperationException("Purchase Order not found.");
            var po = await _db.Orders.FindAsync(id);
            await _db.Entry(po).ReloadAsync();
            return po;
        }

        public async Task<Order> UnpostAsync(Guid id, string user, DateTime date)
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var po = await LockAsync(id);
                var dependency = await GetDependencyMessageAsync(id);
                if (dependency != null) throw new InvalidOperationException(dependency);
                if (!po.PostedDt.HasValue && String.IsNullOrWhiteSpace(po.PostedBy))
                    throw new InvalidOperationException("This Purchase Order is already unposted.");
                if (await _db.RISses.AnyAsync(r => r.OrderRequest.OrderId == id && r.PostedDt != null))
                    throw new InvalidOperationException("This Purchase Order already has a posted RIS and cannot be unposted.");
                po.PostedBy = null;
                po.PostedDt = null;
                po.UpdatedBy = user;
                po.UpdatedDt = date;
                await _db.SaveChangesAsync();
                transaction.Commit();
                return po;
            }
        }
    }
}
