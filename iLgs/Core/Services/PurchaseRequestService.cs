using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using iLgs.Core.ViewModels;

namespace iLgs.Core.Services
{
    public interface IPurchaseRequestService
    {
        IQueryable<PurchaseRequestViewModel> GetPurchaseRequestsQueryable();
        Task<List<PurchaseRequestViewModel>> GetApprovedForPOAsync();
        Task<List<PurchaseRequestItemViewModel>> GetItemsByPRIdsAsync(List<string> prIds);
        Task<List<PurchaseRequestItemViewModel>> GetItemsByPRIdAsync(string prId);
        Task<PurchaseRequestViewModel> GetByIdAsync(string id);
        Task<bool> ApprovePurchaseRequestAsync(string prId, string approvedBy);
        Task UpdateItemAllocationStatusAsync(List<string> prItemIds, string status);
    }

    public class PurchaseRequestService : IPurchaseRequestService
    {
        private readonly Models.AppManEntities _db;

        public PurchaseRequestService(Models.AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<PurchaseRequestViewModel> GetPurchaseRequestsQueryable()
        {
            return _db.Requests
                .Include(r => r.RequestItems)
                .Select(r => new PurchaseRequestViewModel
                {
                    Id = r.Id.ToString(),
                    PRNumber = r.PrNo,
                    PRDate = r.PrDate,
                    Department = r.Department,
                    //ModeOfProcurement = r.ModeOfProcurement,
                    Status = r.PostedDt != null ? "POSTED" : "DRAFT",
                    ItemCount = r.RequestItems.Count,
                    TotalEstimatedAmount = r.RequestItems.Sum(i => i.TotalCost) ?? 0
                });
        }

        public async Task<List<PurchaseRequestViewModel>> GetApprovedForPOAsync()
        {
            return await _db.Requests
                .Where(r => r.PostedDt != null)
                .Include(r => r.RequestItems)
                .Select(r => new PurchaseRequestViewModel
                {
                    Id = r.Id.ToString(),
                    PRNumber = r.PrNo,
                    PRDate = r.PrDate,
                    Department = r.Department,
                    //ModeOfProcurement = r.ModeOfProcurement,
                    Status = r.PostedDt != null ? "POSTED" : "DRAFT",
                    ItemCount = r.RequestItems.Count,
                    TotalEstimatedAmount = r.RequestItems.Sum(i => i.TotalCost) ?? 0
                })
                .ToListAsync();
        }

        public async Task<List<PurchaseRequestItemViewModel>> GetItemsByPRIdsAsync(List<string> prIds)
        {
            if (prIds == null || !prIds.Any())
                return new List<PurchaseRequestItemViewModel>();

            return await _db.RequestItems
                .Include(i => i.Request)
                .Where(i => prIds.Contains(i.PrId.ToString()) 
                    //&& i.Status != "ORDERED"
                    )
                .Select(i => new PurchaseRequestItemViewModel
                {
                    Id = i.Id.ToString(),
                    PurchaseRequestId = i.PrId.ToString(),
                    PRNumber = i.Request.PrNo,
                    ItemCode = i.PpmpCode,
                    Description = i.Description,
                    TechDescription = i.OtherDesc,
                    Unit = i.Unit,
                    Quantity = (int)(i.Qty ?? 0),
                    UnitCost = i.UnitCost ?? 0,
                    TotalCost = i.TotalCost ?? 0,
                    //Status = i.Status
                })
                .ToListAsync();
        }

        public async Task<List<PurchaseRequestItemViewModel>> GetItemsByPRIdAsync(string prId)
        {            
            return await _db.RequestItems
                .Where(i => i.PrId == Guid.Parse(prId))
                .Select(i => new PurchaseRequestItemViewModel
                {
                    Id = i.Id.ToString(),
                    PurchaseRequestId = i.PrId.ToString(),
                    ItemCode = i.PpmpCode,
                    Description = i.Description,
                    TechDescription = i.OtherDesc,
                    Unit = i.Unit,
                    Quantity = (int)(i.Qty ?? 0),
                    UnitCost = i.UnitCost ?? 0,
                    TotalCost = i.TotalCost ?? 0,
                    //Status = i.Status
                })
                .ToListAsync();
        }

        public async Task<PurchaseRequestViewModel> GetByIdAsync(string id)
        {
            var pr = await _db.Requests
                .Include(r => r.RequestItems)
                .FirstOrDefaultAsync(r => r.Id == Guid.Parse(id));

            if (pr == null) return null;

            return new PurchaseRequestViewModel
            {
                Id = pr.Id.ToString(),
                PRNumber = pr.PrNo,
                PRDate = pr.PrDate,
                Department = pr.Department,
                //ModeOfProcurement = pr.ModeOfProcurement,
                Status = pr.PostedDt != null ? "POSTED" : "DRAFT",
                ItemCount = pr.RequestItems.Count,
                TotalEstimatedAmount = pr.RequestItems.Sum(i => i.TotalCost) ?? 0,
                Items = pr.RequestItems.Select(i => new PurchaseRequestItemViewModel
                {
                    Id = i.Id.ToString(),
                    PurchaseRequestId = i.PrId.ToString(),
                    PRNumber = pr.PrNo,
                    ItemCode = i.PpmpCode,
                    Description = i.Description,
                    TechDescription = i.OtherDesc,
                    Unit = i.Unit,
                    Quantity = (int)(i.Qty ?? 0),
                    UnitCost = i.UnitCost ?? 0,
                    TotalCost = i.TotalCost ?? 0,
                    //Status = i.Status
                }).ToList()
            };
        }

        public async Task<bool> ApprovePurchaseRequestAsync(string prId, string approvedBy)
        {
            //var pr = await _db.Requests.FindAsync(Guid.Parse(prId));
            //if (pr == null) return false;

            //pr.Status = "APPROVED";
            //await _db.SaveChangesAsync();
            return true;
        }

        public async Task UpdateItemAllocationStatusAsync(List<string> prItemIds, string status)
        {
            //var items = await _db.PurchaseRequestItems
            //    .Where(i => prItemIds.Contains(i.Id))
            //    .ToListAsync();

            //foreach (var item in items)
            //{
            //    item.Status = status;
            //}

            //await _db.SaveChangesAsync();
        }
    }
}