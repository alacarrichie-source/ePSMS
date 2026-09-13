using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using iLgs.Ai.Models;
using iLgs.Models;
using iLgs.Services.PurchaseOrder;
using Newtonsoft.Json.Linq;

namespace iLgs.Ai.Services.PurchaseOrder
{
    public partial class PurchaseOrderAiService
    {
        public async Task<POGroupDraftViewModel> GetExistingPOAsync(Guid id)
        {
            await new PurchaseOrderLifecycleService(_db).EnsureEditableAsync(id);
            var po = await LoadExistingOrderAsync(id);
            var group = new POGroupDraftViewModel {
                GroupId = po.Id.ToString(), GroupName = "Purchase Order " + po.PoNo,
                PONumber = po.PoNo, CtrlNo = po.CtrlNo, PRNumber = po.PrNo, DepartmentName = po.Department,
                PODate = po.PoDate ?? DateTime.Today, SupplierId = po.SupplierId, SupplierName = po.SupName,
                SupBusiness = po.SupBusiness, SupAddress = po.SupAddress, SupTIN = po.SupTIN,
                SupEmail = po.SupEmail, SupZipCode = po.SupZipCode, SupContactNo = po.SupContactNo,
                DeliveryPeriodDays = po.DeliveryDate, DeliveryDate = po.DeliveryDate, PlaceOfDelivery = po.DeliveryPlace,
                TermDelivery = po.TermDelivery, PaymentTerms = po.TermPayment, ModeOfProcurement = po.PoMode,
                SignedBySuppName = po.SignedBySuppName, SignedBySuppDate = po.SignedBySuppDate,
                SignedByAuthName = po.SignedByAuthName, SignedByAuthDesignation = po.SignedByAuthDesignation,
                ResoNo = po.ResoNo, CertifiedCorrectBy = po.CertifiedCorrectBy, CertifiedCorrectDate = po.CertifiedCorrectDate
            };
            group.SourcePRs = po.OrderRequests.Where(x => x.Request != null).Select(x => new POGroupSourcePRViewModel {
                PRId = x.Request.Id, PRNumber = x.Request.PrNo
            }).ToList();
            foreach (var item in po.OrderItems.OrderBy(x => x.ItemNo))
            {
                group.Items.Add(new POLineItemDraftViewModel {
                    Id = item.Id, ItemNo = item.ItemNo, Description = item.Description,
                    ItemCodeId = item.ItemCodeId, ItemCode = item.PpmpCode, PpmpCode = item.PpmpCode,
                    StockNo = item.PsNo, Quantity = Convert.ToInt32(item.Qty ?? 0),
                    Unit = item.Unit, UnitCost = item.UnitCost ?? 0, TechnicalDescription = item.OtherDesc,
                    AllFields = GetAllFieldDictionary(item.AllField, _itemCodeService.GetPartialView(item.ItemCodeId)),
                    Allocations = item.OrderItemRequests.Select(x => new POItemAllocationDraftViewModel {
                        RequestItemId = x.RequestItemId, PRId = x.RequestItem == null ? null : x.RequestItem.PrId,
                        PRNumber = x.RequestItem == null || x.RequestItem.Request == null ? null : x.RequestItem.Request.PrNo,
                        Quantity = x.QtyApplied ?? 0
                    }).ToList(),
                    SetLotItems = item.OrderSubItems.OrderBy(x => x.SortOrder).Select(x => new POSetLotItemViewModel {
                        OrderSubItemId = x.Id, RequestSubItemId = x.OrderSubItemRequests.Select(r => (Guid?)r.RequestSubItemId).FirstOrDefault(),
                        ItemNo = x.ItemNo, ItemName = x.Description, Unit = x.Unit,
                        Qty = Convert.ToInt32(x.QtyPerSet), EstimatedCost = x.UnitCost ?? 0
                    }).ToList()
                });
            }
            // Older wizard uploads were retained in the completed progress JSON, not attached to Order.
            var progress = await _db.PurchaseOrderWizardProgresses.AsNoTracking()
                .Where(x => x.IsCompleted && x.DraftNo == po.CtrlNo).OrderByDescending(x => x.UpdatedAt).FirstOrDefaultAsync();
            if (progress != null && !String.IsNullOrWhiteSpace(progress.SelectedPrIds))
            {
                try {
                    var state = JObject.Parse(progress.SelectedPrIds);
                    var saved = (state["poGroups"] as JArray ?? new JArray()).OfType<JObject>()
                        .FirstOrDefault(x => String.Equals((string)x["poNumber"], po.PoNo, StringComparison.OrdinalIgnoreCase));
                    if (saved != null) {
                        group.POCopyDoc = saved["poCopyDoc"] == null || saved["poCopyDoc"].Type == JTokenType.Null ? null : saved["poCopyDoc"].ToObject<PODocumentViewModel>();
                        group.AdditionalDocs = saved["additionalDocs"] == null ? new List<PODocumentViewModel>() : saved["additionalDocs"].ToObject<List<PODocumentViewModel>>();
                    }
                } catch (Newtonsoft.Json.JsonException) { /* Legacy PR-ID-only progress contains no document metadata. */ }
            }
            var uploads = await _db.Uploads.AsNoTracking().Where(x => x.ImageId == id).ToListAsync();
            foreach (var upload in uploads)
            {
                var directory = (upload.VirtualDirectory ?? "").Replace('\\', '/');
                var document = new PODocumentViewModel {
                    ExistingUploadId = upload.Id, FileName = upload.Description ?? upload.FileName,
                    FilePath = directory.StartsWith("~/App_Data/Uploads/", StringComparison.OrdinalIgnoreCase) ? directory.TrimEnd('/') + "/" + upload.FileName : null,
                    Category = directory.EndsWith("/ORDERS/", StringComparison.OrdinalIgnoreCase) || directory.EndsWith("/POCopies/", StringComparison.OrdinalIgnoreCase) ? "POCopy" : "Supporting",
                    UploadedAt = upload.InsertedDt ?? DateTime.MinValue
                };
                if (document.Category == "POCopy") group.POCopyDoc = document;
                else if (!group.AdditionalDocs.Any(x => x.FilePath != null && x.FilePath == document.FilePath)) group.AdditionalDocs.Add(document);
            }
            return group;
        }

