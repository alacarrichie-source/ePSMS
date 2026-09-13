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
using iLgs.Services.Items;
using iLgs.Utilities;
using System.Globalization;
using System.Reflection;

namespace iLgs.Ai.Services.PurchaseOrder
{
    public interface IPurchaseOrderAiService
    {
        IQueryable<PurchaseOrderGridViewModel> GetPurchaseOrdersGrid();
        Task<PurchaseOrderDetailViewModel> GetPODetailsAsync(Guid id);
        Task<List<Order>> CreatePOsFromWizardAsync(List<POGroupDraftViewModel> poGroups, string user, bool isDraft);
        Task<POGroupDraftViewModel> GetExistingPOAsync(Guid id);
        Task<Order> SaveExistingPOAsync(Guid id, POGroupDraftViewModel group, string user, bool post);
        Task<string> ValidateWizardGroupsAsync(List<POGroupDraftViewModel> groups, bool posting, Guid? existingOrderId = null);
        Task<bool> PostPOAsync(Guid id, string user, string bacResolutionNo);
        Task<bool> CancelPOAsync(Guid id, string user, string reason);
        Task<string> GeneratePONumberAsync(DateTime poDate);

        // NEW: Wizard Draft Functionalities
        Task<Guid> SaveWizardProgressAsync(Guid? draftId, string stateJson, List<Guid> prIds, int currentStep, string user);
        Task<WizardDraftDto> GetWizardDraftAsync(Guid draftId, string user);
        Task<List<POWizardDraftListItemViewModel>> GetActiveWizardDraftsAsync(string user);
        Task<bool> DiscardWizardDraftAsync(Guid draftId, string user);
    }

    public partial class PurchaseOrderAiService : IPurchaseOrderAiService
    {
        private readonly AppManEntities _db;
        private readonly IItemCodeService _itemCodeService;

        private static readonly Dictionary<string, string[]> AllFieldNamesByPartial =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                { "_FieldAlcohol", new[] { "GenericName", "DosageVolume", "Multipliers", "Brand" } },
                { "_FieldDrugs", new[] { "GenericName", "DosageStrength", "DosageForm", "Multipliers", "Brand" } },
                { "_FieldLand", new[] { "Area" } },
                { "_FieldMultiple", new[] { "Multipliers" } },
                { "_FieldMultiple_A", new[] { "Multipliers", "Brand" } },
                { "_FieldBrand", new[] { "Multipliers", "Brand", "Model_", "Dimension", "Size", "Weight", "Materials", "Capacity", "Color" } },
                { "_FieldBrand_A", new[] { "Brand", "Model_", "Dimension", "Size", "Weight", "Materials", "Capacity", "Color" } },
                { "_FieldBrand_B", new[] { "Brand", "Model_", "Weight", "Color" } },
                { "_FieldSerial", new[] { "Multipliers", "Brand", "Model_", "Dimension", "Size", "Weight", "Materials", "Capacity", "Color", "PropNo", "SerialNo" } },
                { "_FieldSerial_A", new[] { "Multipliers", "Brand", "Model_", "Dimension", "Size", "Weight", "Materials", "Capacity", "Color", "MVFileNo", "BodyNo", "PlateNo" } },
                { "_FieldSerial_B", new[] { "Multipliers", "PropNo", "SerialNo" } },
                { "_FieldSerial_C", new[] { "Multipliers", "MVFileNo", "BodyNo", "PlateNo" } },
                { "_FieldSerial_D", new[] { "Brand", "Model_", "Dimension", "Size", "Weight", "Materials", "Capacity", "Color", "PropNo", "SerialNo" } },
                { "_FieldSerial_E", new[] { "MVFileNo", "BodyNo", "PlateNo" } },
                { "_FieldSerial_F", new[] { "PropNo", "SerialNo" } }
            };

