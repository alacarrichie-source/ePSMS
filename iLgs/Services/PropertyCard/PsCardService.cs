using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.AllFields;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardService : IPsCardSharedService
    {
        IQueryable<PsCardVM> GetAll(string userName);
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
        Task<string> GetStockNoAsync(PsCardVM model);
        bool IsPosted(Guid psCardId);
        bool IsPosted(PsCard psCard);
        bool IsPosted(PsCardItem psCardItem);
        bool IsPosted(PsCardItemExtn psCardItemExtn);

        ValueTask<PsCard> PostAsync(Guid id, string user, DateTime date);
        ValueTask<PsCard> UnpostAsync(Guid id, string user, DateTime date);
        Task<string> GetPsNoAsync(string psName);

        IAllFieldService AllField { get; }
        IPsCardItemService PsCardItem { get; }

        ValueTask<PsCard> TransferPo(Guid? psCardItemId, Guid? transferToPsCardId, string user, DateTime date);
        ValueTask<PsCard> TransferPoBatch(string selectedIds, Guid? transferToPsCardId, string user, DateTime date);

    }

    public class PsCardService : BaseValidator, IPsCardService
    {
        protected readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<PsCardVM> _vmExceptionService;
        private readonly IExceptionService<PsCard> _exceptionService;
        protected readonly IAllFieldService _allFieldService;
        protected readonly IPsCardSharedService _psCardSharedService;
        protected readonly IPsCardItemService _psCardItemService;

        public PsCardService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _vmExceptionService = new ExceptionService<PsCardVM>();
            _exceptionService = new ExceptionService<PsCard>();
            _allFieldService = new AllFieldService(_db);
            _psCardSharedService = new PsCardSharedService(_db);
            _psCardItemService = new PsCardItemService(_db);
        }

        //public PsCardService(AppManEntities db,     
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<PsCardVM> vmExceptionService,
        //    IExceptionService<PsCard> exceptionService,
        //    IAllFieldService allFieldService,
        //    IPsCardSharedService psCardSharedService,
        //    IPsCardItemService psCardItemService            
        //    )
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _exceptions = exceptions;
        //    _vmExceptionService = vmExceptionService;
        //    _exceptionService = exceptionService;
        //    _allFieldService = allFieldService;
        //    _psCardSharedService = psCardSharedService;
        //    _psCardItemService = psCardItemService;            
        //}

        public IAllFieldService AllField => _allFieldService;
        public IPsCardItemService PsCardItem => _psCardItemService;

        public IQueryable<PsCardVM> GetAll(string userName) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<PsCardVM>("Exec Card_GetRecords 'P', {0}", userName).AsQueryable();
            return data;
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
                    InsertedDt = s.InsertedDt,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
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
                    InsertedDt = s.InsertedDt,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
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
                    InsertedDt = s.InsertedDt,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
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
                    c == CatFuelOilSupply() || c == CatOtherSupplies());
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
                    || c == CatOtherSupplies()
                    || c == CatSemiExpendableFurnitureSupply()
                    || c == CatSemiExpendableMachinerySupply())
                {
                    partialView = "_ItemFieldBrand";
                }
                else if (c == CatDrugsSupply())
                {
                    partialView = "_ItemFieldDrugs";
                }
                else if (c == CatFuelOilSupply())
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

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                throw new InvalidValueException("Fund is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.PsNo))
            {
                throw new InvalidValueException("Property/Stock No. is Required!");
            }
        }

        public async Task<string> GetStockNoAsync(PsCardVM model) => await _allFieldService.GetCardStockNoAsync(model);

        public Task<string> GetPsNoAsync(string psName)
        {
            return _db.Database.SqlQuery<string>("Select dbo.fn_GetPsNo({0})", psName).FirstOrDefaultAsync();
        }

        public string GetDescription(PsCardVM fields) => "Please see attachment.";

        public string GetItemExtnNameByItmExtnId(Guid? id)
        {
            return _psCardSharedService.GetItemExtnNameByItmExtnId(id);
        }

        public string GetItemExtnName(Guid? id)
        {
            return _psCardSharedService.GetItemExtnName(id);
        }

        public virtual ValueTask<PsCard> PostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCards.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        public virtual ValueTask<PsCard> UnpostAsync(Guid id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.PsCards.FindAsync(id);
            ValidateRecord(entity);
            ValidateIfNotPosted(entity);

            // AIR check: Block unpost if any acquisition originated from AIR
            var hasAirAcquisition = await _db.PsCardItems.AnyAsync(x => x.PsCardId == id && (x.AIRItemId != null || (x.AirNo != null && x.AirNo != "") || (x.OrderItemRequestId != null && x.TranType == "A")));
            if (hasAirAcquisition)
            {
                throw new InvalidOperationException("This Property Card contains acquisitions generated from an AIR inspection and cannot be independently unposted. Source reversal must be performed through the AIR module.");
            }

            // ICS/PAR check: Block unpost if any unit has draft or posted accountability records
            var icsPars = await _db.IcsParItems.AsNoTracking()
                .Where(x => x.PsCardItemExtn.PsCardItem.PsCardId == id)
                .Select(x => (x.IcsPar.RefType ?? "PAR/ICS") + " No. " + x.IcsPar.RefNo)
                .Distinct()
                .ToListAsync();
            if (icsPars.Any())
            {
                throw new InvalidOperationException($"This Property Card cannot be unposted because one or more units already have accountability records ({string.Join(", ", icsPars)}).");
            }

            // Component accountability check: Block unpost if any components are assigned to accountability records
            var compIcsPars = await _db.IcsParItemComponents.AsNoTracking()
                .Where(x => x.PsCardSubItem.PsCardItem.PsCardId == id || x.PsCardItemExtn.PsCardItem.PsCardId == id)
                .Select(x => (x.IcsParItem.IcsPar.RefType ?? "PAR/ICS") + " No. " + x.IcsParItem.IcsPar.RefNo)
                .Distinct()
                .ToListAsync();
            if (compIcsPars.Any())
            {
                throw new InvalidOperationException($"This Property Card cannot be unposted because components are assigned to accountability records ({string.Join(", ", compIcsPars)}).");
            }

            // Downstream transfer movement
            var hasTransfers = await _db.PsCardItemTransfers.AnyAsync(x => x.PsCardItem.PsCardId == id && x.ParentId != null);
            if (hasTransfers)
            {
                throw new InvalidOperationException("This Property Card contains transferred acquisitions and cannot be unposted.");
            }

            // Downstream issuance movement
            var hasIssuances = await _db.PsCardItems.AnyAsync(x => x.PsCardId == id && (x.QtyIss ?? 0) > 0);
            if (hasIssuances)
            {
                throw new InvalidOperationException("This Property Card contains issued quantities and cannot be unposted.");
            }

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        });

        public virtual ValueTask<PsCard> TransferPo(Guid? psCardItemId, Guid? transferToPsCardId, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var targetEntity = await _db.PsCards.Include(i => i.ItemCode).FirstOrDefaultAsync(p => p.Id == transferToPsCardId);
            ValidateRecord(targetEntity);

            // load source
            var psCardItemSource = await _db.PsCardItems.Include(i => i.PsCard).FirstOrDefaultAsync(x => x.Id == psCardItemId);
            if (psCardItemSource == null)
            {
                throw new NotFoundException((Guid)psCardItemId);
            }

            // Validate card category compatibility
            if (psCardItemSource.PsCard != null && targetEntity != null && psCardItemSource.PsCard.CardCategory != targetEntity.CardCategory)
            {
                throw new InvalidOperationException($"Cannot transfer acquisition to an incompatible card category. Source is {psCardItemSource.PsCard.CardCategory}, target is {targetEntity.CardCategory}.");
            }

            // Validate no posted accountability dependency
            var postedIcsPars = await _db.IcsParItems.AsNoTracking()
                .Where(x => x.PsCardItemExtn.PsCardItemId == psCardItemId && x.IcsPar.PostedDt != null)
                .Select(x => (x.IcsPar.RefType ?? "PAR/ICS") + " No. " + x.IcsPar.RefNo)
                .Distinct()
                .ToListAsync();
            if (postedIcsPars.Any())
            {
                throw new InvalidOperationException($"This acquisition cannot be transferred to another card because one or more units already have posted accountability records ({string.Join(", ", postedIcsPars)}).");
            }

            var compIcsPars = await _db.IcsParItemComponents.AsNoTracking()
                .Where(x => (x.PsCardSubItem.PsCardItemId == psCardItemId || x.PsCardItemExtn.PsCardItemId == psCardItemId) && x.IcsParItem.IcsPar.PostedDt != null)
                .Select(x => (x.IcsParItem.IcsPar.RefType ?? "PAR/ICS") + " No. " + x.IcsParItem.IcsPar.RefNo)
                .Distinct()
                .ToListAsync();
            if (compIcsPars.Any())
            {
                throw new InvalidOperationException($"This acquisition cannot be transferred to another card because components are assigned to posted accountability records ({string.Join(", ", compIcsPars)}).");
            }

            // transfer source to target card
            psCardItemSource.PsCardId = targetEntity.Id;
            psCardItemSource.UpdatedBy = user;
            psCardItemSource.UpdatedDt = date;

            if (targetEntity.ItemCode.IsConsumable?.ToUpper() == "Y")
            {
                psCardItemSource.IsConsumable = true;
            }
            else if (targetEntity.ItemCode.IsConsumable?.ToUpper() == "N")
            {
                psCardItemSource.IsConsumable = false;
            }
            else
            {
                psCardItemSource.IsConsumable = null;
            }

            await _db.SaveChangesAsync();

            return targetEntity;
        });

        public virtual ValueTask<PsCard> TransferPoBatch(string selectedIds, Guid? transferToPsCardId, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var selectedIdList = selectedIds.Split(',').ToList();
            if (selectedIdList.Count() == 0)
            {
                throw new RecordNotFoundException("No Items to process");
            }

            var targetEntity = await _db.PsCards.Include(i => i.ItemCode).FirstOrDefaultAsync(p => p.Id == transferToPsCardId);
            ValidateRecord(targetEntity);

            foreach (var selectedId in selectedIdList)
            {
                var psCardItemId = Guid.Parse(selectedId);

                // load source
                var psCardItemSource = await _db.PsCardItems.FindAsync(psCardItemId);
                if (psCardItemSource == null)
                {
                    throw new NotFoundException((Guid)psCardItemId);
                }

                // transfer source to target card
                psCardItemSource.PsCardId = targetEntity.Id;
                psCardItemSource.UpdatedBy = user;
                psCardItemSource.UpdatedDt = date;

                if (targetEntity.ItemCode.IsConsumable?.ToUpper() == "Y")
                {
                    psCardItemSource.IsConsumable = true;
                }
                else if (targetEntity.ItemCode.IsConsumable?.ToUpper() == "N")
                {
                    psCardItemSource.IsConsumable = false;
                }
                else
                {
                    psCardItemSource.IsConsumable = null;
                }

                await _db.SaveChangesAsync();
            }

            return targetEntity;
        });

        public bool IsPosted(Guid psCardId)
        {
            var entity = _db.PsCards.Find(psCardId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(PsCard psCard)
        {
            return IsPosted(psCard.Id);
        }

        public bool IsPosted(PsCardItem psCardItem)
        {
            var psCardId = (Guid)psCardItem.PsCardId;
            return IsPosted(psCardId);
        }

        public bool IsPosted(PsCardItemExtn psCardItemExtn)
        {
            var psCardItemId = (Guid)psCardItemExtn.PsCardItemId;
            return IsPosted(psCardItemId);
        }

        private void ValidateIfNull(PsCardVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(PsCard entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(PsCard entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(PsCard entity)
        {
            if (entity != null && entity.PostedDt == null)
            {
                var msg = $"Record not yet posted!";
                throw new RecordNotYetPostedException(msg);
            }
        }
    }
}