        private Task<Order> LoadExistingOrderAsync(Guid id)
        {
            return _db.Orders
                .Include(x => x.OrderRequests.Select(r => r.Request))
                .Include(x => x.OrderItems.Select(i => i.AllField))
                .Include(x => x.OrderItems.Select(i => i.OrderItemRequests.Select(r => r.RequestItem.Request)))
                .Include(x => x.OrderItems.Select(i => i.OrderSubItems.Select(s => s.OrderSubItemRequests)))
                .FirstAsync(x => x.Id == id);
        }

        public async Task<Order> SaveExistingPOAsync(Guid id, POGroupDraftViewModel group, string user, bool post)
        {
            if (group == null) throw new InvalidOperationException("No Purchase Order was submitted.");
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                var lifecycle = new PurchaseOrderLifecycleService(_db);
                await lifecycle.LockAsync(id);
                await lifecycle.EnsureEditableAsync(id);
                var po = await LoadExistingOrderAsync(id);
                if (!String.Equals(group.PONumber, po.PoNo, StringComparison.Ordinal))
                    throw new InvalidOperationException("The existing PO number cannot be changed during correction.");
                group.CtrlNo = po.CtrlNo;
                var message = await ValidateWizardGroupsAsync(new List<POGroupDraftViewModel> { group }, post, id);
                if (message != null) throw new InvalidOperationException(message);
                var now = DateTime.Now;
                po.PoDate = group.PODate.Date;
                po.SupplierId = group.SupplierId; po.SupName = group.SupplierName;
                po.SupBusiness = group.SupBusiness; po.SupAddress = group.SupAddress; po.SupTIN = group.SupTIN;
                po.SupEmail = group.SupEmail; po.SupZipCode = group.SupZipCode; po.SupContactNo = group.SupContactNo;
                po.DeliveryDate = group.DeliveryDate; po.DeliveryPlace = group.PlaceOfDelivery;
                po.TermDelivery = group.TermDelivery; po.TermPayment = group.PaymentTerms; po.PoMode = group.ModeOfProcurement;
                po.SignedBySuppName = group.SignedBySuppName; po.SignedBySuppDate = group.SignedBySuppDate;
                po.SignedByAuthName = group.SignedByAuthName; po.SignedByAuthDesignation = group.SignedByAuthDesignation;
                po.ResoNo = group.ResoNo; po.CertifiedCorrectBy = group.CertifiedCorrectBy; po.CertifiedCorrectDate = group.CertifiedCorrectDate;

                if (group.Items.GroupBy(x => x.Id).Any(x => x.Key != Guid.Empty && x.Count() > 1))
                    throw new InvalidOperationException("The same PO line was submitted more than once.");
                var retained = new HashSet<Guid>();
                foreach (var draft in group.Items)
                {
                    var item = po.OrderItems.FirstOrDefault(x => x.Id == draft.Id);
                    if (item == null) {
                        if (draft.Id != Guid.Empty && await _db.OrderItems.AnyAsync(x => x.Id == draft.Id))
                            throw new InvalidOperationException("A submitted line belongs to another Purchase Order.");
                        item = new OrderItem { Id = Guid.NewGuid(), OrderId = id, InsertedBy = user, InsertedDt = now };
                        po.OrderItems.Add(item);
                    }
                    retained.Add(item.Id);
                    item.ItemNo = draft.ItemNo; item.ItemCodeId = draft.ItemCodeId; item.Description = draft.Description;
                    item.PpmpCode = draft.PpmpCode; item.PsNo = draft.StockNo; item.Unit = draft.Unit;
                    item.Qty = draft.Quantity; item.UnitCost = draft.UnitCost; item.Amount = draft.Quantity * draft.UnitCost;
                    item.OtherDesc = draft.TechnicalDescription; item.UpdatedBy = user; item.UpdatedDt = now;
                    var fields = await CreateValidatedAllFieldAsync(draft, item.Id, user, now);
                    if (fields != null) {
                        if (item.AllField == null) item.AllField = fields;
                        else {
                            fields.InsertedBy = item.AllField.InsertedBy; fields.InsertedDt = item.AllField.InsertedDt;
                            _db.Entry(item.AllField).CurrentValues.SetValues(fields);
                        }
                    }
                    SyncAllocations(item, draft.Allocations, user, now);
                    SyncSubItems(item, draft.SetLotItems, user, now);
                }
                foreach (var removed in po.OrderItems.Where(x => !retained.Contains(x.Id)).ToList())
                {
                    //if (removed.OrderItemUnitGroupDescriptionItems.Any() || removed.PARItems.Any())
                    //    throw new InvalidOperationException("A PO line with linked group/property records cannot be removed.");
                    SyncAllocations(removed, new List<POItemAllocationDraftViewModel>(), user, now);
                    SyncSubItems(removed, new List<POSetLotItemViewModel>(), user, now);
                    if (removed.AllField != null) _db.AllFields.Remove(removed.AllField);
                    _db.OrderItems.Remove(removed);
                }
                var prIds = group.Items.SelectMany(x => x.Allocations).Select(x => x.RequestItemId.Value).ToList();
                var prs = await _db.RequestItems.Where(x => prIds.Contains(x.Id)).Select(x => x.Request).Distinct().ToListAsync();
                foreach (var link in po.OrderRequests.Where(x => !prs.Any(r => r.Id == x.PrId)).ToList()) {
                    if (link.RISses.Any()) throw new InvalidOperationException("A source PR with linked RIS records cannot be removed.");
                    _db.OrderRequests.Remove(link);
                }
                foreach (var pr in prs.Where(x => !po.OrderRequests.Any(r => r.PrId == x.Id)))
                    po.OrderRequests.Add(new OrderRequest { Id = Guid.NewGuid(), PrId = pr.Id, InsertedBy = user, InsertedDt = now, UpdatedBy = user, UpdatedDt = now });
                po.PrNo = String.Join(", ", prs.Select(x => x.PrNo).Distinct());
                await AttachWizardDocumentsAsync(po, group, user, now);
                po.UpdatedBy = user; po.UpdatedDt = now;
                if (post) { po.PostedBy = user; po.PostedDt = now; }
                await _db.SaveChangesAsync();
                transaction.Commit();
                return po;
            }
        }

