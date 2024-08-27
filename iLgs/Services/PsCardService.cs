using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.CategoryEnum;

namespace iLgs.Services
{
    public interface IPsCardService
    {
        IQueryable<PsCardVM> GetAll();
        IQueryable<PsCardVM> GetAllByItemCodeId(Guid? itemCodeId);
        ValueTask<PsCardVM> GetVmByIdAsync(Guid? id);

        PsCard GetById(Guid id);
        ValueTask<PsCard> GetByIdAsync(Guid id);
        ValueTask<PsCard> GetByPsNoAsync(string psNo);
        FieldSw GetFieldSw(string category);
        string GetItemFieldsPartialView(string category);
        ValueTask<bool> GetAnyPsNoAsync(Guid id, string psNo);
        string GetDescription(PsCardVM model);
        string GetStockNo(PsCardVM model);
        string GetItemExtnName(Guid? id);

        ValueTask<PsCardVM> CreateAsync(PsCardVM model, string user, DateTime date);
        ValueTask<PsCardVM> UpdateAsync(PsCardVM model, string user, DateTime date);
        ValueTask<PsCardVM> DeleteAsync(PsCardVM model, string user, DateTime date);        

        //string GetRisDescription(RisItemEntryVM model);
        //string GetRisStockNo(RisItemEntryVM model);
    }

