using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace iLgs.Services.Requisition
{
    public interface IRisItemService
    {
        IQueryable<RisItemVM> GetByRisId(Guid? risId);
        Task<RisItemVM> GetByIdAsync(Guid? id);
        //RisItemVM GetVmById(Guid? id);
        //RisItemVM GetEntryVmById(Guid? id);
        //string PsNoDisplay(RisItemVM model);
        //Task<string> GetDescriptionAsync(RisItemVM entry);        

        //ValueTask<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date);
        //ValueTask<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date);
        //ValueTask<RisItemVM> DeleteAsync(RisItemVM model, string user, DateTime date);
    }

    internal class RisItemService : IRisItemService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RisItemVM> _vmExceptionService;
        //private readonly IExceptionService<RisItem> _exceptionService;
        //private readonly IItemCodeService _itemCodeService;
        //private readonly IAllFieldService _allFieldService;
        //private readonly IRisItemValidator _validator;

        public RisItemService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<RisItemVM>();
            //_exceptionService = new ExceptionService<RisItem>();
            //_itemCodeService = new ItemCodeService(_db);
            //_allFieldService = new AllFieldService(_db);
            //_validator = new RisItemValidator(_db);
        }

        //public RisItemService(AppManEntities db,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<RisItemVM> vmExceptionService,
        //    IExceptionService<RisItemVM> entryVmExceptionService,
        //    IExceptionService<RisItem> exceptionService,
        //    IItemCodeService itemCodeService,
        //    IAllFieldService allFieldService,
        //    IRisItemValidator validator)
        //{
        //    _db = db;
        //    _exceptions = exceptions;
        //    _vmExceptionService = vmExceptionService;
        //    _entryVmExceptionService = entryVmExceptionService;
        //    _exceptionService = exceptionService;
        //    _itemCodeService = itemCodeService;
        //    _allFieldService = allFieldService;
        //    _validator = validator;
        //}

        private Expression<Func<RisItem, RisItemVM>> Projection()
        {
            return s => new RisItemVM
            {
                Id = s.Id,
                RisId = s.RisId,
                OrderItemRequestId = s.OrderItemRequestId,
                ItemNo = s.OrderItemRequest.OrderItem.ItemNo,
                ItemNoIndex = s.OrderItemRequest.OrderItem.ItemNoIndex,
                ItemName = s.OrderItemRequest.OrderItem.ItemName,
                Category = s.OrderItemRequest.OrderItem.ItemCode.ItemType.Category,
                PsType = s.OrderItemRequest.OrderItem.ItemCode.ItemType.Code,
                PsNo = s.OrderItemRequest.OrderItem.PsNo,
                PsNoDisplay = s.OrderItemRequest.OrderItem.PsNoDisplay,
                PsItem = s.OrderItemRequest.OrderItem.ItemCode.Description,
                Description = s.OrderItemRequest.OrderItem.Description,
                OtherDesc = s.OrderItemRequest.OrderItem.OtherDesc,
                Unit = s.OrderItemRequest.OrderItem.Unit,
                QtyRequest = s.QtyRequest,
                //QtyIssue = s.QtyIssue,
                Remarks = s.Remarks,                
                InsertedDt = s.InsertedDt,
                IsPosted = s.RISs.PostedDt != null,
                //SetLotNo = s.RisItemUnitGroupDescriptionItems.FirstOrDefault().RisItemUnitGroupDescription.RisItemUnitGroup.SetLotNo,
                Padding = (s.OrderItemRequest.OrderItem.ItemNo.Length - s.OrderItemRequest.OrderItem.ItemNo.Replace(".", "").Length) * 20,
                //IsSetLot = s.OrderItemRequest.OrderItem.Unit == "set" || s.OrderItemRequest.OrderItem.Unit == "lot" ? true : false,
                //IsSetLotItem = s.OrderItemRequest.OrderItem.ItemNo.Contains("."),
                RIS = s.RISs
            };
        }

        //public RisItemVM GetVmById(Guid? id) 
        //{
        //    var data =  _db.RisItems.AsNoTracking().Where(w => w.Id == id)
        //        .Select(s => new RisItemVM
        //        {
        //            Id = s.Id,
        //            RisId = s.RisId,
        //            ItemNo = s.OrderItemRequest.OrderItem.ItemNo,
        //            ItemNoIndex = s.OrderItemRequest.OrderItem.ItemNoIndex,
        //            ItemCodeId = s.ItemCodeId,
        //            ItemCode = s.ItemCode.Code,                    
        //            ItemType = s.ItemCode.Description,
        //            Category = s.ItemCode.ItemType.Category,
        //            PsType = s.ItemCode.ItemType.Code,
        //            PsTypeDesc = s.ItemCode.ItemType.Description,
        //            PsNo = s.PsNo,
        //            PsNoDisplay = s.PsNoDisplay,
        //            SubAccountCode = s.SubAccountCode,
        //            Unit = s.Unit,
        //            ItemName = s.ItemName,
        //            Description = s.Description,
        //            OtherDesc = s.OtherDesc,
        //            QtyRequest = s.QtyRequest,
        //            QtyIssue = s.QtyIssue,
        //            Remarks = s.Remarks,
        //            InsertedDt = s.InsertedDt,
        //            Department = s.RISs.Office,
        //            IsPosted = s.RISs.PostedDt != null,
        //            AllField = s.AllField,
        //            SetLotNo = s.RisItemUnitGroupDescriptionItems.FirstOrDefault().RisItemUnitGroupDescription.RisItemUnitGroup.SetLotNo,
        //            PpmpCode = s.PpmpCode                    
        //        }).ToList()
        //        .Select(s => new RisItemVM {
        //            Id = s.Id,
        //            RisId = s.RisId,
        //            ItemNo = s.ItemNo,
        //            ItemCodeId = s.ItemCodeId,
        //            ItemCode = s.ItemCode,                    
        //            ItemType = s.ItemType,
        //            Category = s.Category,
        //            PsType = s.PsType,
        //            PsTypeDesc = s.PsTypeDesc,
        //            PsNo = s.PsNo,
        //            PsNoDisplay = s.PsNoDisplay,
        //            SubAccountCode = s.SubAccountCode,
        //            SubAccount = _itemCodeService.GetSubAccounts(s.ItemCodeId), // Call the service on the in-memory data
        //            Unit = s.Unit,
        //            ItemName = s.ItemName,
        //            Description = s.Description,
        //            OtherDesc = s.OtherDesc,
        //            QtyRequest = s.QtyRequest,
        //            QtyIssue = s.QtyIssue,
        //            Remarks = s.Remarks,
        //            InsertedDt = s.InsertedDt,
        //            Department = s.Department,
        //            IsPosted = s.IsPosted,
        //            AllField = s.AllField,
        //            SetLotNo = s.SetLotNo,
        //            PpmpCode = s.PpmpCode,
        //            Padding = s.ItemNo.Count(c => c == '.') * 20
        //        }).FirstOrDefault();
        //    return data;
        //}

        //public RisItemVM GetEntryVmById(Guid? id) 
        //{
        //    var data = _db.RisItems.AsNoTracking().Where(w => w.Id == id)
        //        .Select(s => new RisItemVM
        //        {
        //            Id = s.Id,
        //            RisId = s.RisId,
        //            ItemNo = s.OrderItemRequest.OrderItem.ItemNo,
        //            ItemCodeId = s.ItemCodeId,
        //            ItemCode = s.ItemCode.Code,
        //            ItemType = s.ItemCode.Description,
        //            Category = s.ItemCode.ItemType.Category,
        //            PsType = s.ItemCode.ItemType.Code,
        //            PsTypeDesc = s.ItemCode.ItemType.Description,
        //            PsNo = s.PsNo,
        //            PsNoDisplay = s.PsNoDisplay,
        //            SubAccountCode = s.SubAccountCode,
        //            Unit = s.Unit,
        //            ItemName = s.ItemName,
        //            Description = s.Description,
        //            OtherDesc = s.OtherDesc,
        //            QtyRequest = s.QtyRequest,
        //            QtyIssue = s.QtyIssue,
        //            Remarks = s.Remarks,
        //            InsertedDt = s.InsertedDt,
        //            Department = s.RISs.Office,
        //            IsPosted = s.RISs.PostedDt != null,
        //            AllField = s.AllField,
        //            SetLotNo = s.RisItemUnitGroupDescriptionItems.FirstOrDefault().RisItemUnitGroupDescription.RisItemUnitGroup.SetLotNo,
        //            PpmpCode = s.PpmpCode
        //        }).ToList()
        //        .Select(s => new RisItemVM
        //        {
        //            Id = s.Id,
        //            RisId = s.RisId,
        //            ItemNo = s.ItemNo,
        //            ItemCodeId = s.ItemCodeId,
        //            ItemCode = s.ItemCode,                    
        //            ItemType = s.ItemType,
        //            Category = s.Category,
        //            PsType = s.PsType,
        //            PsTypeDesc = s.PsTypeDesc,
        //            PsNo = s.PsNo,
        //            PsNoDisplay = s.PsNoDisplay,
        //            SubAccountCode = s.SubAccountCode,
        //            SubAccount = _itemCodeService.GetSubAccounts(s.ItemCodeId), // Call the service on the in-memory data
        //            Unit = s.Unit,
        //            ItemName = s.ItemName,
        //            Description = s.Description,
        //            OtherDesc = s.OtherDesc,
        //            QtyRequest = s.QtyRequest,
        //            QtyIssue = s.QtyIssue,
        //            Remarks = s.Remarks,
        //            InsertedDt = s.InsertedDt,
        //            Department = s.Department,
        //            IsPosted = s.IsPosted,
        //            AllField = s.AllField,
        //            SetLotNo = s.SetLotNo,
        //            PpmpCode = s.PpmpCode
        //        }).FirstOrDefault();
        //    return data;
        //}

        public async Task<RisItemVM> GetByIdAsync(Guid? id) 
        {
            var data = await _db.RisItems.AsNoTracking().Where(w => w.Id == id).Select(Projection()).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<RisItemVM> GetByRisId(Guid? risId)
        {
            var data = _db.RisItems.AsNoTracking().Where(w => w.RisId == risId).Select(Projection());
            return data;
        }

        //public IQueryable<RisItemVM> GetByRisId(Guid? risId) =>
        //_vmExceptionService.TryCatch(() =>
        //{
        //    var data = _db.RisItems.AsNoTracking().Where(w => w.RisId == risId)
        //        .Select(s => new RisItemVM
        //        {
        //            Id = s.Id,
        //            RisId = s.RisId,
        //            ItemNo = s.OrderItemRequest.OrderItem.ItemNo,
        //            ItemCodeId = s.ItemCodeId,
        //            ItemCode = s.ItemCode.Code,
        //            ItemType = s.ItemCode.Description,
        //            Category = s.ItemCode.ItemType.Category,
        //            PsType = s.ItemCode.ItemType.Code,
        //            PsTypeDesc = s.ItemCode.ItemType.Description,
        //            PsNo = s.PsNo,
        //            PsNoDisplay = s.PsNoDisplay,
        //            SubAccountCode = s.SubAccountCode,
        //            Unit = s.Unit,
        //            ItemName = s.ItemName,
        //            Description = s.Description,
        //            OtherDesc = s.OtherDesc,
        //            QtyRequest = s.QtyRequest,
        //            QtyIssue = s.QtyIssue,
        //            Remarks = s.Remarks,
        //            InsertedDt = s.InsertedDt,
        //            Department = s.RISs.Office,
        //            IsPosted = s.RISs.PostedDt != null,
        //            AllField = s.AllField,
        //            SetLotNo = s.RisItemUnitGroupDescriptionItems.FirstOrDefault().RisItemUnitGroupDescription.RisItemUnitGroup.SetLotNo,
        //            PpmpCode = s.PpmpCode,
        //            ItemNoIndex = s.OrderItemRequest.OrderItem.ItemNoIndex
        //        }).ToList()
        //        .Select(s => new RisItemVM
        //        {
        //            Id = s.Id,
        //            RisId = s.RisId,
        //            ItemNo = s.ItemNo,
        //            ItemCodeId = s.ItemCodeId,
        //            ItemCode = s.ItemCode,
        //            ItemType = s.ItemType,
        //            Category = s.Category,
        //            PsType = s.PsType,
        //            PsTypeDesc = s.PsTypeDesc,
        //            PsNo = s.PsNo,
        //            PsNoDisplay = s.PsNoDisplay,
        //            SubAccountCode = s.SubAccountCode,
        //            SubAccount = _itemCodeService.GetSubAccounts(s.ItemCodeId), // Call the service on the in-memory data
        //            Unit = s.Unit,
        //            ItemName = s.ItemName,
        //            Description = s.Description,
        //            OtherDesc = s.OtherDesc,
        //            QtyRequest = s.QtyRequest,
        //            QtyIssue = s.QtyIssue,
        //            Remarks = s.Remarks,
        //            InsertedDt = s.InsertedDt,
        //            Department = s.Department,
        //            IsPosted = s.IsPosted,
        //            AllField = s.AllField,
        //            SetLotNo = s.SetLotNo,
        //            PpmpCode = s.PpmpCode,
        //            Padding = s.ItemNo.Count(c => c == '.') * 20,
        //            ItemNoIndex = s.ItemNoIndex
        //        }).AsQueryable();
        //    return data;
        //});

        private void ValidateFields(RisItemVM model)
        {
            if (!model.QtyRequest.HasValue || model.QtyRequest == 0)
            {
                throw new InvalidValueException("Quantity Request is Required!");
            }
        }

        //public ValueTask<RisItemVM> CreateAsync(RisItemVM model, string user, DateTime date) =>
        //_vmExceptionService.TryCatch(async () =>
        //{
        //    await _validator.ValidateOnCreateAsync(model);

        //    model.Id = Guid.NewGuid();
        //    model.InsertedBy = user;
        //    model.UpdatedBy = user;
        //    model.InsertedDt = date;
        //    model.UpdatedDt = date;

        //    model.PsNo = await PsNoAsync(model);
        //    model.PsNoDisplay = PsNoDisplay(model);

        //    var entity = new RisItem()
        //    {
        //        Id = model.Id,
        //        RisId = model.RisId,
        //        OrderItemRequestId = model.OrderItemRequestId,
        //        ItemCodeId = model.ItemCodeId,
        //        SubAccountCode = model.SubAccountCode,
        //        PsNo = model.PsNo,
        //        PsNoDisplay = model.PsNoDisplay,
        //        ItemName = model.ItemType,                                
        //        Description = model.Description,
        //        OtherDesc = model.OtherDesc,
        //        Unit = model.Unit,
        //        QtyRequest = model.QtyRequest,
        //        QtyIssue = model.QtyIssue,
        //        Remarks = model.Remarks ?? "",
        //        PpmpCode = model.PpmpCode,
        //        InsertedBy = model.InsertedBy,
        //        InsertedDt = model.InsertedDt,
        //        UpdatedBy = model.UpdatedBy,
        //        UpdatedDt = model.UpdatedDt,                
        //    };

        //    model.AllField.Id = entity.Id;
        //    entity.AllField = model.AllField;
        //    entity.AllField.InsertedBy = user;
        //    entity.AllField.UpdatedBy = user;
        //    entity.AllField.InsertedDt = date;
        //    entity.AllField.UpdatedDt = date;

        //    _db.RisItems.Add(entity);
        //    await _db.SaveChangesAsync();

        //    return model;
        //});

        //public ValueTask<RisItemVM> DeleteAsync(RisItemVM model, string user, DateTime date) =>
        //_entryVmExceptionService.TryCatch(async () =>
        //{
        //    await _validator.ValidateOnDeleteAsync(model);

        //    model.UpdatedBy = user;
        //    model.UpdatedDt = date;

        //    var entity = await _db.RisItems
        //        .Include(i => i.AllField)
        //        .Where(w => w.Id == model.Id).FirstOrDefaultAsync();

        //    entity.UpdatedBy = model.UpdatedBy;
        //    entity.UpdatedDt = model.UpdatedDt;

        //    _db.RisItems.Attach(entity);
        //    _db.Entry(entity).State = EntityState.Modified;
        //    await _db.SaveChangesAsync();

        //    _db.RisItems.Remove(entity);
        //    _db.Entry(entity).State = EntityState.Deleted;
        //    await _db.SaveChangesAsync();

        //    return model;
        //});

        //public ValueTask<RisItemVM> UpdateAsync(RisItemVM model, string user, DateTime date) =>
        //_entryVmExceptionService.TryCatch(async () =>
        //{
        //    await _validator.ValidateOnUpdateAsync(model);

        //    model.UpdatedBy = user;
        //    model.UpdatedDt = date;

        //    var entity = await _db.RisItems
        //        .Include(i => i.AllField)
        //        .Where(w => w.Id == model.Id).FirstOrDefaultAsync();

        //    model.PsNo = await PsNoAsync(model);
        //    model.PsNoDisplay = PsNoDisplay(model);

        //    entity.RisId = model.RisId;
        //    entity.ItemCodeId = model.ItemCodeId;
        //    entity.PsNo = model.PsNo;
        //    entity.PsNoDisplay = model.PsNoDisplay;
        //    entity.ItemName = model.ItemType;
        //    entity.SubAccountCode = model.SubAccountCode;
        //    entity.Unit = model.Unit;
        //    entity.Description = model.Description;
        //    entity.OtherDesc = model.OtherDesc;
        //    entity.QtyRequest = model.QtyRequest;
        //    entity.QtyIssue = model.QtyIssue;
        //    entity.Remarks = model.Remarks ?? "";
        //    entity.PpmpCode = model.PpmpCode;
        //    entity.UpdatedBy = model.UpdatedBy;
        //    entity.UpdatedDt = model.UpdatedDt;

        //    //entity = SetItemEntity(entity, model);

        //    model.AllField.Id = entity.Id;
        //    entity.AllField = model.AllField;
        //    entity.AllField.UpdatedBy = user;
        //    entity.AllField.UpdatedDt = date;

        //    _db.RisItems.Attach(entity);
        //    _db.Entry(entity).State = EntityState.Modified;
            
        //    // cascade updates
        //    // PR, Qty
        //    // PO, Description, Qty
        //    // AIR, Qty            

        //    //var prItem = await _db.RequestItems.FirstOrDefaultAsync(f => f.RisItemId == model.Id);
        //    //if (prItem != null)
        //    //{
        //    //    prItem.Qty = model.QtyRequest;
        //    //    prItem.TotalCost = model.QtyRequest * prItem.UnitCost;
        //    //    _db.RequestItems.Attach(prItem);
        //    //    _db.Entry(prItem).State = EntityState.Modified;

        //    //    var poItem = await _db.OrderItems.FirstOrDefaultAsync(f => f.RequestItemId == prItem.Id);
        //    //    if (poItem != null)
        //    //    {
        //    //        poItem.Qty = model.QtyRequest;
        //    //        poItem.Description = model.Description;
        //    //        poItem.Amount = model.QtyRequest * poItem.UnitCost;
        //    //        poItem.Unit = model.Unit;
        //    //        _db.OrderItems.Attach(poItem);
        //    //        _db.Entry(poItem).State = EntityState.Modified;

        //    //        var airItem = await _db.AIRItems.FirstOrDefaultAsync(f => f.OrderItemId == poItem.Id);
        //    //        airItem.Qty = model.QtyRequest;
        //    //        _db.AIRItems.Attach(airItem);
        //    //        _db.Entry(airItem).State = EntityState.Modified;
        //    //    }
        //    //}

        //    await _db.SaveChangesAsync();

        //    return model;
        //});        

        //private Task<string> PsNoAsync(RisItemVM fields)
        //{
        //    return _allFieldService.GetRisStockNoAsync(fields);
        //}

        //public string PsNoDisplay(RisItemVM model)
        //{
        //    string display = "";
        //    if (Enum.TryParse(model.PsType, out Category c))
        //    {
        //        if (c == CatLandsProp())
        //        {
        //            display = $"{model.ItemCode}/{model.AllField.Area}SqM";
        //        }
        //        else
        //        {
        //            display = $"{model.ItemCode}";
        //        }
        //    }
        //    return display;
        //}

        //public Task<string> GetDescriptionAsync(RisItemVM entry)
        //{
        //    return _allFieldService.GetRisDescriptionAsync(entry);
        //}
    }
}