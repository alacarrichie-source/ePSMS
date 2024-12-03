using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services
{
    public interface IPsCardService
    {
        IQueryable<PsCardVM> GetAll();
        IQueryable<PsCardVM> GetAllStocks();
        IQueryable<PsCardVM> GetAllProperties();
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

        //IStockCardService StockCard { get; }
        //IPropertyCardService PropertyCard { get; }        
        IAllFieldService AllField { get; }
        IPsCardItemService PsCardItem { get; }
        IPsCardItemIssuanceService PsCardItemIssuance { get; }
    }

    public class PsCardService : IPsCardService
    {
        protected readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<PsCardVM> _vmExceptionService = new ExceptionService<PsCardVM>();
        private readonly IExceptionService<PsCard> _exceptionService = new ExceptionService<PsCard>();
        protected IAllFieldService _allFieldService;
        protected IPsCardItemService _psCardItemService;
        protected IPsCardItemIssuanceService _psCardItemIssuanceService;
        
        public PsCardService(AppManEntities db)
        {
            _db = db;
            _allFieldService = new AllFieldService(_db);
            _psCardItemService = new PsCardItemService(_db);
            _psCardItemIssuanceService = new PsCardItemIssuanceService(_db);
        }

        //public IStockCardService StockCard { get { return _stockCardService = _stockCardService ?? new StockCardService(_db); } }
        //public IPropertyCardService PropertyCard { get { return _propertyCardService = _propertyCardService ?? new PropertyCardService(_db); } }
        public IAllFieldService AllField { get { return _allFieldService = _allFieldService ?? new AllFieldService(_db); } }
        public IPsCardItemService PsCardItem { get { return _psCardItemService = _psCardItemService ?? new PsCardItemService(_db); } }
        public IPsCardItemIssuanceService PsCardItemIssuance { get { return _psCardItemIssuanceService = _psCardItemIssuanceService ?? new PsCardItemIssuanceService(_db); } }

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
                    PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
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
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                });
            return data;
            //return GetAllByCategory(data);
        });

        public IQueryable<PsCardVM> GetAllStocks()
        {
            return GetAllByCategory("S");
        }

        public IQueryable<PsCardVM> GetAllProperties()
        {
            return GetAllByCategory("P");
        }

        private IQueryable<PsCardVM> GetAllByCategory(string category) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PsCards.AsNoTracking()
                .Where(w => w.ItemCode.ItemType.Category == category)
                .Select(s => new PsCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemNo = s.ItemCode.ItemNo,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
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
                    AllField = s.AllField,
                    InsertedDt = s.InsertedDt
                });
            return data;        
        });        
        
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
                    PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
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
                    PartialPage = s.ItemCode.PartialPage == null ? s.ItemCode.ItemType.PartialPage : s.ItemCode.PartialPage,
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
                fieldSw.InvDist = (c == CatDrugsSupply() || c == CatMedicalSupply() || c == CatAgriculturalSupply());
                fieldSw.AcqDate = (c == CatLandsProp() || c == CatLandImprovementsProp() || c == CatInfrastructuresProp() || c == CatBuildingsProp() ||
                    c == CatMachineriesProp() || c == CatTransportationProp() || c == CatFurnituresProp() || c == CatOtherProperties());
                fieldSw.AcqYear = (c == CatConstructionInProgressProp());
                fieldSw.PhaseNo = (c == CatLandsProp() || c == CatInfrastructuresProp() || c == CatBuildingsProp() || c == CatConstructionInProgressProp());
                fieldSw.CapitalOutlay = (c == CatLandsProp() || c == CatLandImprovementsProp() || c == CatInfrastructuresProp() || c == CatBuildingsProp() ||
                    c == CatConstructionInProgressProp());
                fieldSw.Type = (c == CatMachineriesProp() || c == CatTransportationProp() || c == CatFurnituresProp() || c == CatOtherProperties() ||
                    c == CatMedicalSupply() || c == CatAgriculturalSupply() || c == CatAnimalSupplies() || c == CatConstructionMaterialsSupply() ||
                    c == CatOfficeSupplies() || c == CatAccountableFormsSupply() || c == CatNonAccountableFornsSupply() || c == CatMilitarySupply() ||
                    c == CatRepairSupply() || c == CatOtherSupplies());
            }
            return fieldSw;
        }

        public string GetItemFieldsPartialView(string category)
        {
            string partialView = "";
            if (Enum.TryParse(category, out Category c))
            {
                if (c == CatLandsProp())
                {
                    partialView = "_ItemFieldLand";
                }
                else if (c == CatMachineriesProp()
                    || c == CatTransportationProp()
                    || c == CatFurnituresProp()
                    || c == CatOtherProperties()
                    || c == CatMedicalSupply()
                    || c == CatAgriculturalSupply()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterialsSupply()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableFormsSupply()
                    || c == CatNonAccountableFornsSupply()
                    || c == CatMilitarySupply()
                    || c == CatOtherSupplies())
                {
                    partialView = "_ItemFieldBrand";
                }
                else if (c == CatDrugsSupply())
                {
                    partialView = "_ItemFieldDrugs";
                }
                else if (c == CatRepairSupply())
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
        
        public string GetStockNo(PsCardVM model) => _allFieldService.GetCardStockNo(model);
                
        public string GetDescription(PsCardVM fields) => "Please see attachment.";        

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
                if (c == CatLandsProp())
                {
                    itemExtnName = "ItemExtnLand";
                }
                else if (c == CatTransportationProp())
                {
                    itemExtnName = "ItemExtnVehicle";
                }
                else if (c == CatMachineriesProp()
                    || c == CatFurnituresProp()
                    || c == CatOtherProperties()
                    || c == CatMedicalSupply()
                    || c == CatAgriculturalSupply()
                    || c == CatAnimalSupplies()
                    || c == CatConstructionMaterialsSupply()
                    || c == CatOfficeSupplies()
                    || c == CatAccountableFormsSupply()
                    || c == CatNonAccountableFornsSupply()
                    || c == CatMilitarySupply()
                    || c == CatOtherSupplies()
                    || c == CatDrugsSupply()
                    || c == CatRepairSupply()
                    )
                {
                    itemExtnName = "ItemExtnOther";
                }
            }
            return itemExtnName;
        }

    }
}