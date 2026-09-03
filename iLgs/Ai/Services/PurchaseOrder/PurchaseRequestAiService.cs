using iLgs.Ai.Models;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace iLgs.Ai.Services.PurchaseOrder
{    
    public interface IPurchaseRequestAiService
    {
        IQueryable<PurchaseRequestGridViewModel> GetApprovedPRsGrid();
        Task<List<PRItemAllocationViewModel>> GetPRItemsForAllocationAsync(string[] prIds);
    }

    public class PurchaseRequestAiService : IPurchaseRequestAiService
    {
        private readonly AppManEntities _db;
        private readonly JavaScriptSerializer _serializer;

        public PurchaseRequestAiService(AppManEntities db)
        {
            _db = db;
            _serializer = new JavaScriptSerializer();
        }

        public IQueryable<PurchaseRequestGridViewModel> GetApprovedPRsGrid()
        {
            return _db.Requests
                //.Where(pr => pr.Status == PRStatus.Approved && pr.Items.Any(i => i.RemainingQty > 0))
                .Where(pr => pr.PostedDt != null && pr.RequestItems.Any())
                .Select(pr => new PurchaseRequestGridViewModel
                {
                    Id = pr.Id,
                    PRNumber = pr.PrNo,
                    PRDate = pr.PrDate,
                    Department = pr.Department,
                    Purpose = pr.Purpose,
                    RequestedBy = pr.RequestedBy,
                    TotalAmount = pr.RequestItems.Sum(s => s.TotalCost) ?? 0,
                    //Status = pr.Status.ToString(),
                    Status = pr.PostedDt != null ? "POSTED" : "DRAFT",
                    ItemsCount = pr.RequestItems.Count                    
                });
        }

        public async Task<List<PRItemAllocationViewModel>> GetPRItemsForAllocationAsync(string[] prIds)
        {
            if (prIds == null || prIds.Length == 0) return new List<PRItemAllocationViewModel>();

            var items = await _db.RequestItems
                .Include(i => i.PPMPItem)
                .Include(i => i.Request)
                .Include(i => i.RequestSubItems)
                .Where(i => prIds.Contains(i.PrId.ToString())) // && i.RemainingQty > 0)
                .ToListAsync();

            var result = new List<PRItemAllocationViewModel>();

            foreach (var item in items)
            {
                var vm = new PRItemAllocationViewModel
                {
                    Id = item.Id,
                    Department = item.Request.Department,
                    PRNumber = item.Request.PrNo,
                    Description = item.Description,
                    Unit = item.Unit,
                    RequestedQty = (int)(item.Qty ?? 0),
                    //RemainingQty = item.RemainingQty,
                    RemainingQty = 0,
                //AssignedQty = item.RemainingQty, // Default assign remaining
                    AssignedQty = (int)(item.Qty ?? 0), // Default assign remaining
                    UnitCost = item.UnitCost ?? 0,
                    //GSOCategory = item.GSOCategory,
                    //SuggestedSupplier = item.SuggestedSupplier,
                    TargetGroupId = "grp-1", // Default assignment to PO Group 1
                    PPMPCode = item.PPMPItem?.Code
                };

                vm.SetLotItems = item.RequestSubItems
                    .OrderBy(subItem => subItem.ItemNo)
                    .Select(subItem => new POSetLotItemViewModel
                    {
                        ItemNo = subItem.ItemNo,
                        ItemName = subItem.Description,
                        Unit = subItem.Unit,
                        Qty = (int)(subItem.Qty ?? 0),
                        EstimatedCost = subItem.UnitCost ?? 0
                    })
                    .ToList();

                result.Add(vm);
            }

            return result;
        }
    }
}