        private void SyncAllocations(OrderItem item, List<POItemAllocationDraftViewModel> drafts, string user, DateTime now)
        {
            drafts = drafts ?? new List<POItemAllocationDraftViewModel>();
            foreach (var existing in item.OrderItemRequests.ToList())
            {
                var draft = drafts.FirstOrDefault(x => x.RequestItemId == existing.RequestItemId);
                if (draft == null || draft.Quantity != existing.QtyApplied) {
                    if (existing.AIRItems.Any() || existing.AIRItemAllocations.Any() || existing.PsCardItems.Any() || existing.RisItems.Any())
                        throw new InvalidOperationException("An allocation linked to a downstream record cannot be changed.");
                }
                if (draft == null) _db.OrderItemRequests.Remove(existing);
                else { existing.QtyApplied = draft.Quantity; existing.UpdatedBy = user; existing.UpdatedDt = now; }
            }
            foreach (var draft in drafts.Where(x => !item.OrderItemRequests.Any(r => r.RequestItemId == x.RequestItemId)))
                item.OrderItemRequests.Add(new OrderItemRequest { Id = Guid.NewGuid(), RequestItemId = draft.RequestItemId,
                    QtyApplied = draft.Quantity, InsertedBy = user, InsertedDt = now, UpdatedBy = user, UpdatedDt = now });
        }

