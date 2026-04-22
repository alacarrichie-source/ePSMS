using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PropertyCard;
using iLgs.Services.Validators;
using iLgs.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcsFromPo
{
    public interface IIcsService : IIcsSharedService
    {
        IQueryable<IcsVM> GetAll();
        IQueryable<ParIcsPOGroupVM> GetAllPo();
        IQueryable<ParIcsPOGroupVM> GetAllPo(int? forYear);
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo();
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text);
        ValueTask<ParIcsItemVm> GetItemByIdAsync(Guid? psCardItemId);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, decimal? priceCap);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, decimal? priceCap);        
        IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);

        ValueTask<IcsPar> PostAsync(string refNo, string user, DateTime date);
        ValueTask<IcsPar> UnPostAsync(string refNo, string user, DateTime date);

        ValueTask<ParIcsItemVm> GetByIdAsync(Guid? id);

        ValueTask<GenerateIcsParVM> GenerateIcs(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateIcsSet(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateIcsBatch(GenerateIcsParVM model, string user, DateTime date);

        ValueTask<PsCardItemUnitGroupDescriptionItem> UpdateNoICSAsync(PsCardItemUnitGroupDescriptionItem model, string user, DateTime date);
        //ValueTask<string> NextRefNoAsync(DateTime parDate, string refType, IcsValue icsValue);

    }

    internal class IcsService : BaseValidator, IIcsService
    {
        private readonly AppManEntities _db;
        private decimal? _priceCap;
        private decimal? _SPHV;

        private readonly IPsCardItemService _psCardItemService;
        private readonly IPsCardItemExtnService _psCardItemExtnService;
        private readonly IExceptionService<GenerateIcsParVM> _generateParExceptionService;
        private readonly IExceptionService<PsCardItem> _postExceptionService;
        private readonly IExceptionService<IcsPar> _icsParExceptionService;
        private readonly IExceptionService<PsCardItemUnitGroupDescriptionItem> _psCardItemUnitGroupDescriptionItemExceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IIcsParSharedService _icsParSharedService;
        private readonly IIcsSharedService _icsSharedService;
        private readonly IPriceCapService _priceCapService;
        private readonly ISemiExpendableService _semiExpendableService;

        public IcsService(AppManEntities db)
        {
            _db = db;
            _psCardItemService = new PsCardItemService(_db);
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _generateParExceptionService = new ExceptionService<GenerateIcsParVM>();
            _postExceptionService = new ExceptionService<PsCardItem>();
            _icsParExceptionService = new ExceptionService<IcsPar>();
            _psCardItemUnitGroupDescriptionItemExceptionService = new ExceptionService<PsCardItemUnitGroupDescriptionItem>();
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _icsParSharedService = new IcsParSharedService(_db);
            _icsSharedService = new IcsSharedService(_db);
            _priceCapService = new PriceCapService(_db);
            _semiExpendableService = new SemiExpendableService(_db);
        }

        //public IcsService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IPsCardItemService psCardItemService,
        //    IPsCardItemExtnService psCardItemExtnService,
        //    IExceptionService<GenerateIcsParVM> generateParExceptionService,
        //    IExceptionService<PsCardItem> postExceptionService,
        //    IExceptionService<IcsPar> icsParExceptionService,
        //    IExceptionService<PsCardItemUnitGroupDescriptionItem> psCardItemUnitGroupDescriptionItemService,
        //    IPsCardItemTransactionService psCardItemTransactionService,
        //    IIcsParSharedService icsParSharedService,
        //    IIcsSharedService icsSharedService,
        //    IPriceCapService priceCapService,
        //    ISemiExpendableService semiExpendableService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _psCardItemService = psCardItemService;
        //    _psCardItemExtnService = psCardItemExtnService;
        //    _generateParExceptionService = generateParExceptionService;
        //    _postExceptionService = postExceptionService;
        //    _icsParExceptionService = icsParExceptionService;
        //    _psCardItemUnitGroupDescriptionItemService = psCardItemUnitGroupDescriptionItemService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
        //    _psCardItemTransactionService = psCardItemTransactionService;
        //    _icsParSharedService = icsParSharedService;
        //    _icsSharedService = icsSharedService;
        //    _priceCapService = priceCapService;
        //    _semiExpendableService = semiExpendableService;
        //}

        private decimal GetPriceCap(DateTime? asOfDate)
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap(asOfDate)).Value;
        }

        private decimal GetSPHV(DateTime? asOfDate)
        {
            return _SPHV ?? (_SPHV = _semiExpendableService.GetSPHV(asOfDate)).Value;
        }

        public IQueryable<IcsVM> GetAll()
        {
            var priceCap = GetPriceCap(DateTime.Now);
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null
                    && !w.OrderItem.OrderItemUnitGroupDescriptionItems
                        .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= priceCap)
                    && w.UnitCost < priceCap)
                .Select(s => new IcsVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    //Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
                    Qty = (int?)s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    InsertedDt = s.InsertedDt,
                    DeptId = s.DeptId,
                    LocationId = s.LocationId,
                    Department = s.Codextn.Description,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    DeptDisplay = s.DeptDisplay,
                    LocCode = s.Codextn1.Code,
                    Location = s.Codextn1.Description,
                    StockNo = s.PsCard.PsNo,
                    //IcsBalance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) -
                    //    (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    IcsBalance = (int?)s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            var priceCap = GetPriceCap(DateTime.Now);
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'I', {0}", priceCap).AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo(int? forYear)
        {
            var priceCap = GetPriceCap(DateTime.Now);
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'I', {0}, {1}", priceCap, forYear).AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPoCombo()
        {
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPoByText ''").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text)
        {
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPoBytext {0}", text).AsQueryable();
            return data;
        }

        public async ValueTask<ParIcsItemVm> GetItemByIdAsync(Guid? psCardItemId)
        {
            var data = await _db.PsCardItems
                .Where(w => w.Id == psCardItemId)
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    Qty = (int?)s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = (_db.IcsParItems.Where(w => !w.IcsPar.IcsParUpdates.Any() && w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    InvDist = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate)
        {
            var priceCap = GetPriceCap(poDate);
            var data = GetItemsByPoNo(poNo, priceCap);
            return data;
        }

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, decimal? priceCap)
        {
            var data = _db.Database.SqlQuery<ParIcsItemVm>("Exec ParIcs_GetIcsPoItems {0}, {1}, NULL", poNo, priceCap).AsQueryable();
            return data;
        }

        //public async Task<IList<ParIcsItemSetVm>> GetItemSetsByPoNoAsyncNew(string poNo, DateTime? poDate)
        //{
        //    var priceCap = GetPriceCap(poDate);
        //    var data = await GetItemSetsByPoNoAsyncNew(poNo, priceCap);
        //    return data;
        //}

        //public async Task<IList<ParIcsItemSetVm>> GetItemSetsByPoNoAsyncNew(string poNo, decimal? priceCap)
        //{
        //    var data = await _db.Database.SqlQuery<ParIcsItemSetVm>("Exec ParIcs_GetIcsSetPoItems {0}, {1}", poNo, priceCap).ToListAsync();
        //    return data;
        //}

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate)
        {
            var priceCap = GetPriceCap(poDate);
            var data = GetItemSetsByPoNo(poNo, priceCap);
            return data;
        }

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, decimal? priceCap)
        {
            var data = _db.PsCardItemUnitGroups
                .AsNoTracking()
                .Where(w => w.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroupDescriptionItems
                    .Any(b => b.PsCardItem.TransferRefId == null
                        && b.PsCardItem.PoNo == (string.IsNullOrEmpty(poNo) ? b.PsCardItem.PoNo : poNo)                        
                    ))
                    && (w.UnitCost < priceCap
                        || w.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroup.UnitCost >= priceCap
                            && a.PsCardItemUnitGroupDescriptionItems.Any(b => b.PsCardItem.IsForICS == true)))
                )
                .Select(s => new ParIcsItemSetVm
                {
                    Id = s.Id,
                    SetLotNo = s.SetLotNo,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    AddCost = s.AddCost,
                    GTotalCost = s.GTotalCost,
                    InsertedDt = s.InsertedDt,
                    UnitGroupDescriptions = s.PsCardItemUnitGroupDescriptions,
                    SetPostedBy = s.PostedBy,
                    SetPostedDt = s.PostedDt
                }).AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId)
        {
            var data = _db.PsCardItemUnitGroupDescriptions.AsNoTracking()
                .Where(w => w.UnitGroupId == unitGroupId).AsQueryable();
            return data;
        }

        public IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId)
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null && w.PsCardItemUnitGroupDescriptionItems
                    .Any(a => a.UnitGroupDescriptionId == unitGroupDescriptionId
                    //&& a.PsCardItemId == w.Id                        
                    )
                //&& w.IsForICS == true
                )
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    //Qty = s.Qty,
                    Qty = s.PsCardItemUnitGroupDescriptionItems.FirstOrDefault(f => f.PsCardItemId == s.Id).PoQty,
                    TotalQty = s.PsCardItemUnitGroupDescriptionItems.FirstOrDefault(f => f.PsCardItemId == s.Id).PoQty
                        * s.PsCardItemUnitGroupDescriptionItems.FirstOrDefault(f => f.PsCardItemId == s.Id).PsCardItemUnitGroupDescription.PsCardItemUnitGroup.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = (_db.IcsParItems.Where(w => !w.IcsPar.IcsParUpdates.Any() && w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    InvDist = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution,
                    ParPostedBy = s.ParPostedBy,
                    ParPostedDt = s.ParPostedDt
                }).AsQueryable();
            return data;
        }

        public async ValueTask<ParIcsItemVm> GetByIdAsync(Guid? psCardItemId)
        {
            //var data = await _db.PsCardItems.Where(w => w.Id == id)
            //    .Select(s => new IcsVM
            //    {
            //        Id = s.Id,
            //        GroupId = s.GroupId,
            //        PsCardId = s.PsCardId,
            //        OrderItemId = s.OrderItemId,
            //        PoNo = s.PoNo,
            //        PoDate = s.PoDate,
            //        AirDate = s.AirDate,
            //        AirNo = s.AirNo,
            //        Qty = (int?)s.Qty,
            //        Unit = s.Unit,
            //        UnitCost = s.UnitCost,
            //        Amount = s.Amount,
            //        PriceRate = s.PriceRate,
            //        InsertedDt = s.InsertedDt,
            //        DeptId = s.DeptId,
            //        LocationId = s.LocationId,
            //        Department = s.Codextn.Description,
            //        Article = s.PsCard.ItemCode.Description,
            //        Description = s.Description,
            //        DeptDisplay = s.DeptDisplay,
            //        LocCode = s.Codextn1.Code,
            //        Location = s.Codextn1.Description,
            //        StockNo = s.PsCard.PsNo,
            //        IcsBalance = (int?)s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
            //        OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId),
            //        IsConsumable = s.IsConsumable,
            //        IsIncorporated = s.IsIncorporated,
            //        IsOthers = s.IsOthers,
            //        OtherRemarks = s.OtherRemarks,
            //        AcqDate = s.AcqDate,
            //        InvDist = s.InvDist
            //    }).FirstOrDefaultAsync();
            var data = await _db.Database.SqlQuery<ParIcsItemVm>("Exec ParIcs_GetIcsPoItems NULL, NULL, {0}", psCardItemId).FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<GenerateIcsParVM> GenerateIcs(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatch(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            var cardItem = await GetByIdAsync(model.PsCardItemId);

            if (cardItem == null)
            {
                throw new NotFoundException((Guid)model.PsCardItemId);
            }

            if (cardItem.IsConsumable == true)
            {
                throw new InvalidValueException("Item is marked as Consumable, cannot generate ICS!");
            }

            if (cardItem.IsIncorporated == true)
            {
                throw new InvalidValueException("Item is marked as Incorporated, cannot generate ICS!");
            }

            if (cardItem.IsOthers == true)
            {
                throw new InvalidValueException(string.Format("Item is {0}, cannot generate ICS!", cardItem.OtherRemarks));
            }

            if (cardItem.ParIcsBalance == 0)
            {
                throw new InvalidValueException(string.Format("All Items have ICS."));
            }


            if (model.Qty > cardItem.ParIcsBalance)
            {
                throw new InvalidValueException(string.Format("Cannot generate more than the available balance."));
            }

            if (cardItem.InvDist == "D")
            {
                throw new InvalidValueException(string.Format("Item is For Distribution, cannot generate ICS!"));
            }


            // -----------
            if (model.SelectedIds == null)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }

            var selectedItemExtnIds = model.SelectedIds.Split(',');
            if (selectedItemExtnIds.Count() == 0)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }
            // -----------

            var refType = (await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking().Where(w => w.PsCardItemExtn.PsCardItemId == model.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync())?.IcsPar.RefType;
            if (!string.IsNullOrWhiteSpace(refType))
            {
                throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
            }

            var acqYear = GetAcqDate(cardItem).Value.Year.ToString();
            var selectedIcsValues = await SetIcsValueAsync(selectedItemExtnIds);
            var selectedIcsValueGroups = selectedIcsValues.GroupBy(g => g.IcsValue).Select(s => s.Key);
            foreach (var selectedIcsValueGroup in selectedIcsValueGroups)
            {
                var icsPar = await SetIcsParAsync(model, (int)selectedIcsValueGroup, user, date);
                var selectedIcsValueGroupItems = selectedIcsValues.Where(w => w.IcsValue == selectedIcsValueGroup);
                foreach (var selectedIcsValueGroupItem in selectedIcsValueGroupItems)
                {
                    var psCardItemExtnId = selectedIcsValueGroupItem.Id;
                    var acqCost = (await _db.PsCardItemExtns.FirstOrDefaultAsync(f => f.Id == psCardItemExtnId)).AcqCost;
                    IcsParItem icsParItem = new IcsParItem()
                    {
                        Id = Guid.NewGuid(),
                        IcsParId = icsPar.Id,
                        PsCardItemExtnId = psCardItemExtnId,
                        Qty = 1,
                        Amount = acqCost,
                        IssuedTo = model.IssuedTo,
                        Designation = model.Designation,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    icsPar.IcsParItems.Add(icsParItem);
                }

                _db.IcsPars.Add(icsPar);
                //_db.Entry(icsPar).State = EntityState.Added;
                await _db.SaveChangesAsync();

                // assign property numbers to the items of generated ICS/PAR
                foreach (var selectedIcsValueGroupItem in selectedIcsValueGroupItems)
                {
                    var psCardItemExtnId = selectedIcsValueGroupItem.Id;
                    var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                    var propSplit = propNo.Split('/');
                    var propSeq = propSplit[propSplit.Length - 2];
                    var psCardItemExtn = await _db.PsCardItemExtns.FirstOrDefaultAsync(f => f.Id == psCardItemExtnId);
                    psCardItemExtn.LocationId = model.LocationId;
                    psCardItemExtn.PropYear = acqYear;
                    psCardItemExtn.PropNo = propNo;
                    psCardItemExtn.PropSeq = propSeq;
                    psCardItemExtn.UpdatedBy = user;
                    psCardItemExtn.UpdatedDt = date;

                    await _db.SaveChangesAsync();
                    await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "ICS", user, date);
                }
            }

            return model;
        });

        public ValueTask<GenerateIcsParVM> GenerateIcsSet(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatch(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            if (model.SelectedIds == null)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }

            // Deserialize the JSON string to a List of your model
            var selectedItems = JsonConvert.DeserializeObject<List<PsCardItemExtnSetVM>>(model.SelectedIds);
            if (selectedItems.Count() == 0)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }

            // validate records
            foreach (var selectedItem in selectedItems)
            {
                var psCardItemExtnId = selectedItem.Id;
                var psCardItemExtn = await _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId).FirstOrDefaultAsync();
                var cardItem = await GetByIdAsync(psCardItemExtn.PsCardItemId);

                if (cardItem == null)
                {
                    throw new RecordNotFoundException((Guid)psCardItemExtn.PsCardItemId);
                }

                if (cardItem.ParIcsBalance == 0)
                {
                    throw new InvalidValueException(string.Format("All Items have ICS."));
                }

                if (model.Qty > cardItem.ParIcsBalance)
                {
                    throw new InvalidValueException(string.Format("Cannot generate more than the available balance."));
                }

                var refType = (await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking()
                    .Where(w => w.PsCardItemExtn.PsCardItemId == psCardItemExtn.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync())?.IcsPar.RefType;
                if (!string.IsNullOrWhiteSpace(refType))
                {
                    throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
                }
            }
            var selectedIcsValueGroups = selectedItems.GroupBy(g => new { g.IcsValue })
                    .Select(s => new
                    {
                        IcsValue = s.Key.IcsValue
                    }).ToList();
            // process Set by valueGroup
            foreach (var selectedIcsValueGroup in selectedIcsValueGroups)
            {
                var icsPar = await SetIcsParAsync(model, (int)selectedIcsValueGroup.IcsValue, user, date);
                _db.IcsPars.Add(icsPar);
                _db.Entry(icsPar).State = EntityState.Added;
                await _db.SaveChangesAsync();

                /*
                 *  Get all Sets w/ specific ValueGroup
                 *  Get all Set Items
                */

                var selectedItemGroups = selectedItems.Where(w => w.IcsValue == selectedIcsValueGroup.IcsValue).GroupBy(g => new { g.SetLotNo, g.SetLotQtyNo })
                    .Select(s => new
                    {
                        SetLotNo = s.Key.SetLotNo,
                        SetLotQtyNo = s.Key.SetLotQtyNo
                    }).ToList();
                foreach (var selectedItemGroup in selectedItemGroups)
                {
                    var selectedSetItems = selectedItems.Where(w => w.SetLotNo == selectedItemGroup.SetLotNo && w.SetLotQtyNo == selectedItemGroup.SetLotQtyNo).ToList();
                    foreach (var selectedSetItem in selectedSetItems)
                    {
                        var psCardItemExtnId = selectedSetItem.Id;
                        var psCardItemExtn = await _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId).FirstOrDefaultAsync();
                        var cardItem = await GetByIdAsync(psCardItemExtn.PsCardItemId);
                        string acqYear = GetAcqDate(cardItem).Value.Year.ToString();

                        IcsParItem icsParItem = new IcsParItem()
                        {
                            Id = Guid.NewGuid(),
                            IcsParId = icsPar.Id,
                            PsCardItemExtnId = selectedSetItem.Id,
                            Qty = 1,
                            Amount = selectedSetItem.AcqCost,
                            IssuedTo = model.IssuedTo,
                            Designation = model.Designation,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        _db.IcsParItems.Add(icsParItem);
                        _db.Entry(icsParItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();

                        var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                        var propSplit = propNo.Split('/');
                        var propSeq = propSplit[propSplit.Length - 2];

                        psCardItemExtn.LocationId = model.LocationId;
                        psCardItemExtn.PropYear = acqYear;
                        psCardItemExtn.PropNo = propNo;
                        psCardItemExtn.PropSeq = propSeq;
                        psCardItemExtn.UpdatedBy = user;
                        psCardItemExtn.UpdatedDt = date;

                        //_db.PsCardItemExtns.Attach(psCardItemExtn);
                        //_db.Entry(psCardItemExtn).State = EntityState.Modified;
                        await _db.SaveChangesAsync();

                        await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "ICS", user, date);
                    }

                    // insert unit groups after creating the par/ics

                    var psCardItemId = selectedItems.FirstOrDefault().PsCardItemId;
                    var poNo = selectedItems.FirstOrDefault().PoNo;

                    var unitGroups = _db.PsCardItemUnitGroups
                        .Include(i => i.PsCardItemUnitGroupDescriptions)
                        .Where(w => w.PoNo == poNo)
                        .ToList();
                    foreach (var unitGroup in unitGroups)
                    {
                        for (int q = 1; q <= unitGroup.Qty; q++)
                        {
                            var selectedItemList = selectedSetItems.Where(w => w.SetLotNo == unitGroup.SetLotNo && w.SetLotQtyNo == q).ToList();
                            var selectedCount = selectedItemList.Count();
                            var groupItemCount = _db.PsCardItemUnitGroupDescriptionItems
                                .Where(w => w.PsCardItemUnitGroupDescription.UnitGroupId == unitGroup.Id)
                                .Sum(s => s.PoQty) ?? 0;

                            if (selectedCount == groupItemCount) // all items in the set were selected
                            {
                                // create set record
                                int qty = 1;
                                var icsParUnitGroup = new IcsParUnitGroup()
                                {
                                    Id = Guid.NewGuid(),
                                    IcsParId = icsPar.Id,
                                    SetLotNo = unitGroup.SetLotNo,
                                    Qty = qty,
                                    Unit = unitGroup.Unit,
                                    UnitCost = unitGroup.UnitCost,
                                    TotalCost = unitGroup.UnitCost * qty,
                                    AddCost = unitGroup.AddCost,
                                    TUnitCost = unitGroup.TUnitCost,
                                    GTotalCost = unitGroup.GTotalCost,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    Updatedby = user,
                                    UpdatedDt = date
                                };

                                foreach (var unitGroupDescription in unitGroup.PsCardItemUnitGroupDescriptions)
                                {
                                    var icsParUnitGroupDescription = new IcsPartUnitGroupDescription()
                                    {
                                        Id = Guid.NewGuid(),
                                        UnitGroupId = icsParUnitGroup.Id,
                                        Description = unitGroupDescription.Description,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };

                                    var unitGroupDescriptionItems = _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.UnitGroupDescriptionId == unitGroupDescription.Id).ToList();
                                    foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                                    {
                                        var unitGroupDescriptionItemSelections = selectedItemList.Where(w => w.PsCardItemId == unitGroupDescriptionItem.PsCardItemId);
                                        foreach (var selectedItem in unitGroupDescriptionItemSelections) // selecteditemList from GenerateParSet Selection
                                        {
                                            var icsParItemId = _db.IcsParItems.Where(w => w.PsCardItemExtnId == selectedItem.Id && w.IcsPar.RefNo == icsPar.RefNo).FirstOrDefault().Id;
                                            var icsParUnitGroupDescriptionItems = new IcsParUnitGroupDescriptionItem()
                                            {
                                                Id = Guid.NewGuid(),
                                                UnitGroupDescriptionId = icsParUnitGroupDescription.Id,
                                                IcsParItemId = icsParItemId,
                                                InsertedBy = user,
                                                InsertedDt = date,
                                                UpdatedBy = user,
                                                UpdatedDt = date
                                            };
                                            icsParUnitGroupDescription.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescriptionItems);
                                        }
                                    }
                                    icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDescription);
                                }

                                _db.IcsParUnitGroups.Add(icsParUnitGroup);
                                _db.Entry(icsParUnitGroup).State = EntityState.Added;
                                await _db.SaveChangesAsync();
                            }
                        }
                    }
                }
            }

            return model;
        });

        private async ValueTask<IcsPar> SetIcsParAsync(GenerateIcsParVM model, int icsValue, string user, DateTime date)
        {
            var refNo = await NextRefNoAsync(model.Date, model.RefType, (IcsValue)icsValue);
            var icsPar = new IcsPar()
            {
                Id = Guid.NewGuid(),
                UpdateCode = "N",
                LocationId = model.LocationId,
                LocationCode = model.LocationCode,
                Location = model.Location,
                RefNo = refNo,
                RefDate = model.Date,
                RefType = model.RefType,
                ReceivedById = model.IcsPar.ReceivedById,
                ReceivedBy = model.IcsPar.ReceivedBy.Trim(),
                ReceivedByTitle = model.IcsPar.ReceivedByTitle?.Trim(),
                ReceivedByTitle2 = model.IcsPar.ReceivedByTitle2?.Trim(),
                ReceivedByPosition = model.IcsPar.ReceivedByPosition?.Trim(),
                ReceivedDate = model.IcsPar.ReceivedDate,
                ReceivedDept = model.IcsPar.ReceivedDept.Trim(),
                IssuedBy = model.IcsPar.IssuedBy.Trim(),
                IssuedByPosition = model.IcsPar.IssuedByPosition.Trim(),
                IssuedDate = model.IcsPar.IssuedDate,
                IssuedDept = model.IcsPar.IssuedDept.Trim(),
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };
            return icsPar;
        }

        private DateTime? GetAcqDate(ParIcsItemVm cardItem)
        {
            //if (cardItem.AcqDate != null)
            //{
            //    return cardItem.AcqDate;
            //}
            //else 

            if (cardItem.AirDate != null)
            {
                return cardItem.AirDate;
            }

            if (cardItem.PoDate == null)
            {
                throw new InvalidValueException("Acquisition Date is Required.");
            }

            return cardItem.PoDate;
        }

        public ValueTask<GenerateIcsParVM> GenerateIcsBatch(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatch(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            /*
             *  Get all Non-Set PO Items, without ICS
             *  Get all Set PO Items, without ICS
             *  Generate Ics by IcsValue
             */

            // Get all PO Items, For ICS (Cost < 50k) without ICS            
            var priceCap = GetPriceCap(model.PoDate);
            var cardPoItems = GetItemsByPoNo(model.PoNo, priceCap).ToList();            
            cardPoItems = cardPoItems.Where(w => !w.IsSetLot && w.UnitGroupId == null
                && w.InvDist == "I"
                && w.IsConsumable != true
                && w.IsIncorporated != true
                && w.IsOthers != true
                && (w.IsForICS == true || _db.PsCardItemExtns.Any(a => a.PsCardItemId == w.PsCardItemId && a.AcqCost < priceCap && !a.IcsParItems.Any()))).ToList();

            var unitGroups = GetItemSetsByPoNo(model.PoNo, priceCap).ToList();                
            unitGroups = unitGroups.Where(w => _db.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemUnitGroupDescription.UnitGroupId == w.Id 
                && a.PsCardItem.PsCardItemExtns.Any(b => !b.IcsParItems.Any()))).ToList();

            if (!cardPoItems.Any() && !unitGroups.Any())
            {
                throw new NotFoundException("No candidate items found for ICS.");
            }

            var SPHV = GetSPHV(model.PoDate);
            List<PsCardItemExtn> psCardItemExtnList = null;

            for (int icsValue = 1; icsValue <= 2; icsValue++)
            {

                IcsPar icsPar = null;
                var icsSw = 0;
                // Individual Items
                foreach (var cardPoItem in cardPoItems)
                {
                    if (icsValue == (int)IcsValue.SPLV)
                    {
                        // Get Low Value Items
                        psCardItemExtnList = await _db.PsCardItemExtns
                                .Where(w => w.PsCardItemId == cardPoItem.PsCardItemId && !w.IcsParItems.Any() && w.AcqCost < SPHV)
                                .OrderBy(o => o.SetLotNo).ThenBy(o => o.SetLotQtyNo).ThenBy(o => o.ContentNo)
                                .ToListAsync();
                    }
                    else
                    {
                        // Get High Value Items
                        psCardItemExtnList = await _db.PsCardItemExtns
                                .Where(w => w.PsCardItemId == cardPoItem.PsCardItemId && !w.IcsParItems.Any() && w.AcqCost >= SPHV)
                                .OrderBy(o => o.SetLotNo).ThenBy(o => o.SetLotQtyNo).ThenBy(o => o.ContentNo)
                                .ToListAsync();
                    }

                    // Create ICS for icsValue if not yet created and if with item
                    if (icsSw == 0 && psCardItemExtnList.Any())
                    {
                        icsPar = await SetIcsParAsync(model, icsValue, user, date);
                        _db.IcsPars.Add(icsPar);
                        _db.Entry(icsPar).State = EntityState.Added;
                        await _db.SaveChangesAsync();

                        icsSw = 1;
                    }

                    foreach (var psCardItemExtn in psCardItemExtnList)
                    {
                        var psCardItemExtnId = psCardItemExtn.Id;
                        var acqCost = psCardItemExtn.AcqCost;
                        var icsParItem = new IcsParItem()
                        {
                            Id = Guid.NewGuid(),
                            IcsParId = icsPar.Id,
                            PsCardItemExtnId = psCardItemExtnId,
                            Qty = 1,
                            Amount = acqCost,
                            IssuedTo = model.IssuedTo,
                            Designation = model.Designation,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        _db.IcsParItems.Add(icsParItem);
                        _db.Entry(icsParItem).State = EntityState.Added;
                        await _db.SaveChangesAsync();

                        // generate and update property number foreach item
                        var cardItem = await GetByIdAsync(cardPoItem.PsCardItemId);
                        var acqYear = GetAcqDate(cardItem).Value.Year.ToString();
                        var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                        var propSplit = propNo.Split('/');
                        var propSeq = propSplit[propSplit.Length - 2];

                        var psCardItemExtnEntity = _db.PsCardItemExtns.Find(psCardItemExtn.Id);
                        psCardItemExtnEntity.LocationId = model.LocationId;
                        psCardItemExtnEntity.PropNo = propNo;
                        psCardItemExtnEntity.PropYear = acqYear;
                        psCardItemExtnEntity.PropSeq = propSeq;
                        psCardItemExtnEntity.UpdatedBy = user;
                        psCardItemExtnEntity.UpdatedDt = date;

                        //_db.PsCardItemExtns.Attach(psCardItemExtnEntity);
                        //_db.Entry(psCardItemExtnEntity).State = EntityState.Modified;
                        await _db.SaveChangesAsync();
                        await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "ICS", user, date);
                    }
                }

                // Set Items
                foreach (var unitGroup in unitGroups)
                {
                    var selectedItems = _psCardItemExtnService.GetCardItemExtnSetForIcsParByUnitGroupId(unitGroup.Id);
                    var selectedItemGroups = selectedItems.Where(w => w.IcsValue == icsValue) // Get groups by icsValue (LV or HV)
                            .GroupBy(g => new { g.SetLotNo, g.SetLotQtyNo })
                            .Select(s => new
                            {
                                SetLotNo = s.Key.SetLotNo,
                                SetLotQtyNo = s.Key.SetLotQtyNo
                            }).ToList();

                    // Create ICS for icsValue if not yet created and if with item
                    if (icsSw == 0 && selectedItemGroups.Any())
                    {
                        icsPar = await SetIcsParAsync(model, icsValue, user, date);
                        _db.IcsPars.Add(icsPar);
                        _db.Entry(icsPar).State = EntityState.Added;
                        await _db.SaveChangesAsync();

                        icsSw = 1;
                    }

                    foreach (var selectedItemGroup in selectedItemGroups) // Groups may hve multiple SetLotNo
                    {
                        var selectedSetItems = selectedItems.Where(w => w.SetLotNo == selectedItemGroup.SetLotNo && w.SetLotQtyNo == selectedItemGroup.SetLotQtyNo).ToList();
                        foreach (var selectedSetItem in selectedSetItems)
                        {
                            var psCardItemExtnId = selectedSetItem.Id;
                            // check if with ics
                            if (_db.IcsParItems.Any(a => a.PsCardItemExtnId == psCardItemExtnId))
                            {
                                continue;
                            }

                            var psCardItemExtn = await _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId).FirstOrDefaultAsync();
                            var cardItem = await GetByIdAsync(psCardItemExtn.PsCardItemId);
                            var acqYear = GetAcqDate(cardItem).Value.Year.ToString();

                            var icsParItem = new IcsParItem()
                            {
                                Id = Guid.NewGuid(),
                                IcsParId = icsPar.Id,
                                PsCardItemExtnId = selectedSetItem.Id,
                                Qty = 1,
                                Amount = psCardItemExtn.AcqCost,
                                IssuedTo = model.IssuedTo,
                                Designation = model.Designation,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };
                            _db.IcsParItems.Add(icsParItem);
                            _db.Entry(icsParItem).State = EntityState.Added;
                            await _db.SaveChangesAsync();

                            var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                            var propSplit = propNo.Split('/');
                            var propSeq = propSplit[propSplit.Length - 2];

                            psCardItemExtn.LocationId = model.LocationId;
                            psCardItemExtn.PropYear = acqYear;
                            psCardItemExtn.PropNo = propNo;
                            psCardItemExtn.PropSeq = propSeq;
                            psCardItemExtn.UpdatedBy = user;
                            psCardItemExtn.UpdatedDt = date;

                            //_db.PsCardItemExtns.Attach(psCardItemExtn);
                            //_db.Entry(psCardItemExtn).State = EntityState.Modified;
                            await _db.SaveChangesAsync();

                            await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "ICS", user, date);
                        }

                        // insert unit groups after creating the par/ics                   
                        for (int q = 1; q <= unitGroup.Qty; q++)
                        {
                            var selectedItemList = selectedSetItems.Where(w => w.SetLotNo == unitGroup.SetLotNo && w.SetLotQtyNo == q).ToList();
                            var selectedCount = selectedItemList.Count();
                            var groupItemCount = _db.PsCardItemUnitGroupDescriptionItems
                                .Where(w => w.PsCardItemUnitGroupDescription.UnitGroupId == unitGroup.Id)
                                .Sum(s => s.PoQty) ?? 0;

                            if (selectedCount == groupItemCount) // all items in the set were selected
                            {
                                // create set record
                                int qty = 1;
                                var icsParUnitGroup = new IcsParUnitGroup()
                                {
                                    Id = Guid.NewGuid(),
                                    IcsParId = icsPar.Id,
                                    SetLotNo = unitGroup.SetLotNo,
                                    Qty = qty,
                                    Unit = unitGroup.Unit,
                                    UnitCost = unitGroup.UnitCost,
                                    TotalCost = unitGroup.UnitCost * qty,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    Updatedby = user,
                                    UpdatedDt = date
                                };

                                foreach (var unitGroupDescription in unitGroup.UnitGroupDescriptions)
                                {
                                    var icsParUnitGroupDescription = new IcsPartUnitGroupDescription()
                                    {
                                        Id = Guid.NewGuid(),
                                        UnitGroupId = icsParUnitGroup.Id,
                                        Description = unitGroupDescription.Description,
                                        InsertedBy = user,
                                        InsertedDt = date,
                                        UpdatedBy = user,
                                        UpdatedDt = date
                                    };

                                    var unitGroupDescriptionItems = _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.UnitGroupDescriptionId == unitGroupDescription.Id).ToList();
                                    foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                                    {
                                        var unitGroupDescriptionItemSelections = selectedItemList.Where(w => w.PsCardItemId == unitGroupDescriptionItem.PsCardItemId);
                                        foreach (var selectedItem in unitGroupDescriptionItemSelections) // selecteditemList from GenerateParSet Selection
                                        {
                                            var icsParItemId = _db.IcsParItems.Where(w => w.PsCardItemExtnId == selectedItem.Id && w.IcsPar.RefNo == icsPar.RefNo).FirstOrDefault().Id;
                                            var icsParUnitGroupDescriptionItems = new IcsParUnitGroupDescriptionItem()
                                            {
                                                Id = Guid.NewGuid(),
                                                UnitGroupDescriptionId = icsParUnitGroupDescription.Id,
                                                IcsParItemId = icsParItemId,
                                                InsertedBy = user,
                                                InsertedDt = date,
                                                UpdatedBy = user,
                                                UpdatedDt = date
                                            };
                                            icsParUnitGroupDescription.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescriptionItems);
                                        }
                                    }
                                    icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDescription);
                                }

                                _db.IcsParUnitGroups.Add(icsParUnitGroup);
                                _db.Entry(icsParUnitGroup).State = EntityState.Added;
                                await _db.SaveChangesAsync();
                            }
                        }
                    }
                }
            }

            return model;
        });
        
        /*
         * Insert all selectedIds to a List with corresponding IcsValue
         */
        public async ValueTask<List<IcsValueVM>> SetIcsValueAsync(string[] selectedItemExtnIds)
        {
            var icsValueList = new List<IcsValueVM>();
            foreach (var selectedItemExtnId in selectedItemExtnIds)
            {
                var id = Guid.Parse(selectedItemExtnId);
                var icsParItem = await _db.IcsParItems.FirstOrDefaultAsync(f => f.PsCardItemExtnId == id);
                if (icsParItem == null) // no ICS/PAR
                {
                    var poDate = await _db.PsCardItemExtns.Where(f => f.Id == id).Select(f => f.PsCardItem.PoDate).FirstOrDefaultAsync();
                    var icsValue = new IcsValueVM()
                    {
                        Id = id,
                        IcsValue = await GetIcsValueAsync((Guid?)id, poDate)
                    };
                    icsValueList.Add(icsValue);
                }
            }
            return icsValueList;
        }

        /*
         * Insert all Set Ids of selectedIds to a List with corresponding IcsValue
         */
        public async ValueTask<List<IcsValueVM>> SetIcsValueSetAsync(string[] selectedItemExtnIds)
        {
            var icsValueList = new List<IcsValueVM>();
            foreach (var selectedItemExtnId in selectedItemExtnIds)
            {
                var id = Guid.Parse(selectedItemExtnId);
                var icsParItem = await _db.IcsParItems.Include(i => i.PsCardItemExtn).FirstOrDefaultAsync(f => f.PsCardItemExtnId == id);
                if (icsParItem == null) // no ICS/PAR
                {
                    var psCardItemId = icsParItem.PsCardItemExtn.PsCardItemId;
                    var unitGroupItem = await _db.PsCardItemUnitGroupDescriptionItems.Include(i => i.PsCardItemUnitGroupDescription.PsCardItemUnitGroup).FirstOrDefaultAsync(f => f.PsCardItemId == psCardItemId);
                    var unitGroupId = unitGroupItem.PsCardItemUnitGroupDescription.UnitGroupId; 
                    // Store Set/Lot Group Id, if not yet inside the list
                    if (!icsValueList.Any(a => a.Id == unitGroupId))
                    {
                        var poNo = unitGroupItem.PsCardItemUnitGroupDescription.PsCardItemUnitGroup.PoNo;
                        var poDate = (await _db.Orders.FirstOrDefaultAsync(f => f.PoNo == poNo)).PoDate;
                        var iv = await GetIcsValueSetAsync(unitGroupId, poDate);
                        var icsValue = new IcsValueVM()
                        {
                            Id = (Guid)unitGroupId,
                            IcsValue = iv
                        };
                        icsValueList.Add(icsValue);
                    }
                }
            }
            return icsValueList;
        }

        private async ValueTask<IcsValue> GetIcsValueSetAsync(Guid? unitGroupId, DateTime? poDate)
        {
            var SPHV = GetSPHV(poDate);
            var gUnitCost = (await _db.PsCardItemUnitGroups.AsNoTracking().FirstOrDefaultAsync(w => w.Id == unitGroupId)).UnitCost;
            if (gUnitCost < SPHV)
            {
                return IcsValue.SPLV;
            }
            return IcsValue.SPHV;
        }

        /*
         * TO DO:
         * Update unitCost based on Acquisition Cost, i.e. UnitCost plus Additional Cost
         */
        private async ValueTask<IcsValue> GetIcsValueAsync(Guid? psCardItemExtnId, DateTime? poDate)
        {
            var SPHV = GetSPHV(poDate);
            var unitCost = (await _db.PsCardItemExtns.Include(i => i.PsCardItem).AsNoTracking().FirstOrDefaultAsync(f => f.Id == psCardItemExtnId)).PsCardItem.UnitCost;
            if (unitCost < SPHV)
            {
                return IcsValue.SPLV;
            }
            return IcsValue.SPHV;
        }

        /*
         * Generate Batch ICS for each Item of same PO Number (Contained in cardItemIdList)
         * 
         */

        public ValueTask<IcsPar> PostAsync(string parNo, string user, DateTime date) => _icsParExceptionService.TryCatch(async () =>
        {
            return await _icsParSharedService.PostAsync(parNo, "I", user, date);
        });

        public ValueTask<IcsPar> UnPostAsync(string parNo, string user, DateTime date) => _icsParExceptionService.TryCatch(async () =>
        {
            return await _icsParSharedService.UnPostAsync(parNo, "I", user, date);
        });

        private string NextPropNo(string acqYear, string stockNo, string locationCode, string refType)
        {
            var propNo = _db.Database.SqlQuery<string>("Exec PoIssuance_GetNextSeqNo {0}, {1}, {2}, {3}", acqYear, stockNo, locationCode, refType).ToList();
            return propNo.LastOrDefault();
        }

        public async ValueTask<string> NextRefNoAsync(DateTime parDate, string refType, IcsValue icsValue)
        {
            return await _icsSharedService.NextRefNoAsync(parDate, refType, icsValue);
        }

        private string RefTypeDesc(string refType)
        {
            return refType == "P" ? "PAR" : "ICS";
        }

        private async Task<bool> IsWwithUploadAsync(Guid? groupId)
        {
            var result = await _db.Uploads.AnyAsync(a => a.ImageId == groupId);
            return result;
        }

        private async Task ValidateUploadAsync(Guid? groupId, string article)
        {
            if (!await IsWwithUploadAsync(groupId))
            {
                throw new InvalidValueException($"No uploaded files found for {article}, cannot post!");
            }
        }

        public ValueTask<PsCardItemUnitGroupDescriptionItem> UpdateNoICSAsync(PsCardItemUnitGroupDescriptionItem model, string user, DateTime date)
        => _psCardItemUnitGroupDescriptionItemExceptionService.TryCatch(async () =>
        {
            var parIcsItemVm = await GetItemByIdAsync(model.PsCardItemId);
            await _psCardItemService.UpdateNoICSAsync(parIcsItemVm, user, date);
            return model;
        });

        private void ValidateRecord(PsCardItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(PsCardItem entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(PsCardItem entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}