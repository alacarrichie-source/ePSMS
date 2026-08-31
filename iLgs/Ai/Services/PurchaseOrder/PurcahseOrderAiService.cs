using iLgs.Ai.Models;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.Ai.PurchaseOrder
{

    public interface IPurchaseOrderAiService
    {
        IQueryable<PurchaseOrderGridViewModel> GetPurchaseOrdersGrid();
        Task<PurchaseOrderDetailViewModel> GetPODetailsAsync(Guid id);
        Task<List<Order>> CreatePOsFromWizardAsync(List<POGroupDraftViewModel> poGroups, string user, bool isDraft);
        Task<bool> PostPOAsync(Guid id, string user, string bacResolutionNo);
        Task<bool> CancelPOAsync(Guid id, string user, string reason);
        Task<string> GeneratePONumberAsync();
    }

    public class PurchaseOrderAiService : IPurchaseOrderAiService
    {
        private readonly AppManEntities _db;

        public PurchaseOrderAiService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<PurchaseOrderGridViewModel> GetPurchaseOrdersGrid()
        {
            return _db.Orders
                .Include(p => p.Supplier)
                .Include(p => p.OrderItems)
                //.Include(p => p.SourcePRs)
                //.Include(p => p.Documents)
                .Select(p => new PurchaseOrderGridViewModel
                {
                    Id = p.Id,
                    PONumber = p.PoNo,
                    PODate = p.PoDate,
                    SupplierName = p.Supplier.Name,
                    SupplierId = p.SupplierId,
                    //SourcePRs = string.Join(", ", p.SourcePRs.Select(pr => pr.PRNumber)),
                    SourcePRs = p.PrNo,
                    ItemCount = p.OrderItems.Count,
                    TotalAmount = p.OrderItems.Sum(s => s.Amount) ?? 0,
                    //Status = p.Status.ToString(),
                    Status = p.PostedDt != null ? "POSTED" : "DRAFT",
                    CreatedBy = p.InsertedBy,
                    DeliveryPeriodDays = p.DeliveryDate,
                    PlaceOfDelivery = p.DeliveryPlace,
                    PaymentTerms = p.TermPayment,
                    ModeOfProcurement = p.PoMode,
                    //BACResolutionNo = p.BACResolutionNo,
                    //HasPOCopy = p.Documents.Any(d => d.Category == DocumentCategory.POCopy),
                    //AdditionalDocsCount = p.Documents.Count(d => d.Category == DocumentCategory.Additional)
                    HasPOCopy = false,
                    AdditionalDocsCount = 0
                });
        }

        public async Task<PurchaseOrderDetailViewModel> GetPODetailsAsync(Guid id)
        {
            var po = await _db.Orders
                .Include(p => p.Supplier)
                //.Include(p => p.OrderItems.Select(li => li.SetLotItems))
                .Include(p => p.OrderItems.Select(i => i.ItemCode.ItemType))
                //.Include(p => p.SourcePRs)
                //.Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return null;

            return new PurchaseOrderDetailViewModel
            {
                Id = po.Id,
                PONumber = po.PoNo,
                PODate = po.PoDate,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier.Name,
                SupplierTIN = po.Supplier.TIN,
                SupplierAddress = po.Supplier.Address,
                //SupplierContactPerson = po.Supplier.ContactPerson,
                SupplierContactPerson = "",
                SupplierContactNumber = po.Supplier.ContactNos,
                DeliveryPeriodDays = po.DeliveryDate,
                PlaceOfDelivery = po.DeliveryPlace,
                PaymentTerms = po.TermPayment,
                ModeOfProcurement = po.PoMode,
                //BACResolutionNo = po.BACResolutionNo,
                BACResolutionNo = "",
                Status = po.PostedDt != null ? "POSTED" : "DRAFT",
                TotalAmount = po.OrderItems.Sum(s => s.Amount) ?? 0,
                CreatedBy = po.InsertedBy,
                CreatedAt = po.InsertedDt,
                PostedBy = po.PostedBy,
                PostedAt = po.PostedDt,
                //SourcePRNumbers = po.SourcePRs.Select(pr => pr.PRNumber).ToList(),
                SourcePRNumbers = po.OrderRequests.Select(pr => pr.Request.PrNo).ToList(),
                LineItems = po.OrderItems.OrderBy(li => li.ItemNo).Select(li => new POLineItemDetailViewModel
                {
                    Id = li.Id,
                    ItemNo = li.ItemNo,
                    Description = li.Description,
                    ItemCode = li.PpmpCode,
                    StockNo = li.PsNo,
                    Quantity = (int)(li.Qty ?? 0),
                    Unit = li.Unit,
                    UnitCost = li.UnitCost ?? 0,
                    GSOCategory = li.ItemCode.ItemType.Description,
                    TechnicalDescription = li.OtherDesc,
                    //SetLotItems = li.OrderItemRequests.Select(s => new POSetLotItemViewModel
                    //{
                    //    ItemNo = s.ItemNo,
                    //    ItemName = s.ItemName,
                    //    Unit = s.Unit,
                    //    EstimatedCost = s.EstimatedCost
                    //}).ToList()
                    SetLotItems = li.OrderItemRequests
                        .SelectMany(oir => oir.RequestItem.RequestSubItems)
                        .OrderBy(s => s.ItemNo)
                        .Select(s => new POSetLotItemViewModel
                        {
                            ItemNo = s.ItemNo,
                            ItemName = s.Description,
                            Unit = s.Unit,
                            Qty = (int)(s.Qty ?? 0),
                            EstimatedCost = s.UnitCost ?? 0
                        })
                        .ToList()
                }).ToList(),
                //Documents = po.Documents.Select(d => new PODocumentViewModel
                //{
                //    Id = d.Id,
                //    FileName = d.FileName,
                //    FilePath = d.FilePath,
                //    FileSize = d.FileSize,
                //    Category = d.Category.ToString(),
                //    UploadedAt = d.UploadedAt
                //}).ToList()
                Documents = new List<PODocumentViewModel>()
            };
        }

        public async Task<List<Order>> CreatePOsFromWizardAsync(
            List<POGroupDraftViewModel> poGroups,
            string user,
            bool isDraft)
        {
            var createdPOs = new List<Order>();

            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    foreach (var grp in poGroups)
                    {
                        // =========================================================
                        // CREATE PO HEADER
                        // =========================================================

                        var poNumber = await GeneratePONumberAsync();

                        var po = new Order
                        {
                            PoNo = poNumber,
                            PoDate = grp.PODate,
                            SupplierId = grp.SupplierId,
                            DeliveryDate = grp.DeliveryPeriodDays,
                            DeliveryPlace = grp.PlaceOfDelivery,
                            TermPayment = grp.PaymentTerms,
                            PoMode = grp.ModeOfProcurement,

                            // Status = isDraft ? POStatus.Draft : POStatus.Posted,

                            InsertedBy = user,
                            InsertedDt = DateTime.Now,

                            PostedBy = isDraft ? null : user,
                            PostedDt = isDraft
                                ? null
                                : (DateTime?)DateTime.Now
                        };

                        decimal grandTotal = 0;
                        int itemIndex = 1;

                        // =========================================================
                        // CREATE CONSOLIDATED PO ITEMS
                        // =========================================================

                        foreach (var itemDraft in grp.Items)
                        {
                            if (itemDraft == null)
                                continue;

                            if (itemDraft.Quantity <= 0)
                            {
                                throw new InvalidOperationException(
                                    $"PO item '{itemDraft.Description}' must have " +
                                    $"a quantity greater than zero.");
                            }

                            // =====================================================
                            // CREATE CONSOLIDATED ORDER ITEM
                            // =====================================================

                            var lineItem = new OrderItem
                            {
                                ItemNo = (itemIndex++).ToString(),
                                Description = itemDraft.Description,
                                PpmpCode = itemDraft.ItemCode,
                                Qty = itemDraft.Quantity,
                                Unit = itemDraft.Unit,
                                UnitCost = itemDraft.UnitCost,
                                Amount = itemDraft.Quantity * itemDraft.UnitCost,
                                OtherDesc = itemDraft.TechnicalDescription
                            };

                            // =====================================================
                            // SAVE PO SET/LOT COMPOSITION
                            // =====================================================

                            //if (itemDraft.SetLotItems != null &&
                            //    itemDraft.SetLotItems.Any())
                            //{
                            //    foreach (var sub in itemDraft.SetLotItems)
                            //    {
                            //        lineItem.orde.Add(
                            //            new POSetLotItemEntity
                            //            {
                            //                ItemNo = sub.ItemNo,
                            //                ItemName = sub.ItemName,
                            //                Unit = sub.Unit,
                            //                EstimatedCost = sub.EstimatedCost
                            //            });
                            //    }
                            //}

                            // =====================================================
                            // SAVE PR ALLOCATIONS
                            //
                            // One OrderItem can have MANY OrderItemRequest records.
                            //
                            // Example:
                            //
                            // OrderItem Qty = 100
                            //
                            // PR001 RequestItem 10 = 40
                            // PR002 RequestItem 25 = 35
                            // PR003 RequestItem 31 = 25
                            //
                            // Total allocation = 100
                            // =====================================================

                            decimal totalAllocatedQty = 0;

                            if (itemDraft.Allocations != null &&
                                itemDraft.Allocations.Any())
                            {
                                foreach (var allocation in itemDraft.Allocations)
                                {
                                    if (allocation.RequestItemId == null)
                                    {
                                        throw new InvalidOperationException(
                                            $"Invalid RequestItemId for PO item " +
                                            $"'{itemDraft.Description}'.");
                                    }

                                    if (allocation.Quantity <= 0)
                                    {
                                        throw new InvalidOperationException(
                                            $"Allocation quantity must be greater " +
                                            $"than zero for PO item " +
                                            $"'{itemDraft.Description}'.");
                                    }

                                    // =============================================
                                    // GET SOURCE PR ITEM
                                    // =============================================

                                    var prItem =
                                        await _db.RequestItems
                                            .Include(x => x.Request)
                                            .FirstOrDefaultAsync(
                                                x => x.Id == allocation.RequestItemId);

                                    if (prItem == null)
                                    {
                                        throw new InvalidOperationException(
                                            $"Purchase Request Item " +
                                            $"{allocation.RequestItemId} was not found.");
                                    }

                                    // =============================================
                                    // VALIDATE REMAINING QUANTITY
                                    // =============================================

                                    //if (allocation.Quantity > prItem.RemainingQty)
                                    //{
                                    //    throw new InvalidOperationException(
                                    //        $"Allocation quantity ({allocation.Quantity}) " +
                                    //        $"for Request Item {prItem.Id} exceeds " +
                                    //        $"its remaining quantity ({prItem.RemainingQty}).");
                                    //}

                                    // =============================================
                                    // CREATE ALLOCATION RECORD
                                    //
                                    // OrderItem = consolidated PO item
                                    // RequestItem = source PR item
                                    // Qty = allocated quantity
                                    // =============================================

                                    var orderItemRequest = new OrderItemRequest
                                    {
                                        OrderItem = lineItem,
                                        RequestItemId = prItem.Id,
                                        QtyApplied = allocation.Quantity
                                    };

                                    lineItem.OrderItemRequests.Add(orderItemRequest);

                                    // =============================================
                                    // UPDATE PR REMAINING QUANTITY
                                    // =============================================

                                    //prItem.RemainingQty =
                                    //    Math.Max(
                                    //        0,
                                    //        prItem.RemainingQty -
                                    //        allocation.Quantity);

                                    // =============================================
                                    // ADD SOURCE PR TO PO
                                    // =============================================

                                    //if (prItem.PurchaseRequest != null &&
                                    //    !po.SourcePRs.Contains(
                                    //        prItem.PurchaseRequest))
                                    //{
                                    //    po.SourcePRs.Add(
                                    //        prItem.PurchaseRequest);
                                    //}

                                    totalAllocatedQty += allocation.Quantity;
                                }
                            }

                            // =====================================================
                            // VALIDATE CONSOLIDATED QUANTITY
                            //
                            // The OrderItem quantity must exactly equal the sum
                            // of all allocations.
                            // =====================================================

                            if (totalAllocatedQty != itemDraft.Quantity)
                            {
                                throw new InvalidOperationException(
                                    $"PO item '{itemDraft.Description}' has a " +
                                    $"consolidated quantity of {itemDraft.Quantity}, " +
                                    $"but its PR allocations total " +
                                    $"{totalAllocatedQty}.");
                            }

                            // =====================================================
                            // ADD CONSOLIDATED ITEM TO PO
                            // =====================================================

                            po.OrderItems.Add(lineItem);

                            grandTotal += lineItem.Amount ?? 0;
                        }

                        // =========================================================
                        // PO TOTAL
                        // =========================================================

                        //po.TotalAmount = grandTotal;

                        // =========================================================
                        // PO COPY DOCUMENT
                        // =========================================================

                        // if (grp.POCopyDoc != null)
                        // {
                        //     po.Documents.Add(new PODocumentEntity
                        //     {
                        //         FileName = grp.POCopyDoc.FileName,
                        //         FilePath = grp.POCopyDoc.FilePath,
                        //         FileSize = grp.POCopyDoc.FileSize,
                        //         Category = DocumentCategory.POCopy
                        //     });
                        // }

                        // =========================================================
                        // ADDITIONAL DOCUMENTS
                        // =========================================================

                        // if (grp.AdditionalDocs != null)
                        // {
                        //     foreach (var addDoc in grp.AdditionalDocs)
                        //     {
                        //         po.Documents.Add(new PODocumentEntity
                        //         {
                        //             FileName = addDoc.FileName,
                        //             FilePath = addDoc.FilePath,
                        //             FileSize = addDoc.FileSize,
                        //             Category = DocumentCategory.Additional
                        //         });
                        //     }
                        // }

                        // =========================================================
                        // SAVE ENTIRE PO GRAPH
                        //
                        // Order
                        //   └── OrderItem
                        //         ├── OrderItemRequest
                        //         └── POSetLotItemEntity
                        // =========================================================

                        _db.Orders.Add(po);

                        await _db.SaveChangesAsync();

                        createdPOs.Add(po);
                    }

                    transaction.Commit();

                    return createdPOs;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<bool> PostPOAsync(Guid id, string user, string bacResolutionNo)
        {
            var po = await _db.Orders.FindAsync(id);
            //if (po == null || po.Status != POStatus.Draft) return false;

            if (po == null || po.UpdatedDt != null) return false;

            //po.Status = POStatus.Posted;
            po.PostedBy = user;
            po.PostedDt = DateTime.Now;

            //if (!string.IsNullOrEmpty(bacResolutionNo))
            //{
            //    po.BACResolutionNo = bacResolutionNo;
            //}

            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelPOAsync(Guid id, string user, string reason)
        {
            //var po = await _db.Orders.Include(p => p.OrderItems).FirstOrDefaultAsync(p => p.Id == id);
            //if (po == null || po.Status == POStatus.Cancelled) return false;

            //po.Status = POStatus.Cancelled;

            //// Revert PR item remaining quantities
            //foreach (var item in po.LineItems)
            //{
            //    if (item.SourcePRItemId.HasValue)
            //    {
            //        var prItem = await _db.PurchaseRequestItems.FindAsync(item.SourcePRItemId.Value);
            //        if (prItem != null)
            //        {
            //            prItem.RemainingQty += item.Quantity;
            //        }
            //    }
            //}

            //await _db.SaveChangesAsync();
            return true;
        }

        public async Task<string> GeneratePONumberAsync()
        {
            var currentYear = DateTime.Now.Year;
            var prefix = $"PO-{currentYear}-";
            var count = await _db.Orders.CountAsync(p => p.PoNo.StartsWith(prefix)) + 1;
            return $"{prefix}{count:D3}";
        }
    }
}