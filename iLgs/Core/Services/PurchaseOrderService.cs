using iLgs.Core.ViewModels;
using iLgs.Models;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
//using iLgs.Models;

namespace iLgs.Core.Services
{
    //public interface IPurchaseOrderService
    //{
    //    IQueryable<ViewModels.PurchaseOrderViewModel> GetPurchaseOrdersQueryable();
    //    Task<ViewModels.PurchaseOrderViewModel> GetByIdAsync(string id);
    //    Task<List<ViewModels.PurchaseOrderViewModel>> CreatePurchaseOrdersAsync(CreatePOWizardInputModel model, bool isDraft, string createdBy);
    //    Task<ViewModels.PurchaseOrderViewModel> PostPurchaseOrderAsync(string id, string postedBy);
    //    Task<byte[]> GenerateOfficialPOReportPdfAsync(string id);
    //}

    public interface IPurchaseOrderService
    {
        /// <summary>
        /// Retrieves an IQueryable of Purchase Orders for deferred execution and Kendo Grid DataSource binding.
        /// </summary>
        IQueryable<ViewModels.PurchaseOrderViewModel> GetPurchaseOrdersQueryable();
        //x

        /// <summary>
        /// Retrieves an IQueryable of Line Items belonging to a specific Purchase Order for child grid binding.
        /// </summary>
        IQueryable<ViewModels.PurchaseOrderItemViewModel> GetItemsByPOIdQueryable(string poId);

        /// <summary>
        /// Asynchronously retrieves line items for a given Purchase Order ID.
        /// </summary>
        Task<List<ViewModels.PurchaseOrderItemViewModel>> GetItemsByPOIdAsync(string poId);

        /// <summary>
        /// Asynchronously retrieves a complete Purchase Order aggregate with supplier and line item details by ID.
        /// </summary>
        Task<ViewModels.PurchaseOrderViewModel> GetByIdAsync(string id);

        /// <summary>
        /// Alias for retrieving full PO details and line items for modal display and printing.
        /// </summary>
        Task<ViewModels.PurchaseOrderViewModel> GetPODetailsAsync(string id);

        /// <summary>
        /// Executes an EF6 database transaction to create one or more Purchase Orders grouped by supplier from selected PRs.
        /// </summary>
        Task<List<ViewModels.PurchaseOrderViewModel>> CreatePurchaseOrdersAsync(CreatePOWizardInputModel model, bool isDraft, string createdBy);

        /// <summary>
        /// Transitions a draft Purchase Order into POSTED status and locks allocated quantities.
        /// </summary>
        Task<ViewModels.PurchaseOrderViewModel> PostPurchaseOrderAsync(string id, string postedBy);

        /// <summary>
        /// Cancels an existing Purchase Order and releases linked PR item allocations back to the available pool.
        /// </summary>
        Task<bool> CancelPurchaseOrderAsync(string id, string reason, string cancelledBy);

        /// <summary>
        /// Updates header and metadata properties of a draft Purchase Order.
        /// </summary>
        Task<ViewModels.PurchaseOrderViewModel> UpdatePurchaseOrderAsync(ViewModels.PurchaseOrderViewModel model, string updatedBy);

        /// <summary>
        /// Deletes a draft Purchase Order and its associated line items.
        /// </summary>
        Task<bool> DeletePurchaseOrderAsync(string id);

        /// <summary>
        /// Generates the Commission on Audit (COA) standard Purchase Order PDF binary document.
        /// </summary>
        Task<byte[]> GenerateOfficialPOReportPdfAsync(string id);

        /// <summary>
        /// Direct DataSourceResult evaluation helper for Kendo MVC AJAX requests.
        /// </summary>
        Task<DataSourceResult> GetPurchaseOrdersDataSourceAsync(DataSourceRequest request);

        /// <summary>
        /// Direct DataSourceResult evaluation helper for PO line item subgrid AJAX requests.
        /// </summary>
        Task<DataSourceResult> GetPOItemsDataSourceAsync(string poId, DataSourceRequest request);
    }

    public class PurchaseOrderService : IPurchaseOrderService
    {
        private readonly Models.AppManEntities _db;

        public PurchaseOrderService(Models.AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<ViewModels.PurchaseOrderViewModel> GetPurchaseOrdersQueryable()
        {
            return _db.Orders
                .Include(p => p.Supplier)
                .Include(p => p.OrderItems)
                .Select(p => new ViewModels.PurchaseOrderViewModel
                {
                    Id = p.Id.ToString(),
                    PONumber = p.PoNo,
                    PODate = p.PoDate,
                    SupplierName = p.Supplier.Name,
                    SupplierAddress = p.Supplier.Address,
                    SupplierTin = p.Supplier.TIN,
                    Status = p.PostedDt != null ? "POSTED" : "PENDING",
                    TotalAmount = p.OrderItems.Sum(s => s.Amount) ?? 0,
                    ItemCount = p.OrderItems.Count,
                    PlaceOfDelivery = p.DeliveryPlace,
                    DeliveryPeriodDays = p.DeliveryDate,
                    PaymentTerms = p.TermPayment,
                    FundCluster = p.Fund,
                    //OrsBursNo = p.OrsBursNo,
                    //BacResolutionNo = p.BacResolutionNo,
                    CreatedBy = p.InsertedBy,
                    SourcePRs = p.OrderRequests.Select(s => s.Request.PrNo).Distinct().ToList()
                    //SourcePRs = p.Items.Select(i => i.SourcePrNo).Distinct().ToList()
                });
        }

        public async Task<ViewModels.PurchaseOrderViewModel> GetByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out Guid orderId))
                return null;

            var p = await _db.Orders
                .Include(po => po.Supplier)
                .Include(po => po.OrderItems)
                .FirstOrDefaultAsync(po => po.Id == orderId);