        public PurchaseOrderAiService(AppManEntities db)
        {
            _db = db;
            _itemCodeService = new ItemCodeService(_db);
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
                    Status = p.PostedDt != null ? "Posted" : "Draft",
                    HasAIR = p.AIRs.Any(),
                    HasPostedAIR = p.AIRs.Any(a => a.PostedDt != null),
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
                .Include(p => p.OrderItems.Select(i => i.AllField))
                .Include(p => p.OrderItems.Select(i => i.OrderSubItems))
                .Include(p => p.OrderRequests.Select(r => r.Request))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (po == null) return null;

            return new PurchaseOrderDetailViewModel
            {
                Id = po.Id,
                PONumber = po.PoNo,
                PODate = po.PoDate,
                SupplierId = po.SupplierId,
                SupplierName = po.Supplier != null ? po.Supplier.Name : po.SupName,
                SupplierTIN = po.Supplier != null ? po.Supplier.TIN : po.SupTIN,
                SupplierAddress = po.Supplier != null ? po.Supplier.Address : po.SupAddress,
                SupplierContactPerson = "",
                SupplierContactNumber = po.Supplier != null ? po.Supplier.ContactNos : po.SupContactNo,
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
                    GSOCategory = li.ItemCode != null && li.ItemCode.ItemType != null
                        ? li.ItemCode.ItemType.Description
                        : String.Empty,
                    TechnicalDescription = li.OtherDesc,
                    AllFields = GetAllFieldValues(
                        li.AllField,
                        _itemCodeService.GetPartialView(li.ItemCodeId)),
                    SetLotItems = li.OrderSubItems
                        .OrderBy(s => s.ItemNo)
                        .Select(s => new POSetLotItemViewModel
                        {
                            ItemNo = s.ItemNo,
                            ItemName = s.Description,
                            Unit = s.Unit,
                            Qty = (int)(s.QtyPerSet),
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
                            ? await GeneratePONumberAsync(grp.PODate.Date)
                            : grp.PONumber.Trim();
                        var po = new Order
                        {
                            Id = Guid.NewGuid(),
                            PoNo = poNumber,
                            PrNo = grp.PRNumber,
                            Department = grp.DepartmentName,
                            CtrlNo = grp.CtrlNo,
                            PoDate = grp.PODate.Date,
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

                            var allField = await CreateValidatedAllFieldAsync(
                                itemDraft,
                                lineItem.Id,
                                user,
                                date);
                            if (allField != null)
                            {
                                lineItem.AllField = allField;
                            }

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

        private async Task<AllField> CreateValidatedAllFieldAsync(
            POLineItemDraftViewModel itemDraft,
            Guid orderItemId,
            string user,
            DateTime? date)
        {
            var submittedFields = itemDraft.AllFields;
            if ((submittedFields == null || submittedFields.Count == 0) &&
                itemDraft.AdditionalSpecs != null)
            {
                submittedFields = itemDraft.AdditionalSpecs;
            }

            if (submittedFields == null || submittedFields.Count == 0)
            {
                return null;
            }
            if (!itemDraft.ItemCodeId.HasValue)
            {
                throw new InvalidOperationException(
                    "An Article must be selected before its additional information can be saved.");
            }

            var partialView = await _itemCodeService.GetPartialViewAsync(itemDraft.ItemCodeId);
            var partialName = NormalizePartialName(partialView);
            string[] allowedNames;
            if (!AllFieldNamesByPartial.TryGetValue(partialName, out allowedNames))
            {
                throw new InvalidOperationException(
                    "The selected Article does not support the submitted additional information.");
            }

            var allowed = new HashSet<string>(allowedNames, StringComparer.OrdinalIgnoreCase);
            foreach (var submitted in submittedFields)
            {
                if (String.Equals(submitted.Key, "Id", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                if (!allowed.Contains(submitted.Key) &&
                    !String.IsNullOrWhiteSpace(submitted.Value))
                {
                    throw new InvalidOperationException(
                        "The field '" + submitted.Key +
                        "' is not valid for the selected Article.");
                }
            }

            var allField = new AllField
            {
                Id = orderItemId,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            foreach (var fieldName in allowedNames)
            {
                string value;
                if (!submittedFields.TryGetValue(fieldName, out value))
                {
                    continue;
                }
                SetAllFieldValue(allField, fieldName, value);
            }

            return allField;
        }

        private static string NormalizePartialName(string partialView)
        {
            if (String.IsNullOrWhiteSpace(partialView))
            {
                return String.Empty;
            }

            var normalized = partialView.Replace("\\", "/");
            var slashIndex = normalized.LastIndexOf('/');
            if (slashIndex >= 0)
            {
                normalized = normalized.Substring(slashIndex + 1);
            }
            if (normalized.EndsWith(".cshtml", StringComparison.OrdinalIgnoreCase))
            {
                normalized = normalized.Substring(0, normalized.Length - 7);
            }
            return normalized;
        }

        private static void SetAllFieldValue(
            AllField allField,
            string fieldName,
            string value)
        {
            var property = typeof(AllField).GetProperty(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            if (property == null || !property.CanWrite)
            {
                throw new InvalidOperationException(
                    "The additional-information field is not supported.");
            }

            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ??
                property.PropertyType;
            if (String.IsNullOrWhiteSpace(value) &&
                propertyType != typeof(string))
            {
                property.SetValue(allField, null);
                return;
            }

            object convertedValue;
            if (propertyType == typeof(string))
            {
                convertedValue = value;
            }
            else if (propertyType == typeof(int))
            {
                int number;
                if (!Int32.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number))
                {
                    throw new InvalidOperationException(
                        "The value for '" + fieldName + "' must be a whole number.");
                }
                convertedValue = number;
            }
            else if (propertyType == typeof(decimal))
            {
                decimal number;
                if (!Decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out number) &&
                    !Decimal.TryParse(value, NumberStyles.Number, CultureInfo.CurrentCulture, out number))
                {
                    throw new InvalidOperationException(
                        "The value for '" + fieldName + "' must be numeric.");
                }
                convertedValue = number;
            }
            else
            {
                throw new InvalidOperationException(
                    "The value type for '" + fieldName + "' is not supported.");
            }

            property.SetValue(allField, convertedValue);
        }

        public static List<POAllFieldValueViewModel> GetAllFieldValues(
            AllField allField)
        {
            return GetAllFieldValues(allField, null);
        }

        private static List<POAllFieldValueViewModel> GetAllFieldValues(
            AllField allField,
            string partialView)
        {
            var values = new List<POAllFieldValueViewModel>();
            if (allField == null)
            {
                return values;
            }

            IEnumerable<string> fieldNames;
            if (String.IsNullOrWhiteSpace(partialView))
            {
                fieldNames = AllFieldNamesByPartial
                    .SelectMany(partial => partial.Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase);
            }
            else
            {
                string[] applicableFieldNames;
                if (!AllFieldNamesByPartial.TryGetValue(
                    NormalizePartialName(partialView),
                    out applicableFieldNames))
                {
                    return values;
                }
                fieldNames = applicableFieldNames;
            }

            foreach (var fieldName in fieldNames)
            {
                var property = typeof(AllField).GetProperty(fieldName);
                var rawValue = property != null ? property.GetValue(allField) : null;
                if (rawValue == null || String.IsNullOrWhiteSpace(Convert.ToString(rawValue, CultureInfo.CurrentCulture)))
                {
                    continue;
                }

                values.Add(new POAllFieldValueViewModel
                {
                    FieldName = fieldName,
                    Label = Utility.GetDisplayName<AllField>(fieldName),
                    Value = Convert.ToString(rawValue, CultureInfo.CurrentCulture)
                });
            }
            return values;
        }

        public static Dictionary<string, string> GetAllFieldDictionary(
            AllField allField)
        {
            return GetAllFieldDictionary(allField, null);
        }

        public static Dictionary<string, string> GetAllFieldDictionary(
            AllField allField,
            string partialView)
        {
            return GetAllFieldValues(allField, partialView)
                .ToDictionary(
                    field => field.FieldName,
                    field => field.Value,
                    StringComparer.OrdinalIgnoreCase);
        }

        public async Task<bool> PostPOAsync(Guid id, string user, string bacResolutionNo)
        {
            var group = await GetExistingPOAsync(id);
            if (!String.IsNullOrWhiteSpace(bacResolutionNo)) group.ResoNo = bacResolutionNo;
            await SaveExistingPOAsync(id, group, user, true);
            return true;
        }

        public async Task<bool> CancelPOAsync(Guid id, string user, string reason)
        {
            await new iLgs.Services.PurchaseOrder.PurchaseOrderLifecycleService(_db).EnsureEditableAsync(id);
            throw new InvalidOperationException("PO cancellation is not implemented. Use Delete for an eligible draft PO.");
        }

        public async Task<string> ValidateWizardGroupsAsync(List<POGroupDraftViewModel> groups, bool posting, Guid? existingOrderId = null)
        {
            if (groups == null || groups.Count == 0 || groups.Any(g => g == null)) return "No valid PO groups were submitted.";
            if (groups.Count > 50) return "Too many PO groups were submitted.";
            var allocatedQuantities = new Dictionary<Guid, int>();
            var poNumbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                var name = String.IsNullOrWhiteSpace(group.GroupName) ? "PO group" : group.GroupName;
                if (group.Items == null || !group.Items.Any()) return name + " has no line items.";
                if (group.Items.Any(i => i == null || i.Quantity <= 0 || i.UnitCost < 0 || String.IsNullOrWhiteSpace(i.Description)))
                    return name + " contains an invalid line item.";
                foreach (var line in group.Items) {
                    if (line.Allocations == null || line.Allocations.Count == 0 ||
                        line.Allocations.Any(a => a == null || !a.RequestItemId.HasValue || a.Quantity <= 0) ||
                        line.Allocations.Sum(a => a.Quantity) != line.Quantity ||
                        line.Allocations.GroupBy(a => a.RequestItemId).Any(a => a.Count() > 1))
                        return "Every PO line must retain valid source PR allocations matching its quantity.";
                    if (line.SetLotItems != null && line.SetLotItems.Any(s => s == null || s.Qty < 0 || s.EstimatedCost < 0))
                        return "A set/lot component has an invalid quantity or cost.";
                }
                if (posting) {
                if (String.IsNullOrWhiteSpace(group.PONumber))
                    return "Enter the PO number for " + name + ".";
                if (!existingOrderId.HasValue && !System.Text.RegularExpressions.Regex.IsMatch(group.PONumber.Trim(), @"^\d{4}-\d{2}-\d{4}$"))
                    return "Enter the PO number for " + name + " in YYYY-MM-9999 format.";
                if (!poNumbers.Add(group.PONumber.Trim()))
                    return "The PO number " + group.PONumber.Trim() + " is used by more than one PO group.";
                if (await _db.Orders.AnyAsync(o => o.PoNo == group.PONumber.Trim() && (!existingOrderId.HasValue || o.Id != existingOrderId.Value)))
                    return "The PO number " + group.PONumber.Trim() + " already exists.";
                if (!group.SupplierId.HasValue)
                    return "Select a valid supplier for " + name + ".";
                var supplier = await _db.Suppliers.FindAsync(group.SupplierId.Value);
                if (supplier == null)
                    return "Select a valid supplier for " + name + ".";
                group.SupplierName = supplier.Name;
                group.SupBusiness = supplier.BusinessName;
                group.SupAddress = supplier.Address;
                group.SupTIN = supplier.TIN;
                group.SupEmail = supplier.Email;
                group.SupZipCode = supplier.ZipCode;
                group.SupContactNo = supplier.ContactNos;
                if (String.IsNullOrWhiteSpace(group.PlaceOfDelivery) || String.IsNullOrWhiteSpace(group.TermDelivery) ||
                    String.IsNullOrWhiteSpace(group.PaymentTerms) || String.IsNullOrWhiteSpace(group.ModeOfProcurement))
                    return "Complete the delivery and procurement terms for " + name + ".";
                if (group.POCopyDoc == null || (!group.POCopyDoc.ExistingUploadId.HasValue && String.IsNullOrWhiteSpace(group.POCopyDoc.FilePath)))
                    return "Upload the signed PO copy for " + name + ".";
                }
                foreach (var item in group.Items)
                {
                    if (!item.ItemCodeId.HasValue ||
                        !await _db.ItemCodes.AnyAsync(code => code.Id == item.ItemCodeId.Value))
                    {
                        return "Select a valid Article for every item in " + name + ".";
                    }
                    if (item.Allocations == null || !item.Allocations.Any()) return "Source PR allocations are missing for an item in " + name + ".";
                    if (item.Allocations.Any(a => !a.RequestItemId.HasValue || a.Quantity <= 0) || item.Allocations.Sum(a => a.Quantity) != item.Quantity)
                        return "A source PR allocation is invalid for an item in " + name + ".";
                    foreach (var allocation in item.Allocations)
                    {
                        var requestItem = await _db.RequestItems.FindAsync(allocation.RequestItemId.Value);
                        if (requestItem == null)
                            return "A source Purchase Request item is no longer available.";
                        var request = await _db.Requests.FindAsync(requestItem.PrId);
                        if (request == null || request.PostedDt == null) return "A source Purchase Request is no longer approved.";
                        if ((requestItem.UnitCost ?? 0) != item.UnitCost)
                            return "A submitted unit cost no longer matches its approved Purchase Request.";
                        var requestItemId = allocation.RequestItemId.Value;
                        allocatedQuantities[requestItemId] = (allocatedQuantities.ContainsKey(requestItemId) ? allocatedQuantities[requestItemId] : 0) + allocation.Quantity;
                        var usedElsewhere = await _db.OrderItemRequests.Where(a => a.RequestItemId == requestItemId &&
                            (!existingOrderId.HasValue || a.OrderItem.OrderId != existingOrderId.Value))
                            .Select(a => a.QtyApplied).SumAsync() ?? 0;
                        if (allocatedQuantities[requestItemId] + usedElsewhere > (requestItem.Qty ?? 0))
                            return "A Purchase Request item quantity has been over-allocated.";
                    }
                }
            }
            return null;
        }


        public async Task<string> GeneratePONumberAsync(DateTime poDate)
        {
            //var currentYear = poDate.Year;
            //var prefix = $"PO-{currentYear}-";
            //var count = await _db.Orders.CountAsync(p => p.PoNo.StartsWith(prefix)) + 1;
            //return $"{prefix}{count:D3}";

            string yyyy = poDate.Year.ToString().Trim();
            string mm = poDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var order = await _db.Orders.Where(w => w.CtrlNo.Substring(0, 4) == yyyy).OrderByDescending(o => o.CtrlNo).FirstOrDefaultAsync();
            if (order == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(order.CtrlNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
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
