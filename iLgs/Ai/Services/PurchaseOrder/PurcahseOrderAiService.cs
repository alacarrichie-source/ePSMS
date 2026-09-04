using iLgs.Ai.Models;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace iLgs.Ai.Services.PurchaseOrder
{
    public interface IPurchaseOrderAiService
    {
        IQueryable<PurchaseOrderGridViewModel> GetPurchaseOrdersGrid();
        Task<PurchaseOrderDetailViewModel> GetPODetailsAsync(Guid id);
        Task<List<Order>> CreatePOsFromWizardAsync(List<POGroupDraftViewModel> poGroups, string user, bool isDraft);
        Task<bool> PostPOAsync(Guid id, string user, string bacResolutionNo);
        Task<bool> CancelPOAsync(Guid id, string user, string reason);
        Task<string> GeneratePONumberAsync();

        // NEW: Wizard Draft Functionalities
        Task<Guid> SaveWizardProgressAsync(Guid? draftId, string stateJson, List<Guid> prIds, int currentStep, string user);
        Task<WizardDraftDto> GetWizardDraftAsync(Guid draftId, string user);
        Task<List<POWizardDraftListItemViewModel>> GetActiveWizardDraftsAsync(string user);
        Task<bool> DiscardWizardDraftAsync(Guid draftId, string user);
    }

    public class PurchaseOrderAiService : IPurchaseOrderAiService
    {
        private readonly AppManEntities _db;

        public PurchaseOrderAiService(AppManEntities db)
        {
            _db = db;
        }

        // ==========================================
        // NEW: WIZARD PROGRESS LOGIC
        // ==========================================

        /// <summary>
        /// Saves the complete client-side wizard state in the existing JSON-capable progress column.
        /// </summary>
        public async Task<Guid> SaveWizardProgressAsync(Guid? draftId, string stateJson, List<Guid> prIds, int currentStep, string user)
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    PurchaseOrderWizardProgress draft;
                    if (draftId.HasValue)
                    {
                        draft = await _db.PurchaseOrderWizardProgresses
                            .FirstOrDefaultAsync(x => x.Id == draftId.Value && x.CreatedBy == user && x.IsCompleted == false);
                        if (draft == null)
                            throw new InvalidOperationException("The wizard draft was not found or is no longer active.");
                    }
                    else
                    {
                        var createdAt = DateTime.Now;
                        draft = new PurchaseOrderWizardProgress
                        {
                            Id = Guid.NewGuid(),
                            DraftNo = await GenerateDraftNumberAsync(createdAt),
                            CreatedBy = user,
                            CreatedAt = createdAt
                        };
                        _db.PurchaseOrderWizardProgresses.Add(draft);
                    }

                    draft.LastStep = currentStep;
                    draft.UpdatedAt = DateTime.Now;
                    draft.SelectedPrIds = String.IsNullOrWhiteSpace(stateJson)
                        ? string.Join(",", prIds ?? new List<Guid>())
                        : stateJson;

                    await _db.SaveChangesAsync();
                    transaction.Commit();
                    return draft.Id;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private async Task<string> GenerateDraftNumberAsync(DateTime date)
        {
            string prefix = date.ToString("yyyyMM");
            string legacyPrefix = date.ToString("yyyy-MM-");
            var draftNumbers = await _db.PurchaseOrderWizardProgresses
                .Where(x => x.DraftNo != null && x.DraftNo.StartsWith(prefix))
                .Select(x => x.DraftNo)
                .ToListAsync();
            var controlNumbers = await _db.Orders
                .Where(x => x.CtrlNo != null &&
                    (x.CtrlNo.StartsWith(prefix) || x.CtrlNo.StartsWith(legacyPrefix)))
                .Select(x => x.CtrlNo)
                .ToListAsync();

            int lastSequence = draftNumbers.Concat(controlNumbers)
                .Select(x =>
                {
                    int sequence;
                    if (x.Length == 10 && Int32.TryParse(x.Substring(6, 4), out sequence))
                        return sequence;
                    if (x.Length == 12 && Int32.TryParse(x.Substring(8, 4), out sequence))
                        return sequence;
                    return 0;
                })
                .DefaultIfEmpty(0)
                .Max();

            if (lastSequence >= 9999)
                throw new InvalidOperationException("The monthly Purchase Order control-number sequence is exhausted.");

            return prefix + (lastSequence + 1).ToString("D4");
        }
        /// <summary>
        /// Retrieves previously saved PR selections to repopulate Step 1
        /// </summary>
        public async Task<WizardDraftDto> GetWizardDraftAsync(Guid draftId, string user)
        {
            var draft = await _db.PurchaseOrderWizardProgresses
                .FirstOrDefaultAsync(x => x.Id == draftId && x.CreatedBy == user && x.IsCompleted == false);

            if (draft == null || string.IsNullOrEmpty(draft.SelectedPrIds))
                return null;

            var result = new WizardDraftDto
            {
                DraftId = draft.Id,
                CurrentStep = draft.LastStep,
                PrIds = new List<Guid>()
            };
            if (draft.SelectedPrIds.TrimStart().StartsWith("{"))
            {
                try
                {
                    var state = JObject.Parse(draft.SelectedPrIds);
                    result.StateJson = draft.SelectedPrIds;
                    result.PrIds = (state["selectedPRIds"] ?? new JArray())
                        .Values<string>()
                        .Select(value => { Guid id; return Guid.TryParse(value, out id) ? (Guid?)id : null; })
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();
                }
                catch (JsonException)
                {
                    return null;
                }
            }
            else
            {
                result.PrIds = draft.SelectedPrIds.Split(',')
                    .Select(value => { Guid id; return Guid.TryParse(value, out id) ? (Guid?)id : null; })
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();
            }
            return result;
        }

        public async Task<List<POWizardDraftListItemViewModel>> GetActiveWizardDraftsAsync(string user)
        {
            var drafts = await _db.PurchaseOrderWizardProgresses
                .Where(d => d.CreatedBy == user && !d.IsCompleted)
                .OrderByDescending(d => d.UpdatedAt ?? d.CreatedAt)
                .ToListAsync();

            if (!drafts.Any()) return new List<POWizardDraftListItemViewModel>();

            return drafts.Select(d => new POWizardDraftListItemViewModel
            {
                Id = d.Id,
                CreatedAt = d.CreatedAt,
                LastUpdatedAt = d.UpdatedAt ?? d.CreatedAt,
                CurrentStep = Math.Max(1, Math.Min(4, d.LastStep)),
                SelectedPRCount = CountSelectedPRs(d.SelectedPrIds),
                DraftName = d.DraftNo
            }).ToList();
        }

        public async Task<bool> DiscardWizardDraftAsync(Guid draftId, string user)
        {
            var draft = await _db.PurchaseOrderWizardProgresses
                .FirstOrDefaultAsync(d =>
                    d.Id == draftId &&
                    d.CreatedBy == user &&
                    !d.IsCompleted);

            if (draft == null) return false;
            draft.IsCompleted = true;
            draft.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        private static int CountSelectedPRs(string storedState)
        {
            if (String.IsNullOrWhiteSpace(storedState)) return 0;
            if (!storedState.TrimStart().StartsWith("{"))
            {
                return storedState.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Count(value => { Guid id; return Guid.TryParse(value, out id); });
            }

            try
            {
                var state = JObject.Parse(storedState);
                return (state["selectedPRIds"] ?? new JArray())
                    .Values<string>()
                    .Count(value => { Guid id; return Guid.TryParse(value, out id); });
            }
            catch (JsonException)
            {
                return 0;
            }
        }

        // ==========================================
        // EXISTING METHODS (Retained & Cleaned)
        // ==========================================

        public IQueryable<PurchaseOrderGridViewModel> GetPurchaseOrdersGrid()
        {
            return _db.Orders
                .Include(p => p.Supplier)
                .Include(p => p.OrderItems)
                .Select(p => new PurchaseOrderGridViewModel
                {
                    Id = p.Id,
                    PONumber = p.PoNo,
                    PODate = p.PoDate,
                    SupplierName = p.Supplier.Name,
                    SupplierId = p.SupplierId,
                    SourcePRs = p.PrNo,
                    ItemCount = p.OrderItems.Count,
                    TotalAmount = p.OrderItems.Sum(s => s.Amount) ?? 0,
                    Status = p.PostedDt != null ? "POSTED" : "DRAFT",
                    CreatedBy = p.InsertedBy,
                    DeliveryPeriodDays = p.DeliveryDate,
                    PlaceOfDelivery = p.DeliveryPlace,
                    PaymentTerms = p.TermPayment,
                    ModeOfProcurement = p.PoMode,
                    HasPOCopy = false,
                    AdditionalDocsCount = 0
                });
        }

        public async Task<PurchaseOrderDetailViewModel> GetPODetailsAsync(Guid id)
        {
            var po = await _db.Orders
                .Include(p => p.Supplier)
                .Include(p => p.OrderItems.Select(i => i.ItemCode.ItemType))
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
                SupplierContactPerson = "",
                SupplierContactNumber = po.Supplier.ContactNos,
                DeliveryPeriodDays = po.DeliveryDate,
                PlaceOfDelivery = po.DeliveryPlace,
                PaymentTerms = po.TermPayment,
                ModeOfProcurement = po.PoMode,
                BACResolutionNo = "",
                Status = po.PostedDt != null ? "POSTED" : "DRAFT",
                TotalAmount = po.OrderItems.Sum(s => s.Amount) ?? 0,
                CreatedBy = po.InsertedBy,
                CreatedAt = po.InsertedDt,
                PostedBy = po.PostedBy,
                PostedAt = po.PostedDt,
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
                        }).ToList()
                }).ToList(),
                Documents = new List<PODocumentViewModel>()
            };
        }

        public async Task<List<Order>> CreatePOsFromWizardAsync(List<POGroupDraftViewModel> poGroups, string user, bool isDraft)
        {
            var createdPOs = new List<Order>();

            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    foreach (var grp in poGroups)
                    {
                        var date = (DateTime?)DateTime.Now;
                        var poNumber = String.IsNullOrWhiteSpace(grp.PONumber)
                            ? await GeneratePONumberAsync()
                            : grp.PONumber.Trim();
                        var po = new Order
                        {
                            Id = Guid.NewGuid(),
                            PoNo = poNumber,
                            PrNo = grp.PRNumber,
                            Department = grp.DepartmentName,
                            CtrlNo = grp.CtrlNo,
                            PoDate = grp.PODate,
                            SupplierId = grp.SupplierId,
                            SupName = grp.SupplierName,
                            SupBusiness = grp.SupBusiness,
                            SupAddress = grp.SupAddress,
                            SupTIN = grp.SupTIN,
                            SupEmail = grp.SupEmail,
                            SupZipCode = grp.SupZipCode,
                            SupContactNo = grp.SupContactNo,
                            DeliveryDate = String.IsNullOrWhiteSpace(grp.DeliveryDate) ? grp.DeliveryPeriodDays : grp.DeliveryDate,
                            DeliveryPlace = grp.PlaceOfDelivery,
                            TermDelivery = grp.TermDelivery,
                            TermPayment = grp.PaymentTerms,
                            PoMode = grp.ModeOfProcurement,
                            SignedBySuppName = grp.SignedBySuppName,
                            SignedBySuppDate = grp.SignedBySuppDate,
                            SignedByAuthName = grp.SignedByAuthName,
                            SignedByAuthDesignation = grp.SignedByAuthDesignation,
                            ResoNo = grp.ResoNo,
                            CertifiedCorrectBy = grp.CertifiedCorrectBy,
                            CertifiedCorrectDate = grp.CertifiedCorrectDate,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date,
                            PostedBy = isDraft ? null : user,
                            PostedDt = isDraft ? null : date
                        };

                        if (grp.SourcePRs != null)
                        {
                            foreach (var sourcePR in grp.SourcePRs
                                .Where(source => source != null && source.PRId != Guid.Empty)
                                .GroupBy(source => source.PRId)
                                .Select(group => group.First()))
                            {
                                po.OrderRequests.Add(new OrderRequest
                                {
                                    Id = Guid.NewGuid(),
                                    PrId = sourcePR.PRId,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                });
                            }
                        }
                        int itemIndex = 1;
                        foreach (var itemDraft in grp.Items)
                        {
                            if (itemDraft == null || itemDraft.Quantity <= 0) continue;

                            var itemNo = String.IsNullOrWhiteSpace(itemDraft.ItemNo) ? itemIndex.ToString() : itemDraft.ItemNo;
                            itemIndex++;
                            var lineItem = new OrderItem
                            {
                                Id = Guid.NewGuid(),
                                ItemNo = itemNo,
                                ItemCodeId = itemDraft.ItemCodeId,
                                Description = itemDraft.Description,
                                PpmpCode = String.IsNullOrWhiteSpace(itemDraft.PpmpCode) ? itemDraft.ItemCode : itemDraft.PpmpCode,
                                PsNo = itemDraft.StockNo,
                                Qty = itemDraft.Quantity,
                                Unit = itemDraft.Unit,
                                UnitCost = itemDraft.UnitCost,
                                Amount = itemDraft.Quantity * itemDraft.UnitCost,
                                OtherDesc = itemDraft.TechnicalDescription,
                                InsertedBy = user,
                                InsertedDt = date,                                
                                UpdatedBy = user,
                                UpdatedDt = date,
                            };

                            if (itemDraft.SetLotItems != null)
                            {
                                var subItemIndex = 0;
                                foreach (var subItemDraft in itemDraft.SetLotItems)
                                {
                                    if (subItemDraft == null || subItemDraft.Qty < 0 || subItemDraft.EstimatedCost < 0) continue;
                                    subItemIndex++;
                                    var orderSubItem = new OrderSubItem
                                    {
                                        Id = Guid.NewGuid(),
                                        OrderItem = lineItem,
                                        ItemNo = String.IsNullOrWhiteSpace(subItemDraft.ItemNo) ? subItemIndex.ToString() : subItemDraft.ItemNo,
                                        ItemNoIndex = subItemIndex.ToString(),
                                        Description = subItemDraft.ItemName,
                                        Unit = subItemDraft.Unit,
                                        QtyPerSet = subItemDraft.Qty,
                                        TotalQty = subItemDraft.Qty * itemDraft.Quantity,
                                        UnitCost = subItemDraft.EstimatedCost,
                                        EstimatedTotalCost = subItemDraft.Qty * subItemDraft.EstimatedCost,
                                        SortOrder = subItemIndex,
                                        IsActive = true,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };
                                    if (subItemDraft.RequestSubItemId.HasValue)
                                    {
                                        orderSubItem.OrderSubItemRequests.Add(new OrderSubItemRequest
                                        {
                                            Id = Guid.NewGuid(),
                                            RequestSubItemId = subItemDraft.RequestSubItemId.Value,
                                            QtyApplied = subItemDraft.Qty,
                                            InsertedBy = user,
                                            InsertedDt = date,
                                            UpdatedBy = user,
                                            UpdatedDt = date
                                            
                                        });
                                    }
                                    lineItem.OrderSubItems.Add(orderSubItem);
                                }
                            }
                            if (itemDraft.Allocations != null)
                            {
                                foreach (var allocation in itemDraft.Allocations)
                                {
                                    lineItem.OrderItemRequests.Add(new OrderItemRequest
                                    {
                                        Id = Guid.NewGuid(),
                                        OrderItem = lineItem,
                                        RequestItemId = allocation.RequestItemId.Value,
                                        QtyApplied = allocation.Quantity,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    });
                                }
                            }
                            po.OrderItems.Add(lineItem);
                        }
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
            if (po == null || po.PostedDt != null) return false;

            po.PostedBy = user;
            po.PostedDt = DateTime.Now;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CancelPOAsync(Guid id, string user, string reason)
        {
            // Logic for cancellation (Reverting Qty, etc.)
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

    // Supporting DTO for draft retrieval
    public class WizardDraftDto
    {
        public Guid DraftId { get; set; }
        public string DraftNo { get; set; }
        public List<Guid> PrIds { get; set; }
        public int CurrentStep { get; set; }
        public string StateJson { get; set; }
    }
}