    public class PsCardService : IPsCardService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<PsCardVM> _vmExceptionService = new ExceptionService<PsCardVM>();
        private readonly IExceptionService<PsCard> _exceptionService = new ExceptionService<PsCard>();
        private IAllFieldService _allFieldService;
        public PsCardService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(db);
        }

        public IQueryable<PsCardVM> GetAll() => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.AsNoTracking()
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    FieldGroupNo = s.ItemCode.ItemType.FormulaNo,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    Amount = s.Amount,
                    FromDonation = s.FromDonation,
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
                    InsertedDt = s.InsertedDt
                });
            return data;
            //return GetAllByCategory(data);
        });

        //private IQueryable<PsCardVM> GetAllByCategory(IQueryable<PsCardVM> data)
        //{
        //    if (!string.IsNullOrEmpty(_cardCategory))
        //    {
        //        data = data.Where(w => w.CardCategory == _cardCategory);
        //    }
        //    return data;
        //}

        public IQueryable<PsCardVM> GetAllByItemCodeId(Guid? itemCodeId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.Where(w => w.ItemCodeId == itemCodeId).AsNoTracking()
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    FieldGroupNo = s.ItemCode.ItemType.FormulaNo,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
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
                    InsertedDt = s.InsertedDt
                });
            //return GetAllByCategory(data);
            return data;
        });

        public ValueTask<PsCardVM> GetVmByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCards.Where(w => w.Id == id).AsNoTracking()
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    FieldGroupNo = s.ItemCode.ItemType.FormulaNo,
                    CardCategory = s.CardCategory,
                    Description = s.Description,
                    SubAccountCode = s.SubAccountCode,
                    SubAccount = _db.ItemCodes.Where(w => w.ItemTypeId == s.ItemCode.ItemTypeId && w.Code == s.SubAccountCode).Select(x => x.Description).FirstOrDefault(),
                    Fund = s.Fund,
                    Unit = s.Unit,
                    PsNo = s.PsNo,
                    PsName = s.PsName,
                    PrevPsNo = s.PrevPsNo,
                    FromDonation = s.FromDonation,
                    Amount = s.Amount,
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<PsCard> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PsCards
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.AllField)
                .FirstOrDefaultAsync(f => f.Id == id);
        });

        public PsCard GetById(Guid id) 
        {
            return _db.PsCards
                .Include(i => i.ItemCode.ItemType)
                .Include(i => i.AllField)
                .FirstOrDefault(f => f.Id == id);
        }

        public async ValueTask<bool> GetAnyPsNoAsync(Guid id, string psNo)
        {
            return await _db.PsCards.AnyAsync(a => a.Id != id && a.PsNo == psNo);
        }

        public ValueTask<PsCard> GetByPsNoAsync(string psNo) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PsCards.Where(w => w.PsNo == psNo).FirstOrDefaultAsync();
        });

        public FieldSw GetFieldSw(string category)
        {
            var fieldSw = new FieldSw();
            if (Enum.TryParse(category, out Category c))
            {
                fieldSw.InvDist = (c == CatDrugs() || c == CatMedicals() || c == CatAgriculturals());
                fieldSw.AcqDate = (c == CatLands() || c == CatLandImprovements() || c == CatInfrastructures() || c == CatBuildings() ||
                    c == CatMachineries() || c == CatTransportations() || c == CatFurnitures() || c == CatOtherProperties());
                fieldSw.AcqYear = (c == CatConstructionInProgress());
                fieldSw.PhaseNo = (c == CatLands() || c == CatInfrastructures() || c == CatBuildings() || c == CatConstructionInProgress());
                fieldSw.CapitalOutlay = (c == CatLands() || c == CatLandImprovements() || c == CatInfrastructures() || c == CatBuildings() ||
                    c == CatConstructionInProgress());
                fieldSw.Type = (c == CatMachineries() || c == CatTransportations() || c == CatFurnitures() || c == CatOtherProperties() ||
                    c == CatMedicals() || c == CatAgriculturals() || c == CatAnimalSupplies() || c == CatConstructionMaterials() ||
                    c == CatOfficeSupplies() || c == CatAccountableForms() || c == CatNonAccountableForns() || c == CatMilitaries() ||
                    c == CatRepairs() || c == CatOtherSupplies());
            }
            return fieldSw;
        }

        public string GetItemFieldsPartialView(string category)
        {
            string partialView = "";
            if (Enum.TryParse(category, out Category c))
            {
                if (c == CatLands())
                {
                    partialView = "_ItemFieldLand";
                }
                else if (c == CatMachineries()
                    || c == CatTransportations()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies())
                {
                    partialView = "_ItemFieldBrand";
                }
                else if (c == CatDrugs())
                {
                    partialView = "_ItemFieldDrugs";
                }
                else if (c == CatRepairs())
                {
                    partialView = "_ItemFieldSerial";
                }
            }
            return partialView;
        }

        private void ValidateField(PsCardVM model)
        {
            if (model.ItemCodeId == null)
            {
                throw new InvalidValueException("Article is Required!");
            }

            //if (string.IsNullOrWhiteSpace(model.Description))
            //{
            //    throw new InvalidValueException("Description is Required!");
            //}

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                throw new InvalidValueException("Fund is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.PsNo))
            {
                throw new InvalidValueException("Property/Stock No. is Required!");
            }
        }        

        public ValueTask<PsCardVM> CreateAsync(PsCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.Description = "Please see attachment.";
            ValidateField(model);
            _allFieldService.ValidatePsCardAllField(model);
            var itemCode = await _db.ItemCodes.FindAsync(model.ItemCodeId);
            //ValidateAllFieldModel(model.AllField, itemCode);

            if (await _db.PsCards.AnyAsync(a => a.PsNo == model.PsNo))
            {
                throw new RecordAlreadyExistsException(string.Format("Stock No. {0} already exists!", model.PsNo));
            }

            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PsCard
            {
                Id = model.Id,
                ItemCodeId = model.ItemCodeId,
                SubAccountCode = model.SubAccountCode,
                Fund = model.Fund,
                Description = model.Description,
                Unit = model.Unit,
                CardCategory = model.CardCategory,
                PsNo = model.PsNo,
                PsName = model.PsName,
                PrevPsNo = model.PrevPsNo,
                FromDonation = model.FromDonation,
                Amount = model.Amount,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            model.AllField.Id = model.Id;
            model.AllField.InsertedBy = user;
            model.AllField.InsertedDt = date;
            model.AllField.UpdatedBy = user;
            model.AllField.UpdatedDt = date;
            entity.AllField = model.AllField;

            //entity = SetItemEntity(entity, model);

            _db.PsCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardVM> UpdateAsync(PsCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var entity = await GetByIdAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            ValidateField(model);
            _allFieldService.ValidatePsCardAllField(model);            

            if (_db.PsCards.Any(a => a.PsNo == model.PsNo && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("Stock No. {0} already exists!", model.PsNo));
            }

            model.AllField = _allFieldService.ChangeAllFieldCase(model.AllField);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.ItemCodeId = model.ItemCodeId;
            entity.SubAccountCode = model.SubAccountCode;
            entity.Fund = model.Fund;
            entity.Description = model.Description;
            entity.Unit = model.Unit;
            entity.CardCategory = model.CardCategory;
            entity.PsNo = model.PsNo;
            entity.PsName = model.PsName;
            entity.PrevPsNo = model.PrevPsNo;
            entity.FromDonation = model.FromDonation;
            entity.Amount = model.Amount;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            model.AllField.Id = model.Id;
            model.AllField.UpdatedBy = user;
            model.AllField.UpdatedDt = date;
            entity.AllField = model.AllField;

            //entity = SetItemEntity(entity, model);

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardVM> DeleteAsync(PsCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await GetByIdAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCards.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        //private PsCard SetItemEntity(PsCard entity, PsCardVM model)
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

        //    if (Enum.TryParse(model.ItemTypeCode, out Category category))
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
        //            model.FieldsConstruction.Id = entity.Id;
        //            entity.FieldsConstruction = model.FieldsConstruction;
        //        }
        //        else if (category == Category.D)
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
        public string GetStockNo(PsCardVM model) => _allFieldService.GetCardStockNo(model);
                
        public string GetDescription(PsCardVM fields) => "Please see attachment.";

        //public string GetRisStockNo(RisItemEntryVM model)
        //{
        //    string stockNo = model.ItemCode.Trim();
        //    if (Enum.TryParse(model.PsType, out Category category))
        //    {
        //        if (category == Category.A)
        //        {
        //            stockNo += GetAccountableFormPsNo(model.FieldsAccountableForm);
        //        }
        //        else if (category == Category.B)
        //        {

        //        }
        //        else if (category == Category.C)
        //        {
        //            stockNo += GetConstructionPsNo(model.FieldsConstruction);
        //        }
        //        else if (category == Category.D)
        //        {
        //            stockNo += GetMedicinePsNo(model.FieldsMedicine);
        //        }
        //        else if (category == Category.E)
        //        {
        //            stockNo += GetMachineryPsNo(model.FieldsMachinery);
        //        }
        //        else if (category == Category.F)
        //        {

        //        }
        //        else if (category == Category.G)
        //        {
        //            stockNo += GetAgriculturalPsNo(model.FieldsAgricultural);
        //        }
        //        else if (category == Category.I)
        //        {

        //        }
        //        else if (category == Category.L)
        //        {
        //            stockNo += GetLandPsNo(model.FieldsLand);
        //        }
        //        else if (category == Category.M)
        //        {
        //            stockNo += GetMedicalPsNo(model.FieldsMedical);
        //        }
        //        else if (category == Category.N)
        //        {
        //            stockNo += GetNonAccountableFormPsNo(model.FieldsNonAccountableForm);
        //        }
        //        else if (category == Category.O)
        //        {
        //            stockNo += GetOfficeSupplyPsNo(model.FieldsOfficeSupply);
        //        }
        //        else if (category == Category.P)
        //        {
        //            stockNo += GetMilitarySupplyPsNo(model.FieldsMilitarySuuply);
        //        }
        //        else if (category == Category.R)
        //        {
        //            stockNo += GetRepairPsNo(model.FieldsRepair);
        //        }
        //        else if (category == Category.S)
        //        {

        //        }
        //        else if (category == Category.T)
        //        {
        //            stockNo += GetTransportationPsNo(model.FieldsTransportation);
        //        }
        //        else if (category == Category.U)
        //        {
        //            stockNo += GetFurniturePsNo(model.FieldsFurniture);
        //        }
        //        else if (category == Category.V)
        //        {
        //            stockNo += GetAnimalPsNo(model.FieldsAnimal);
        //        }
        //        else if (category == Category.W)
        //        {

        //        }
        //        else if (category == Category.X)
        //        {
        //            stockNo += GetOtherSupplyMaterialPsNo(model.FieldsOtherSupplyMaterial);
        //        }
        //        else if (category == Category.Y)
        //        {

        //        }
        //        else if (category == Category.Z)
        //        {
        //            stockNo += GetOtherPropertyPsNo(model.FieldsOther);
        //        }
        //    }
        //    return stockNo ?? "";
        //}

        //private string GetAccountableFormPsNo(FieldsAccountableForm f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetMachineryPsNo(FieldsMachinery f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetAgriculturalPsNo(FieldsAgricultural f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetMedicalPsNo(FieldsMedical f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetNonAccountableFormPsNo(FieldsNonAccountableForm f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetOfficeSupplyPsNo(FieldsOfficeSupply f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetMilitarySupplyPsNo(FieldsMilitarySuuply f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetTransportationPsNo(FieldsTransportation f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetAnimalPsNo(FieldsAnimal f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetOtherSupplyMaterialPsNo(FieldsOtherSupplyMaterial f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetConstructionPsNo(FieldsConstruction f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetOtherPropertyPsNo(FieldsOther f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }

        //    if (string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/XXX";
        //    }
        //    else
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //private string GetFurniturePsNo(FieldsFurniture f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (!string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Dimension))
        //    {
        //        stockNo += "/" + f.Dimension.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Size))
        //    {
        //        stockNo += "/" + f.Size.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Capacity))
        //    {
        //        stockNo += "/" + f.Capacity.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Color))
        //    {
        //        stockNo += "/" + f.Color.Replace(" ", "").Trim();
        //    }
        //    return stockNo;
        //}

        //private string GetMedicinePsNo(FieldsMedicine f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (!string.IsNullOrWhiteSpace(f.GenericName))
        //    {
        //        if (f.GenericName.Length >= 3)
        //        {
        //            stockNo += "/" + f.GenericName.Substring(0, 1) + f.GenericName.Substring(2, 1);
        //        }
        //        else
        //        {
        //            stockNo += "/" + f.GenericName.Substring(0, 1) + "X";
        //        }
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.DosageStrength))
        //    {
        //        stockNo += "/" + f.DosageStrength.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.DosageForm))
        //    {
        //        stockNo += "/" + f.DosageForm.PadRight(3, 'X').Substring(0, 3);
        //    }

        //    //if (!string.IsNullOrWhiteSpace(f.Others))
        //    //{
        //    //    stockNo += "/" + f.Others.Replace(" ", "").Trim();
        //    //}

        //    if (!string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }
        //    else
        //    {
        //        stockNo += "/xx";
        //    }

        //    if (f.Multipliers.HasValue)
        //    {
        //        stockNo += "/" + f.Multipliers.ToString().Trim() + "'s";
        //    }


        //    return stockNo;
        //}

        //private string GetLandPsNo(FieldsLand f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (f.Area > 0)
        //    {
        //        stockNo += "/" + f.Area.ToString().Trim() + "Sqm";
        //    }

        //    return stockNo;
        //}

        //private string GetRepairPsNo(FieldsRepair f)
        //{
        //    string stockNo = "";
        //    if (f == null)
        //    {
        //        return stockNo;
        //    }

        //    if (!string.IsNullOrWhiteSpace(f.SerialNo))
        //    {
        //        stockNo += "/" + f.SerialNo.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.PropertyNo))
        //    {
        //        stockNo += "/" + f.PropertyNo.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.PlateNo))
        //    {
        //        stockNo += "/" + f.PlateNo.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.BodyNo))
        //    {
        //        stockNo += "/" + f.BodyNo.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.MVFileNo))
        //    {
        //        stockNo += "/" + f.MVFileNo.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Brand))
        //    {
        //        stockNo += "/" + f.Brand.Replace(" ", "").Trim();
        //    }
        //    if (!string.IsNullOrWhiteSpace(f.Model_))
        //    {
        //        stockNo += "/" + f.Model_.Replace(" ", "").Trim();
        //    }

        //    return stockNo;
        //}

        //public string GetRisDescription(RisItemEntryVM model)
        //{
        //    string description = "";
        //    if (Enum.TryParse(model.PsType, out Category category))
        //    {
        //        if (category == Category.A)
        //        {
        //            description += GetAccountableFormDescription(model.FieldsAccountableForm);
        //        }
        //        else if (category == Category.B)
        //        {

        //        }
        //        else if (category == Category.C)
        //        {
        //            description += GetConstructionPsNo(model.FieldsConstruction);
        //        }
        //        else if (category == Category.D)
        //        {
        //            description += GetMedicineDescription(model.FieldsMedicine);
        //        }
        //        else if (category == Category.E)
        //        {
        //            description += GetMachineryDescription(model.FieldsMachinery);
        //        }
        //        else if (category == Category.F)
        //        {

        //        }
        //        else if (category == Category.G)
        //        {
        //            description += GetAgriculturalDescription(model.FieldsAgricultural);
        //        }
        //        else if (category == Category.I)
        //        {

        //        }
        //        else if (category == Category.L)
        //        {
        //            description += GetLandDescription(model.FieldsLand);
        //        }
        //        else if (category == Category.M)
        //        {
        //            description += GetMedicalDescription(model.FieldsMedical);
        //        }
        //        else if (category == Category.N)
        //        {
        //            description += GetNonAccountableFormDescription(model.FieldsNonAccountableForm);
        //        }
        //        else if (category == Category.O)
        //        {
        //            description += GetOfficeSupplyDescription(model.FieldsOfficeSupply);
        //        }
        //        else if (category == Category.P)
        //        {
        //            description += GetMilitarySupplyDescription(model.FieldsMilitarySuuply);
        //        }
        //        else if (category == Category.R)
        //        {
        //            description += GetRepairDescription(model.FieldsRepair);
        //        }
        //        else if (category == Category.S)
        //        {

        //        }
        //        else if (category == Category.T)
        //        {
        //            description += GetTransportationDescription(model.FieldsTransportation);
        //        }
        //        else if (category == Category.U)
        //        {
        //            description += GetFurnitureDescription(model.FieldsFurniture);
        //        }
        //        else if (category == Category.V)
        //        {
        //            description += GetAnimalDescription(model.FieldsAnimal);
        //        }
        //        else if (category == Category.W)
        //        {

        //        }
        //        else if (category == Category.X)
        //        {
        //            description += GetOtherSupplyMaterialDescription(model.FieldsOtherSupplyMaterial);
        //        }
        //        else if (category == Category.Y)
        //        {

        //        }
        //        else if (category == Category.Z)
        //        {
        //            description += GetOtherPropertyDescription(model.FieldsOther);
        //        }

        //    }
        //    return description ?? "";
        //}

        //private string GetMedicineDescription(FieldsMedicine f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.GenericName) ? "" : f.GenericName.Trim();
        //    description += string.IsNullOrWhiteSpace(f.DosageStrength) ? "" : " " + f.DosageStrength.Trim();
        //    description += string.IsNullOrWhiteSpace(f.DosageForm) ? "" : " " + f.DosageForm.Trim();
        //    description += string.IsNullOrWhiteSpace(f.DosageVolume) ? "" : " " + f.DosageVolume.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Others) ? "" : " " + f.Others.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : " (" + f.Brand.Trim() + ")";
        //    return description;
        //}

        //private string GeFurnitureDescription(FieldsFurniture f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Dimension) ? "" : f.Dimension.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Size) ? "" : f.Size.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Capacity) ? "" : f.Capacity.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Color) ? "" : f.Color.Trim();
        //    return description;
        //}

        //private string GetLandDescription(FieldsLand f)
        //{
        //    string description = "";
        //    description += f.Area == null ? "" : f.Area.ToString().Trim() + "Sqm";
        //    return description;
        //}

        //private string GetRepairDescription(FieldsRepair f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.SerialNo) ? "" : f.SerialNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.PropertyNo) ? "" : f.PropertyNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.PlateNo) ? "" : f.PlateNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.BodyNo) ? "" : f.BodyNo.Trim();
        //    description += string.IsNullOrWhiteSpace(f.MVFileNo) ? "" : f.MVFileNo.Trim();
        //    return description;
        //}

        //private string GetAccountableFormDescription(FieldsAccountableForm f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetMachineryDescription(FieldsMachinery f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetAgriculturalDescription(FieldsAgricultural f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetMedicalDescription(FieldsMedical f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetNonAccountableFormDescription(FieldsNonAccountableForm f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetOfficeSupplyDescription(FieldsOfficeSupply f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetMilitarySupplyDescription(FieldsMilitarySuuply f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetTransportationDescription(FieldsTransportation f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetFurnitureDescription(FieldsFurniture f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetAnimalDescription(FieldsAnimal f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetOtherSupplyMaterialDescription(FieldsOtherSupplyMaterial f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetConstructionDescription(FieldsConstruction f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        //private string GetOtherPropertyDescription(FieldsOther f)
        //{
        //    string description = "";
        //    description += string.IsNullOrWhiteSpace(f.Brand) ? "" : f.Brand.Trim();
        //    description += string.IsNullOrWhiteSpace(f.Model_) ? "" : f.Model_.Trim();
        //    return description;
        //}

        public string GetItemExtnName(Guid? id)
        {
            var category = _db.PsCardItems.Where(w => w.Id == id).Select(s => s.PsCard.ItemCode.ItemType.Code).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(category))
            {
                return "";
            }

            string itemExtnName = "";
            if (Enum.TryParse(category, out Category c))
            {
                if (c == CatLands())
                {
                    itemExtnName = "ItemExtnLand";
                }
                else if (c == CatTransportations())
                {
                    itemExtnName = "ItemExtnVehicle";
                }
                else if (c == CatMachineries()
                    || c == CatFurnitures()
                    || c == CatOtherProperties()
                    || c == CatMedicals()
                    || c == CatAgriculturals()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterials()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableForms()
                    || c == CatNonAccountableForns()
                    || c == CatMilitaries()
                    || c == CatOtherSupplies()
                    || c == CatDrugs()
                    || c == CatRepairs()
                    )
                {
                    itemExtnName = "ItemExtnOther";
                }
            }
            return itemExtnName;
        }

    }
}