        private void SyncSubItems(OrderItem item, List<POSetLotItemViewModel> drafts, string user, DateTime now)
        {
            drafts = drafts ?? new List<POSetLotItemViewModel>();
            var retained = new HashSet<Guid>();
            foreach (var draft in drafts)
            {
                var sub = item.OrderSubItems.FirstOrDefault(x => x.Id == draft.OrderSubItemId);
                if (draft.OrderSubItemId.HasValue && sub == null)
                    throw new InvalidOperationException("A set/lot component does not belong to the submitted PO line.");
                if (sub == null) {
                    sub = new OrderSubItem { Id = Guid.NewGuid(), OrderItem = item, InsertedBy = user, InsertedDt = now, IsActive = true };
                    item.OrderSubItems.Add(sub);
                    if (draft.RequestSubItemId.HasValue) sub.OrderSubItemRequests.Add(new OrderSubItemRequest {
                        Id = Guid.NewGuid(), RequestSubItemId = draft.RequestSubItemId.Value, QtyApplied = draft.Qty,
                        InsertedBy = user, InsertedDt = now, UpdatedBy = user, UpdatedDt = now });
                }
                if (!retained.Add(sub.Id)) throw new InvalidOperationException("A set/lot component was submitted more than once.");
                sub.ItemNo = draft.ItemNo; sub.Description = draft.ItemName; sub.Unit = draft.Unit; sub.QtyPerSet = draft.Qty;
                sub.TotalQty = draft.Qty * (item.Qty ?? 0); sub.UnitCost = draft.EstimatedCost;
                sub.EstimatedTotalCost = draft.Qty * draft.EstimatedCost; sub.UpdatedBy = user; sub.UpdatedDt = now;
            }
            foreach (var sub in item.OrderSubItems.Where(x => !retained.Contains(x.Id)).ToList()) {
                _db.OrderSubItemRequests.RemoveRange(sub.OrderSubItemRequests);
                _db.OrderSubItems.Remove(sub);
            }
        }

        private async Task AttachWizardDocumentsAsync(Order po, POGroupDraftViewModel group, string user, DateTime now)
        {
            var documents = new List<PODocumentViewModel>(group.AdditionalDocs ?? new List<PODocumentViewModel>());
            if (group.POCopyDoc != null) documents.Add(group.POCopyDoc);
            foreach (var doc in documents)
            {
                if (doc.ExistingUploadId.HasValue) {
                    if (!await _db.Uploads.AnyAsync(x => x.Id == doc.ExistingUploadId.Value && x.ImageId == po.Id))
                        throw new InvalidOperationException("An attachment belongs to another record.");
                    continue;
                }
                var path = (doc.FilePath ?? "").Replace('\\', '/');
                if (!path.StartsWith("~/App_Data/Uploads/POCopies/", StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWith("~/App_Data/Uploads/SupportingDocs/", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("An attachment path is invalid. Upload the document again.");
                if (path.Contains("..") || !File.Exists(HttpContext.Current.Server.MapPath(path)))
                    throw new InvalidOperationException("An attachment could not be found. Upload the document again.");
                var directory = path.Substring(0, path.LastIndexOf('/') + 1);
                var name = Path.GetFileName(path);
                if (!await _db.Uploads.AnyAsync(x => x.ImageId == po.Id && x.FileName == name && x.VirtualDirectory == directory))
                    _db.Uploads.Add(new Upload { Id = Guid.NewGuid(), ImageId = po.Id, FileName = name,
                        Description = doc.FileName, VirtualDirectory = directory, Remarks = doc.Category,
                        InsertedBy = user, InsertedDt = now, UpdatedBy = user, UpdatedDt = now });
            }
        }
    }
}
