using iLgs.Ai.Models;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Script.Serialization;

namespace iLgs.Services.Ai.PurchaseOrder
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
                .Include(i => i.Request)
                .Where(i => prIds.Contains(i.PrId.ToString())) // && i.RemainingQty > 0)
                .ToListAsync();

            var result = new List<PRItemAllocationViewModel>();

            foreach (var item in items)
            {
                var vm = new PRItemAllocationViewModel
                {
                    Id = item.Id,
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

                // Deserialize sub-items if present
                //if (!string.IsNullOrEmpty(item.SubItemsJson))
                //{
                //    try
                //    {
                //        vm.SetLotItems = _serializer.Deserialize<List<POSetLotItemViewModel>>(item.SubItemsJson);
                //    }
                //    catch
                //    {
                //        vm.SetLotItems = new List<POSetLotItemViewModel>();
                //    }
                //}
                //else
                //{
                //    vm.SetLotItems = new List<POSetLotItemViewModel>();
                //}

                vm.SetLotItems = new List<POSetLotItemViewModel>();

                result.Add(vm);
            }

            return result;
        }
    }
}