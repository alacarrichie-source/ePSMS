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

namespace iLgs.Services
{
    public class RisItemService : IRisItemService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<RisItemVM> _vmExceptionService = new ExceptionService<RisItemVM>();
        private readonly IExceptionService<RisItemEntryVM> _entryVmExceptionService = new ExceptionService<RisItemEntryVM>();
        private readonly IExceptionService<RisItem> _exceptionService = new ExceptionService<RisItem>();
        private IPsCardService _cardService;

        public RisItemService(AppManEntities db)
        {
            _db = db;
            _cardService = new PsCardService(_db);
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
                    FieldsAccountableForm = s.FieldsAccountableForm,
                    FieldsAgricultural = s.FieldsAgricultural,
                    FieldsAnimal = s.FieldsAnimal,
                    FieldsFurniture = s.FieldsFurniture,
                    FieldsLand = s.FieldsLand,
                    FieldsMachinery = s.FieldsMachinery,
                    FieldsMedical = s.FieldsMedical,
                    FieldsMedicine = s.FieldsMedicine,
                    FieldsMilitarySuuply = s.FieldsMilitarySuuply,
                    FieldsNonAccountableForm = s.FieldsNonAccountableForm,
                    FieldsOfficeSupply = s.FieldsOfficeSupply,
                    FieldsOther = s.FieldsOther,
                    FieldsOtherSupplyMaterial = s.FieldsOtherSupplyMaterial,
                    FieldsRepair = s.FieldsRepair,
                    FieldsTransportation = s.FieldsTransportation,
                    FieldsVehicle = s.FieldsVehicle,
                    FieldsConstruction = s.FieldsConstruction,
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
                    FieldsAccountableForm = s.FieldsAccountableForm,
                    FieldsAgricultural = s.FieldsAgricultural,
                    FieldsAnimal = s.FieldsAnimal,
                    FieldsFurniture = s.FieldsFurniture,
                    FieldsLand = s.FieldsLand,
                    FieldsMachinery = s.FieldsMachinery,
                    FieldsMedical = s.FieldsMedical,
                    FieldsMedicine = s.FieldsMedicine,
                    FieldsMilitarySuuply = s.FieldsMilitarySuuply,
                    FieldsNonAccountableForm = s.FieldsNonAccountableForm,
                    FieldsOfficeSupply = s.FieldsOfficeSupply,
                    FieldsOther = s.FieldsOther,
                    FieldsOtherSupplyMaterial = s.FieldsOtherSupplyMaterial,
                    FieldsRepair = s.FieldsRepair,
                    FieldsTransportation = s.FieldsTransportation,
                    FieldsVehicle = s.FieldsVehicle,
                    FieldsConstruction = s.FieldsConstruction,
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
                    FieldsAccountableForm = s.FieldsAccountableForm,
                    FieldsAgricultural = s.FieldsAgricultural,
                    FieldsAnimal = s.FieldsAnimal,
                    FieldsFurniture = s.FieldsFurniture,
                    FieldsLand = s.FieldsLand,
                    FieldsMachinery = s.FieldsMachinery,
                    FieldsMedical = s.FieldsMedical,
                    FieldsMedicine = s.FieldsMedicine,
                    FieldsMilitarySuuply = s.FieldsMilitarySuuply,
                    FieldsNonAccountableForm = s.FieldsNonAccountableForm,
                    FieldsOfficeSupply = s.FieldsOfficeSupply,
                    FieldsOther = s.FieldsOther,
                    FieldsOtherSupplyMaterial = s.FieldsOtherSupplyMaterial,
                    FieldsRepair = s.FieldsRepair,
                    FieldsTransportation = s.FieldsTransportation,
                    FieldsVehicle = s.FieldsVehicle,
                    FieldsConstruction = s.FieldsConstruction,
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
            model.PsNoDisplay = PsNoDisplay(model.ItemCode, model.Description);

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

            entity = SetItemEntity(entity, model);

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
                .Include(i => i.FieldsAccountableForm)
                .Include(i => i.FieldsAgricultural)
                .Include(i => i.FieldsAnimal)
                .Include(i => i.FieldsFurniture)
                .Include(i => i.FieldsLand)
                .Include(i => i.FieldsMachinery)
                .Include(i => i.FieldsMedical)
                .Include(i => i.FieldsMedicine)
                .Include(i => i.FieldsMilitarySuuply)
                .Include(i => i.FieldsNonAccountableForm)
                .Include(i => i.FieldsOfficeSupply)
                .Include(i => i.FieldsOther)
                .Include(i => i.FieldsOtherSupplyMaterial)
                .Include(i => i.FieldsRepair)
                .Include(i => i.FieldsTransportation)
                .Include(i => i.FieldsVehicle)
                .Include(i => i.FieldsConstruction)
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
                .Include(i => i.FieldsAccountableForm)
                .Include(i => i.FieldsAgricultural)
                .Include(i => i.FieldsAnimal)
                .Include(i => i.FieldsFurniture)
                .Include(i => i.FieldsLand)
                .Include(i => i.FieldsMachinery)
                .Include(i => i.FieldsMedical)
                .Include(i => i.FieldsMedicine)
                .Include(i => i.FieldsMilitarySuuply)
                .Include(i => i.FieldsNonAccountableForm)
                .Include(i => i.FieldsOfficeSupply)
                .Include(i => i.FieldsOther)
                .Include(i => i.FieldsOtherSupplyMaterial)
                .Include(i => i.FieldsRepair)
                .Include(i => i.FieldsTransportation)
                .Include(i => i.FieldsVehicle)
                .Include(i => i.FieldsConstruction)
                .Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.PsNo = PsNo(model);
            model.PsNoDisplay = PsNoDisplay(model.ItemCode, model.Description);

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

            entity = SetItemEntity(entity, model);

            _db.RisItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            // cascade updates
            // PR, Description, Qty
            // PO, Description, Qty
            // AIR, Qty            

            return model;
        });

        private RisItem SetItemEntity(RisItem entity, RisItemEntryVM model)
        {
            entity.FieldsAccountableForm = null;
            entity.FieldsAgricultural = null;
            entity.FieldsAnimal = null;
            entity.FieldsFurniture = null;
            entity.FieldsLand = null;
            entity.FieldsMachinery = null;
            entity.FieldsMedical = null;
            entity.FieldsMedicine = null;
            entity.FieldsMilitarySuuply = null;
            entity.FieldsNonAccountableForm = null;
            entity.FieldsOfficeSupply = null;
            entity.FieldsOther = null;
            entity.FieldsOtherSupplyMaterial = null;
            entity.FieldsRepair = null;
            entity.FieldsTransportation = null;
            entity.FieldsVehicle = null;
            entity.FieldsConstruction = null;            

            if (Enum.TryParse(model.PsType, out Category category))
            {
                if (category == Category.A)
                {
                    model.FieldsAccountableForm.Id = entity.Id;
                    entity.FieldsAccountableForm = model.FieldsAccountableForm;
                }
                else if (category == Category.B)
                {

                }
                else if (category == Category.C)
                {

                }
                else  if (category == Category.D)
                {
                    model.FieldsMedicine.Id = entity.Id;
                    entity.FieldsMedicine = model.FieldsMedicine;
                }
                else if (category == Category.E)
                {
                    model.FieldsMachinery.Id = entity.Id;
                    entity.FieldsMachinery = model.FieldsMachinery;
                }
                else if (category == Category.F) // food supplies
                {
                    
                }
                else if (category == Category.G) 
                {
                    model.FieldsAgricultural.Id = entity.Id;
                    entity.FieldsAgricultural = model.FieldsAgricultural;
                }
                else if (category == Category.I)
                {
                    
                }                                
                else if (category == Category.L)
                {
                    model.FieldsLand.Id = entity.Id;
                    entity.FieldsLand = model.FieldsLand;
                }
                else if (category == Category.M)
                {
                    model.FieldsMedical.Id = entity.Id;
                    entity.FieldsMedical = model.FieldsMedical;
                }
                else if (category == Category.N)
                {
                    model.FieldsNonAccountableForm.Id = entity.Id;
                    entity.FieldsNonAccountableForm = model.FieldsNonAccountableForm;
                }
                else if (category == Category.O)
                {
                    model.FieldsOfficeSupply.Id = entity.Id;
                    entity.FieldsOfficeSupply = model.FieldsOfficeSupply;
                }
                else if (category == Category.P)
                {
                    model.FieldsMilitarySuuply.Id = entity.Id;
                    entity.FieldsMilitarySuuply = model.FieldsMilitarySuuply;
                }
                else if (category == Category.R)
                {
                    model.FieldsRepair.Id = entity.Id;
                    entity.FieldsRepair = model.FieldsRepair;
                }
                else if (category == Category.S)
                {
                    model.FieldsRepair.Id = entity.Id;
                    entity.FieldsRepair = model.FieldsRepair;
                }
                else if (category == Category.T)
                {
                    model.FieldsTransportation.Id = entity.Id;
                    entity.FieldsTransportation = model.FieldsTransportation;
                }
                else if (category == Category.U)
                {
                    model.FieldsFurniture.Id = entity.Id;
                    entity.FieldsFurniture = model.FieldsFurniture;
                }                
            }

            return entity;
        }

        private string PsNo(RisItemEntryVM fields)
        {
            return _cardService.GetRisStockNo(fields);
        }

        //private string PsNo(RisItemEntryVM model)
        //{
        //    string psNo = model.ItemCode.Trim();
        //    if (Enum.TryParse(model.PsType, out Category category))
        //    {
        //        if (category == Category.D)
        //        {
        //            var f = model.FieldsMedicine;
        //            if (f.GenericName.Length >= 3)
        //            {
        //                psNo += f.GenericName.Substring(0, 1) + f.GenericName.Substring(2, 1);
        //            }
        //            else
        //            {
        //                psNo += f.GenericName.Substring(0, 1) + "X";
        //            }
        //            if (!string.IsNullOrWhiteSpace(f.DosageStrength))
        //            {
        //                psNo += f.DosageStrength.Replace(" ", "").Trim();
        //            }
        //            if (!string.IsNullOrWhiteSpace(f.DosageForm))
        //            {
        //                psNo += f.DosageForm.PadRight(3, 'X').Substring(0, 3);
        //            }
        //        }
        //        else if (category == Category.T)
        //        {
        //            var f = model.FieldsVehicle;
        //            if (f.Make.Length >= 3)
        //            {
        //                psNo += f.Make.Substring(0, 1) + f.Make.Substring(2, 1);
        //            }
        //            else
        //            {
        //                psNo += f.Make.Substring(0, 1) + "X";
        //            }

        //            if (f.YearModel > 0)
        //            {
        //                psNo += f.YearModel.ToString().Trim();
        //            }

        //            if (string.IsNullOrWhiteSpace(f.Series))
        //            {
        //                psNo += "XXX";
        //            }
        //            else
        //            {
        //                psNo += f.Series.Substring(0, 3);
        //            }
        //        }
        //    }
            
        //    return psNo;
        //}        


        public string PsNoDisplay(string itemCode, string itemName)
        {
            //var raItems = itemName.Replace(" ", "").Split('/');
            //int itemCount = 0;
            //string psNoDisplay = "";
            //foreach (var raItem in raItems)
            //{
            //    if (++itemCount > 1)
            //    {
            //        psNoDisplay += "/";
            //    }
            //    if (raItem.Length >= 3)
            //    {
            //        psNoDisplay += raItem.Substring(0, 1) + raItem.Substring(2, 1);
            //    }
            //    else
            //    {
            //        psNoDisplay += raItem.Substring(0, 1) + "X";
            //    }
            //}
            //return itemCode.Trim() + psNoDisplay; // model.ItemName.Substring(0, 1) + model.ItemName.Substring(2, 1);

            return itemCode.Trim() + itemName.Substring(0, 1) + itemName.Substring(2, 1);
        }

        public string GetDescription(RisItemEntryVM entry)
        {            
            return _cardService.GetRisDescription(entry);
        }

        //public string GetPsDescription(PsCardVM entry)
        //{
        //    string description = "";
        //    if (Enum.TryParse(entry.ItemTypeCode, out Category category))
        //    {
        //        if (category == Category.T)
        //        {
        //            description = GetVehicleDescription(entry.FieldsVehicle);
        //        }
        //        else if (category == Category.D)
        //        {
        //            description = GetMedicineDescription(entry.FieldsMedicine);

        //        }
        //    }
        //    return description;
        //}

        //private string GetVehicleDescription(FieldsVehicle f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Type) ? "" : f.Type.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Make) ? "" : " " + f.Make.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Series) ? "" : " " + f.Series.Trim();
        //    description += string.IsNullOrWhiteSpace(f.YearModel.ToString()) ? "" : " " + f.YearModel.ToString().Trim();
        //    description += string.IsNullOrWhiteSpace(f.PlateNo) ? "" : " " + f.PlateNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.BodyNo) ? "" : " " + f.BodyNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Color) ? "" : " " + f.Color.Trim();
        //    description += string.IsNullOrWhiteSpace(f.EngineNo) ? "" : " " + f.EngineNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.ChassisNo) ? "" : " " + f.ChassisNo.Trim();
        //    return description;
        //}

        //private string GetMedicineDescription(FieldsMedicine f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.GenericName) ? "" : f.GenericName.Trim();
        //    description += string.IsNullOrWhiteSpace(f.DosageStrength) ? "" : " " + f.DosageStrength.Trim();
        //    description += string.IsNullOrWhiteSpace(f.DosageForm) ? "" : " " + f.DosageForm.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Others) ? "" : " " + f.Others.Trim();
        //    return description;
        //}
    }
}