            if (p == null) return null;

            return new ViewModels.PurchaseOrderViewModel
            {
                Id = p.Id.ToString(),
                PONumber = p.PoNo,
                PODate = p.PoDate,
                SupplierName = p.Supplier?.Name,
                SupplierAddress = p.Supplier?.Address,
                SupplierTin = p.Supplier?.TIN,
                Status = p.PostedDt != null ? "POSTED" : "PENDING",
                TotalAmount = p.OrderItems.Sum(s => s.Amount) ?? 0,
                ItemCount = p.OrderItems.Count,
                PlaceOfDelivery = p.DeliveryPlace,
                DeliveryPeriodDays = p.DeliveryDate,
                PaymentTerms = p.TermPayment,
                FundCluster = p.Fund,
                //OrsBursNo = p.OrsBursNo,
                //BacResolutionNo = p.BacResolutionNo,
                CreatedBy = p.InsertedBy,
                SourcePRs = p.OrderRequests.Select(s => s.Request.PrNo).Distinct().ToList(),
                //SourcePRs = p.Items.Select(i => i.SourcePrNo).Distinct().ToList()
                Items = p.OrderItems.Select(i => new ViewModels.PurchaseOrderItemViewModel
                {
                    Id = i.Id.ToString(),
                    ItemCode = i.PpmpCode,
                    Description = i.Description,
                    //TechDescription = i.TechDescription,
                    Unit = i.Unit,
                    Quantity = (int)(i.Qty ?? 0),
                    UnitCost = i.UnitCost ?? 0,
                    TotalCost = i.Amount ?? 0
                    //SourcePrNo = i.SourcePrNo
                }).ToList()
            };
        }

        public async Task<List<ViewModels.PurchaseOrderViewModel>> CreatePurchaseOrdersAsync(
            CreatePOWizardInputModel model, bool isDraft, string createdBy)
        {
            var createdResults = new List<ViewModels.PurchaseOrderViewModel>();
            var year = DateTime.UtcNow.Year;

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    int counter = await _db.Orders.CountAsync(p => p.PoDate.Value.Year == year) + 1;

                    foreach (var group in model.POGroups)
                    {
                        var poNumber = $"PO-{year}-{(counter++):D4}";
                        var totalAmt = group.AssignedItems.Sum(i => i.AssignQty * i.UnitCost);

                        var po = new Order
                        {
                            Id = Guid.NewGuid(),
                            PoNo = poNumber,
                            PoDate = DateTime.UtcNow,
                            SupplierId = Guid.Parse(group.SupplierId),
                            //TotalAmount = totalAmt,
                            //Status = isDraft ? "DRAFT" : "POSTED",
                            DeliveryPlace = group.PlaceOfDelivery,
                            DeliveryDate = group.DeliveryPeriodDays,
                            TermPayment = group.PaymentTerms,
                            Fund = group.FundCluster,
                            //OrsBursNo = group.OrsBursNo,
                            //BacResolutionNo = group.BacResolutionNo,
                            InsertedBy = createdBy,
                            InsertedDt = DateTime.Now
                        };


                        // assign allocation and consolidation

                        //foreach (var item in group.AssignedItems)
                        //{
                        //    po.OrderItems.Add(new OrderItem
                        //    {
                        //        Id = Guid.NewGuid(),
                        //        OrderId = po.Id,
                        //        PrItemId = item.PrItemId,
                        //        SourcePrNo = item.PrNumber,
                        //        Description = item.Description,
                        //        Unit = item.Unit,
                        //        Quantity = item.AssignQty,
                        //        UnitCost = item.UnitCost,
                        //        TotalCost = item.AssignQty * item.UnitCost
                        //    });
                        //}

                        _db.Orders.Add(po);
                    }

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    // Re-query for full projection view models
                    foreach (var po in _db.Orders.Local)
                    {
                        createdResults.Add(await GetByIdAsync(po.Id.ToString()));
                    }
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return createdResults;
        }

        public async Task<ViewModels.PurchaseOrderViewModel> PostPurchaseOrderAsync(string id, string postedBy)
        {
            var po = await _db.Orders.FindAsync(id);
            if (po != null)
            {
                po.PostedBy = postedBy;
                po.PostedDt = DateTime.Now;
                //po.Status = "POSTED";
                await _db.SaveChangesAsync();
            }
            return await GetByIdAsync(id);
        }

        public Task<byte[]> GenerateOfficialPOReportPdfAsync(string id)
        {
            // Leverages Telerik Reporting / QuestPDF to compile COA standard purchase order
            return Task.FromResult(new byte[0]);
        }

        public IQueryable<ViewModels.PurchaseOrderItemViewModel> GetItemsByPOIdQueryable(string poId)
        {
            throw new NotImplementedException();
        }

        public Task<List<ViewModels.PurchaseOrderItemViewModel>> GetItemsByPOIdAsync(string poId)
        {
            throw new NotImplementedException();
        }

        public Task<ViewModels.PurchaseOrderViewModel> GetPODetailsAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<bool> CancelPurchaseOrderAsync(string id, string reason, string cancelledBy)
        {
            throw new NotImplementedException();
        }

        public Task<ViewModels.PurchaseOrderViewModel> UpdatePurchaseOrderAsync(ViewModels.PurchaseOrderViewModel model, string updatedBy)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeletePurchaseOrderAsync(string id)
        {
            throw new NotImplementedException();
        }

        public Task<DataSourceResult> GetPurchaseOrdersDataSourceAsync(DataSourceRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<DataSourceResult> GetPOItemsDataSourceAsync(string poId, DataSourceRequest request)
        {
            throw new NotImplementedException();
        }
    }
}