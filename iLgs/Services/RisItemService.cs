using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public class RisItemService : IRisItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemVM> _vmExceptionService = new ExceptionService<RisItemVM>();
        private readonly IExceptionService<RisItemEntryVM> _entryVmExceptionService = new ExceptionService<RisItemEntryVM>();
        private readonly IExceptionService<RisItem> _exceptionService = new ExceptionService<RisItem>();
        private IAllFieldService _allFieldService;

        public RisItemService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
        }

        public ValueTask<RisItemEntryVM> GetVmByIdAsync(Guid? id) =>
        _entryVmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisItems.Where(w => w.Id == id)
                .Select(s => new RisItemEntryVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    ItemCodeId = s.ItemCodeId,
                    ItemCode = s.ItemCode.Code,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemType = s.ItemCode.Description,
                    PsType = s.ItemCode.ItemType.Code,
                    PsTypeDesc = s.ItemCode.ItemType.Description,
                    PsNo = s.PsNo,
                    PsNoDisplay = s.PsNoDisplay,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.ItemNo == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt,
                    Department = s.RISs.Office,
                    IsPosted = s.RISs.PostedDt != null,
                    //FieldsAccountableForm = s.FieldsAccountableForm,
                    //FieldsAgricultural = s.FieldsAgricultural,
                    //FieldsAnimal = s.FieldsAnimal,
                    //FieldsFurniture = s.FieldsFurniture,
                    //FieldsLand = s.FieldsLand,
                    //FieldsMachinery = s.FieldsMachinery,
                    //FieldsMedical = s.FieldsMedical,
                    //FieldsMedicine = s.FieldsMedicine,
                    //FieldsMilitarySuuply = s.FieldsMilitarySuuply,
                    //FieldsNonAccountableForm = s.FieldsNonAccountableForm,
                    //FieldsOfficeSupply = s.FieldsOfficeSupply,
                    //FieldsOther = s.FieldsOther,
                    //FieldsOtherSupplyMaterial = s.FieldsOtherSupplyMaterial,
                    //FieldsRepair = s.FieldsRepair,
                    //FieldsTransportation = s.FieldsTransportation,
                    //FieldsVehicle = s.FieldsVehicle,
                    //FieldsConstruction = s.FieldsConstruction,
                    AllField = s.AllField,
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<RisItemEntryVM> GetEntryVmByIdAsync(Guid? id) =>
        _entryVmExceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisItems.Where(w => w.Id == id)
                .Select(s => new RisItemEntryVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    ItemCodeId = s.ItemCodeId,
                    ItemCode = s.ItemCode.Code,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemType = s.ItemCode.Description,
                    PsType = s.ItemCode.ItemType.Code,
                    PsTypeDesc = s.ItemCode.ItemType.Description,
                    PsNo = s.PsNo,
                    PsNoDisplay = s.PsNoDisplay,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.ItemNo == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt,
                    Department = s.RISs.Office,
                    IsPosted = s.RISs.PostedDt != null,
                    //FieldsAccountableForm = s.FieldsAccountableForm,
                    //FieldsAgricultural = s.FieldsAgricultural,
                    //FieldsAnimal = s.FieldsAnimal,
                    //FieldsFurniture = s.FieldsFurniture,
                    //FieldsLand = s.FieldsLand,
                    //FieldsMachinery = s.FieldsMachinery,
                    //FieldsMedical = s.FieldsMedical,
                    //FieldsMedicine = s.FieldsMedicine,
                    //FieldsMilitarySuuply = s.FieldsMilitarySuuply,
                    //FieldsNonAccountableForm = s.FieldsNonAccountableForm,
                    //FieldsOfficeSupply = s.FieldsOfficeSupply,
                    //FieldsOther = s.FieldsOther,
                    //FieldsOtherSupplyMaterial = s.FieldsOtherSupplyMaterial,
                    //FieldsRepair = s.FieldsRepair,
                    //FieldsTransportation = s.FieldsTransportation,
                    //FieldsVehicle = s.FieldsVehicle,
                    //FieldsConstruction = s.FieldsConstruction,
                    AllField = s.AllField
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<RisItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.RisItems.FindAsync(id);
            return data;
        });

        public IQueryable<RisItemEntryVM> GetByRisId(Guid? risId) =>
        _entryVmExceptionService.TryCatch(() =>
        {
            var data = _db.RisItems.Where(w => w.RisId == risId)
                .Select(s => new RisItemEntryVM
                {
                    Id = s.Id,
                    RisId = s.RisId,
                    ItemCodeId = s.ItemCodeId,
                    ItemCode = s.ItemCode.Code,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemType = s.ItemCode.Description,
                    PsType = s.ItemCode.ItemType.Code,
                    PsTypeDesc = s.ItemCode.ItemType.Description,
                    PsNo = s.PsNo,
                    PsNoDisplay = s.PsNoDisplay,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.ItemNo == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Unit = s.Unit,
                    ItemName = s.ItemName,
                    Description = s.Description,
                    OtherDesc = s.OtherDesc,
                    QtyRequest = s.QtyRequest,
                    QtyIssue = s.QtyIssue,
                    Remarks = s.Remarks,
                    InsertedDt = s.InsertedDt,
                    Department = s.RISs.Office,
                    IsPosted = s.RISs.PostedDt != null,
                    //FieldsAccountableForm = s.FieldsAccountableForm,
                    //FieldsAgricultural = s.FieldsAgricultural,
                    //FieldsAnimal = s.FieldsAnimal,
                    //FieldsFurniture = s.FieldsFurniture,
                    //FieldsLand = s.FieldsLand,
                    //FieldsMachinery = s.FieldsMachinery,
                    //FieldsMedical = s.FieldsMedical,
                    //FieldsMedicine = s.FieldsMedicine,
                    //FieldsMilitarySuuply = s.FieldsMilitarySuuply,
                    //FieldsNonAccountableForm = s.FieldsNonAccountableForm,
                    //FieldsOfficeSupply = s.FieldsOfficeSupply,
                    //FieldsOther = s.FieldsOther,
                    //FieldsOtherSupplyMaterial = s.FieldsOtherSupplyMaterial,
                    //FieldsRepair = s.FieldsRepair,
                    //FieldsTransportation = s.FieldsTransportation,
                    //FieldsVehicle = s.FieldsVehicle,
                    //FieldsConstruction = s.FieldsConstruction,
                    AllField = s.AllField
                });
            return data;
        });

        public ValueTask<RisItemEntryVM> CreateAsync(RisItemEntryVM model, string user, DateTime date) =>
        _entryVmExceptionService.TryCatchAsync(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            model.PsNo = PsNo(model);
            model.PsNoDisplay = PsNoDisplay(model);            

            var entity = new RisItem()
            {
                Id = model.Id,
                RisId = model.RisId,
                ItemCodeId = model.ItemCodeId,
                PsNo = model.PsNo,
                PsNoDisplay = model.PsNoDisplay,
                ItemName = model.ItemType,
                Unit = model.Unit,
                SubAccountCode = model.SubAccountCode,
                Description = model.Description,
                OtherDesc = model.OtherDesc,
                QtyRequest = model.QtyRequest,
                QtyIssue = model.QtyIssue,
                Remarks = model.Remarks ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt                
            };

            //entity = SetItemEntity(entity, model);
            model.AllField.Id = entity.Id;
            entity.AllField = model.AllField;
            entity.AllField.InsertedBy = user;
            entity.AllField.UpdatedBy = user;
            entity.AllField.InsertedDt = date;
            entity.AllField.UpdatedDt = date;

            _db.RisItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemEntryVM> DeleteAsync(RisItemEntryVM model, string user, DateTime date) =>
        _entryVmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            var entity = await _db.RisItems
                .Include(i => i.AllField)
                //.Include(i => i.FieldsAccountableForm)
                //.Include(i => i.FieldsAgricultural)
                //.Include(i => i.FieldsAnimal)
                //.Include(i => i.FieldsFurniture)
                //.Include(i => i.FieldsLand)
                //.Include(i => i.FieldsMachinery)
                //.Include(i => i.FieldsMedical)
                //.Include(i => i.FieldsMedicine)
                //.Include(i => i.FieldsMilitarySuuply)
                //.Include(i => i.FieldsNonAccountableForm)
                //.Include(i => i.FieldsOfficeSupply)
                //.Include(i => i.FieldsOther)
                //.Include(i => i.FieldsOtherSupplyMaterial)
                //.Include(i => i.FieldsRepair)
                //.Include(i => i.FieldsTransportation)
                //.Include(i => i.FieldsVehicle)
                //.Include(i => i.FieldsConstruction)
                .Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RisItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RisItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RisItemEntryVM> UpdateAsync(RisItemEntryVM model, string user, DateTime date) =>
        _entryVmExceptionService.TryCatchAsync(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.RisItems
                .Include(i => i.AllField)
                //.Include(i => i.FieldsAccountableForm)
                //.Include(i => i.FieldsAgricultural)
                //.Include(i => i.FieldsAnimal)
                //.Include(i => i.FieldsFurniture)
                //.Include(i => i.FieldsLand)
                //.Include(i => i.FieldsMachinery)
                //.Include(i => i.FieldsMedical)
                //.Include(i => i.FieldsMedicine)
                //.Include(i => i.FieldsMilitarySuuply)
                //.Include(i => i.FieldsNonAccountableForm)
                //.Include(i => i.FieldsOfficeSupply)
                //.Include(i => i.FieldsOther)
                //.Include(i => i.FieldsOtherSupplyMaterial)
                //.Include(i => i.FieldsRepair)
                //.Include(i => i.FieldsTransportation)
                //.Include(i => i.FieldsVehicle)
                //.Include(i => i.FieldsConstruction)
                .Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.PsNo = PsNo(model);
            model.PsNoDisplay = PsNoDisplay(model);

            entity.RisId = model.RisId;
            entity.ItemCodeId = model.ItemCodeId;
            entity.PsNo = model.PsNo;
            entity.PsNoDisplay = model.PsNoDisplay;
            entity.ItemName = model.ItemType;
            entity.SubAccountCode = model.SubAccountCode;
            entity.Unit = model.Unit;
            entity.Description = model.Description;
            entity.OtherDesc = model.OtherDesc;
            entity.QtyRequest = model.QtyRequest;
            entity.QtyIssue = model.QtyIssue;
            entity.Remarks = model.Remarks ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            //entity = SetItemEntity(entity, model);

            model.AllField.Id = entity.Id;
            entity.AllField = model.AllField;
            entity.AllField.UpdatedBy = user;
            entity.AllField.UpdatedDt = date;

            _db.RisItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            // cascade updates
            // PR, Description, Qty
            // PO, Description, Qty
            // AIR, Qty            

            return model;
        });

        //private RisItem SetItemEntity(RisItem entity, RisItemEntryVM model)
        //{
        //    entity.FieldsAccountableForm = null;
        //    entity.FieldsAgricultural = null;
        //    entity.FieldsAnimal = null;
        //    entity.FieldsFurniture = null;
        //    entity.FieldsLand = null;
        //    entity.FieldsMachinery = null;
        //    entity.FieldsMedical = null;
        //    entity.FieldsMedicine = null;
        //    entity.FieldsMilitarySuuply = null;
        //    entity.FieldsNonAccountableForm = null;
        //    entity.FieldsOfficeSupply = null;
        //    entity.FieldsOther = null;
        //    entity.FieldsOtherSupplyMaterial = null;
        //    entity.FieldsRepair = null;
        //    entity.FieldsTransportation = null;
        //    entity.FieldsVehicle = null;
        //    entity.FieldsConstruction = null;            

        //    if (Enum.TryParse(model.PsType, out Category category))
        //    {
        //        if (category == Category.A)
        //        {
        //            model.FieldsAccountableForm.Id = entity.Id;
        //            entity.FieldsAccountableForm = model.FieldsAccountableForm;
        //        }
        //        else if (category == Category.B)
        //        {

        //        }
        //        else if (category == Category.C)
        //        {

        //        }
        //        else  if (category == Category.D)
        //        {
        //            model.FieldsMedicine.Id = entity.Id;
        //            entity.FieldsMedicine = model.FieldsMedicine;
        //        }
        //        else if (category == Category.E)
        //        {
        //            model.FieldsMachinery.Id = entity.Id;
        //            entity.FieldsMachinery = model.FieldsMachinery;
        //        }
        //        else if (category == Category.F) // food supplies
        //        {
                    
        //        }
        //        else if (category == Category.G) 
        //        {
        //            model.FieldsAgricultural.Id = entity.Id;
        //            entity.FieldsAgricultural = model.FieldsAgricultural;
        //        }
        //        else if (category == Category.I)
        //        {
                    
        //        }                                
        //        else if (category == Category.L)
        //        {
        //            model.FieldsLand.Id = entity.Id;
        //            entity.FieldsLand = model.FieldsLand;
        //        }
        //        else if (category == Category.M)
        //        {
        //            model.FieldsMedical.Id = entity.Id;
        //            entity.FieldsMedical = model.FieldsMedical;
        //        }
        //        else if (category == Category.N)
        //        {
        //            model.FieldsNonAccountableForm.Id = entity.Id;
        //            entity.FieldsNonAccountableForm = model.FieldsNonAccountableForm;
        //        }
        //        else if (category == Category.O)
        //        {
        //            model.FieldsOfficeSupply.Id = entity.Id;
        //            entity.FieldsOfficeSupply = model.FieldsOfficeSupply;
        //        }
        //        else if (category == Category.P)
        //        {
        //            model.FieldsMilitarySuuply.Id = entity.Id;
        //            entity.FieldsMilitarySuuply = model.FieldsMilitarySuuply;
        //        }
        //        else if (category == Category.R)
        //        {
        //            model.FieldsRepair.Id = entity.Id;
        //            entity.FieldsRepair = model.FieldsRepair;
        //        }
        //        else if (category == Category.S)
        //        {
        //            model.FieldsRepair.Id = entity.Id;
        //            entity.FieldsRepair = model.FieldsRepair;
        //        }
        //        else if (category == Category.T)
        //        {
        //            model.FieldsTransportation.Id = entity.Id;
        //            entity.FieldsTransportation = model.FieldsTransportation;
        //        }
        //        else if (category == Category.U)
        //        {
        //            model.FieldsFurniture.Id = entity.Id;
        //            entity.FieldsFurniture = model.FieldsFurniture;
        //        }
        //        else if (category == Category.V)
        //        {
        //            model.FieldsAnimal.Id = entity.Id;
        //            entity.FieldsAnimal = model.FieldsAnimal;
        //        }
        //        else if (category == Category.X)
        //        {
        //            model.FieldsOtherSupplyMaterial.Id = entity.Id;
        //            entity.FieldsOtherSupplyMaterial = model.FieldsOtherSupplyMaterial;
        //        }
        //        else if (category == Category.Z)
        //        {
        //            model.FieldsOther.Id = entity.Id;
        //            entity.FieldsOther = model.FieldsOther;
        //        }
        //    }

        //    return entity;
        //}

        private string PsNo(RisItemEntryVM fields)
        {
            return _allFieldService.GetRisStockNo(fields);
        }        

        public string PsNoDisplay(RisItemEntryVM model)
        {
            string display = "";
            if (Enum.TryParse(model.PsType, out Category c))
            {
                if (c == CatLands())
                {
                    display = $"{model.ItemCode}/{model.AllField.Area}SqM";
                }
                else
                {
                    display = $"{model.ItemCode}";
                }
            }
            return display;
        }

        public string GetDescription(RisItemEntryVM entry)
        {            
            return _allFieldService.GetRisDescription(entry);
        }        
    }
}