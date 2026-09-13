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

namespace iLgs.Services.ParIcs
{
    public interface IParService
    {
        IQueryable<ParVM> GetAll();
        IQueryable<ParIcsPOGroupVM> GetAllPo();
        Task<ParIcsPOGroupVM> GetByPoNoAsync(string poNo);

        IQueryable<ParIcsPOGroupVM> GetAllPo(int? forYear, int? source);
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo();
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text);

        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        //IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo);
        //IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        //IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        //IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);

        ValueTask<ParVM> GetByIdAsync(Guid? id);

        Task<ParBundleBuilderInitVM> GetBundleBuilderDataAsync(Guid psCardItemId);
        Task<ParBundleBuilderInitVM> GetSingleUnitBundleDataAsync(Guid mainPsCardItemExtnId);
        ValueTask<GenerateIcsParVM> GeneratePAR(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateParSet(GenerateIcsParVM model, string user, DateTime date);

        ValueTask<IcsPar> PostAsync(string refNo, string user, DateTime date);
        ValueTask<IcsPar> UnPostAsync(string refNo, string user, DateTime date);

        ValueTask<string> NextRefNoAsync(DateTime parDate, string refType);

        IParItemService ParItem { get; }
    }

    internal class ParService : BaseValidator, IParService
    {

        private sealed class PhysicalItemInfo
        {
            public string SerialNo { get; set; }
            public string Model { get; set; }
            public string Brand { get; set; }
        }

        private static PhysicalItemInfo GetPhysicalItemInfo(PsCardItemExtn extn, AllField allField = null)
        {
            var result = new PhysicalItemInfo();
            if (extn == null)
                return result;

            // SerialNo comes from the physical extension type.
            var vehicle = extn as PsCardItemExtnVehicle;
            if (vehicle != null)
            {
                result.SerialNo = !string.IsNullOrWhiteSpace(vehicle.PlateNo) ? vehicle.PlateNo : vehicle.ConductionNo;
            }
            else
            {
                var other = extn as PsCardItemExtnOther;
                result.SerialNo = other != null ? other.SerialNo : extn.SeriesNo;
            }

            // Brand and Model come from PsCard.AllField, NOT from PsCardItemExtn.
            // The caller must supply the AllField entity fetched via PsCardItem.PsCard.AllField.
            if (allField != null)
            {
                if (!string.IsNullOrWhiteSpace(allField.Brand))
                    result.Brand = allField.Brand.Trim();
                if (!string.IsNullOrWhiteSpace(allField.Model_))
                    result.Model = allField.Model_.Trim();
            }

            return result;
        }

        private static string GetSerialNo(PsCardItemExtn extn)
        {
            return GetPhysicalItemInfo(extn).SerialNo;
        }

        private static string GetModel(PsCardItemExtn extn, AllField allField = null)
        {
            return GetPhysicalItemInfo(extn, allField).Model;
        }

        private static string GetBrand(PsCardItemExtn extn, AllField allField = null)
        {
            return GetPhysicalItemInfo(extn, allField).Brand;
        }
        private readonly AppManEntities _db;
        private decimal? _priceCap;

        private readonly IParItemService _parItemService;
        private readonly IExceptionService<GenerateIcsParVM> _generateParExceptionService;
        private readonly IExceptionService<PsCardItem> _postExceptionService;
        private readonly IExceptionService<IcsPar> _icsParExceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IIcsParSharedService _icsParSharedService;
        private readonly IPriceCapService _priceCapService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public ParService(AppManEntities db)
        {
            _db = db;
            _parItemService = new ParItemService(_db);
            _generateParExceptionService = new ExceptionService<GenerateIcsParVM>();
            _postExceptionService = new ExceptionService<PsCardItem>();
            _icsParExceptionService = new ExceptionService<IcsPar>();
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _icsParSharedService = new IcsParSharedService(_db);
            _priceCapService = new PriceCapService(_db);
        }

        //public ParService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    IParItemService parItemService,
        //    IExceptionService<GenerateIcsParVM> generateParExceptionService,
        //    IExceptionService<PsCardItem> postExceptionService,
        //    IExceptionService<IcsPar> icsParExceptionService,
        //    IPsCardItemTransactionService psCardItemTransactionService,
        //    IIcsParSharedService icsParSharedService,
        //    IPriceCapService priceCapService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _parItemService = parItemService;
        //    _generateParExceptionService = generateParExceptionService;
        //    _postExceptionService = postExceptionService;
        //    _icsParExceptionService = icsParExceptionService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
        //    _psCardItemTransactionService = psCardItemTransactionService;
        //    _icsParSharedService = icsParSharedService;
        //    _priceCapService = priceCapService;            
        //}

        public IParItemService ParItem { get { return _parItemService; } }

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap() ?? 50000).Value;
        }

        private decimal GetPriceCap(int? forYear)
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap(forYear) ?? 50000).Value;
        }

        public IQueryable<ParVM> GetAll()
        {
            var priceCap = GetPriceCap();
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null
                    //&& (w.OrderItemRequest.OrderItem.OrderItemUnitGroupDescriptionItems
                    //    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= priceCap)
                    //        || w.UnitCost >= priceCap)
                            && w.UnitCost >= priceCap)
                .Select(s => new ParVM
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    CardNo = s.PsCard.PsNo,
                    OrderItemId = s.OrderItemRequest.OrderItemId,
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
                    //AddCost = s.AddCost,
                    //GTotalCost = s.GTotalCost,
                    AddCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AddCost) ?? 0,
                    GTotalCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AcqCost) ?? 0,
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
                    //ParBalance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) -
                    // (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    ParBalance = (int?)s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    //OrderItemUnitGroupDescriptionItem = s.OrderItemRequest.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemRequest.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            var priceCap = GetPriceCap();
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'P', {0}, 0", priceCap).AsQueryable();
            return data;
        }

        public async Task<ParIcsPOGroupVM> GetByPoNoAsync(string poNo)
        {
            var data = await _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'P', NULL, NULL, 0, {0}", poNo).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo(int? forYear, int? source)
        {
            var priceCap = GetPriceCap(forYear);
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'P', {0}, {1}, {2}", priceCap, forYear, source).AsQueryable();
            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPoCombo()
        {

            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPoByText ''").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text)
        {
            //var data = _db.PsCardItems.AsNoTracking()
            //    .Where(w => (w.OrderItem.OrderItemUnitGroupDescriptionItems
            //        .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
            //        || w.UnitCost >= _parPrice) && (w.PoNo.Contains(text) || w.AirNo.Contains(text) ||  w.Codextn.Description.Contains(text)))
            //    .Select(s => new
            //    {
            //        s.PoNo,
            //        s.PoDate,
            //        s.AirDate,
            //        s.AirNo,
            //        s.DeptId,
            //        s.Codextn.Description
            //    }).GroupBy(g => new { g.DeptId, g.Description, g.PoNo, g.PoDate, g.AirNo, g.AirDate })
            //    .Select(s => new ParIcsPOGroupVM
            //    {
            //        Id = Guid.NewGuid(),
            //        PoNo = s.Key.PoNo,
            //        PoDate = s.Key.PoDate,
            //        AirNo = s.Key.AirNo,
            //        AirDate = s.Key.AirDate,
            //        DeptId = s.Key.DeptId,
            //        Department = s.Key.Description
            //    })
            //    .AsQueryable().Take(100);
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPoBytext {0}", text).AsQueryable();
            return data;
        }

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo)
        {
            return GetItemsByPoNo(poNo, null, null);
        }

        //public async Task<IList<ParIcsItemVm>> GetItemsByPoNoAsyncNew(string poNo, DateTime? poDate, Guid? deptId)
        //{
        //    var priceCap = GetPriceCap();

        //    var data = await _db.Database.SqlQuery<ParIcsItemVm>("Exec ParIcs_GetParPoItems {0}, {1}, {2}, {3}", poNo, poDate, deptId, priceCap).ToListAsync();

        //    return data;
        //}

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId)
        {
            var priceCap = GetPriceCap();

            var data = _db.PsCardItems.Include(i => i.PsCard.ItemCode)
                .AsNoTracking()
                .Where(w =>
                    w.PoNo == (string.IsNullOrEmpty(poNo) ? w.PoNo : poNo)
                    && w.PoDate == (poDate == null ? w.PoDate : poDate)
                    && w.DeptId == (deptId == null ? w.DeptId : deptId)
                    && w.UnitCost >= priceCap
                    //&& !w.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == w.Id)
                )
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    CardNo = s.PsCard.PsNo,
                    Qty = (int?)s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    AddCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AddCost) ?? 0,
                    GTotalCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AcqCost) ?? 0,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    IsForICS = s.IsForICS,
                    GeneratedItems = (_db.IcsParItems.Where(w => !w.IcsPar.IcsParUpdates.Any() && w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution,
                    ParPostedBy = s.ParPostedBy,
                    ParPostedDt = s.ParPostedDt
                }).AsQueryable();
            return data;
        }

        //public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo)
        //{
        //    return GetItemSetsByPoNo(poNo, null, null);
        //}

        //public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId)
        //{
        //    var priceCap = GetPriceCap();
        //    var data = _db.PsCardItemUnitGroups.AsNoTracking()
        //        .Where(w => w.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroupDescriptionItems
        //            .Any(b => b.PsCardItem.PoNo == (string.IsNullOrEmpty(poNo) ? b.PsCardItem.PoNo : poNo)
        //                && b.PsCardItem.PoDate == (poDate == null ? b.PsCardItem.PoDate : poDate)
        //                && b.PsCardItem.DeptId == (deptId == null ? b.PsCardItem.DeptId : deptId)
        //                ))
        //        && w.UnitCost >= priceCap)
        //        .Select(s => new ParIcsItemSetVm
        //        {
        //            Id = s.Id,
        //            Qty = s.Qty,
        //            Unit = s.Unit,
        //            UnitCost = s.UnitCost,
        //            TotalCost = s.TotalCost,
        //            AddCost = _db.PsCardItemExtns.Where(w => s.PsCardItemUnitGroupDescriptions
        //                .Any(a => a.PsCardItemUnitGroupDescriptionItems.Any(b => b.PsCardItemId == w.PsCardItemId))).Sum(x => x.AddCost) ?? 0,
        //            GTotalCost = _db.PsCardItemExtns.Where(w => s.PsCardItemUnitGroupDescriptions
        //                .Any(a => a.PsCardItemUnitGroupDescriptionItems.Any(b => b.PsCardItemId == w.PsCardItemId))).Sum(x => x.AcqCost) ?? 0,
        //            InsertedDt = s.InsertedDt,
        //            UnitGroupDescriptions = s.PsCardItemUnitGroupDescriptions,
        //            SetPostedBy = s.PostedBy,
        //            SetPostedDt = s.PostedDt
        //        }).AsQueryable();
        //    return data;
        //}

        //public IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId)
        //{
        //    var data = _db.PsCardItemUnitGroupDescriptions.AsNoTracking()
        //        .Where(w => w.UnitGroupId == unitGroupId).AsQueryable();
        //    return data;
        //}

        //public IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId)
        //{
        //    var data = _db.PsCardItems.AsNoTracking()
        //        .Where(w => w.PsCardItemUnitGroupDescriptionItems.Any(a => a.UnitGroupDescriptionId == unitGroupDescriptionId && a.PsCardItemId == w.Id))
        //        .Select(s => new ParIcsItemVm
        //        {
        //            Id = s.Id,
        //            GroupId = s.GroupId,
        //            PsCardId = s.PsCardId,
        //            //Qty = s.Qty,
        //            Qty = s.PsCardItemUnitGroupDescriptionItems.FirstOrDefault(f => f.PsCardItemId == s.Id).PoQty,
        //            TotalQty = s.PsCardItemUnitGroupDescriptionItems.FirstOrDefault(f => f.PsCardItemId == s.Id).PoQty
        //                * s.PsCardItemUnitGroupDescriptionItems.FirstOrDefault(f => f.PsCardItemId == s.Id).PsCardItemUnitGroupDescription.PsCardItemUnitGroup.Qty,
        //            Unit = s.Unit,
        //            UnitCost = s.UnitCost,
        //            TotalCost = s.Amount,
        //            //AddCost = s.AddCost,
        //            //GTotalCost = s.GTotalCost,
        //            AddCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AddCost) ?? 0,
        //            GTotalCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AcqCost) ?? 0,
        //            Article = s.PsCard.ItemCode.Description,
        //            Description = s.Description,
        //            StockNo = s.PsCard.PsNo,
        //            IsForICS = s.IsForICS,
        //            IsConsumable = s.IsConsumable,
        //            IsIncorporated = s.IsIncorporated,
        //            IsOthers = s.IsOthers,
        //            OtherRemarks = s.OtherRemarks,
        //            GeneratedItems = (_db.IcsParItems.Where(w => !w.IcsPar.IcsParUpdates.Any() && w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
        //            InsertedDt = s.InsertedDt,
        //            ParPostedBy = s.ParPostedBy,
        //            ParPostedDt = s.ParPostedDt
        //        }).AsQueryable();
        //    return data;
        //}

        public async ValueTask<ParVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems.Where(w => w.Id == id)
                .Select(s => new ParVM
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    CardNo = s.PsCard.PsNo,
                    OrderItemId = s.OrderItemRequest.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    Qty = (int?)s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    //AddCost = s.AddCost,
                    //GTotalCost = s.GTotalCost,
                    AddCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AddCost) ?? 0,
                    GTotalCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AcqCost) ?? 0,
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
                    ParBalance = (int?)s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    //OrderItemUnitGroupDescriptionItem = s.OrderItemRequest.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemRequest.OrderItemId),
                    IsForICS = s.IsForICS,
                    AcqDate = s.AcqDate
                }).FirstOrDefaultAsync();
            return data;
        }

        public async Task<ParBundleBuilderInitVM> GetSingleUnitBundleDataAsync(Guid mainPsCardItemExtnId)
        {
            var mainExtn = await _db.PsCardItemExtns
                .Include(e => e.PsCardItem.PsCard.ItemCode)
                .Include(e => e.PsCardItem.PsCard.AllField)
                .Include(e => e.IcsParItems.Select(i => i.IcsPar.IcsParUpdates))
                .Include(e => e.IcsParItems.Select(i => i.IcsParItemComponents))
                .FirstOrDefaultAsync(e => e.Id == mainPsCardItemExtnId);

            if (mainExtn == null || !mainExtn.PsCardItemId.HasValue)
            {
                throw new RecordNotFoundException(mainPsCardItemExtnId);
            }

            var psCardItemId = mainExtn.PsCardItemId.Value;
            var cardItem = await GetByIdAsync(psCardItemId);
            if (cardItem == null)
            {
                throw new RecordNotFoundException(psCardItemId);
            }

            AllField allField = null;
            if (cardItem.PsCardId.HasValue)
            {
                allField = await _db.PsCards
                    .Where(c => c.Id == cardItem.PsCardId.Value)
                    .Select(c => c.AllField)
                    .FirstOrDefaultAsync();
            }

            // Check if an active Draft PAR exists for this unit
            var existingParItem = mainExtn.IcsParItems
                .Where(i => i.IcsPar != null && i.IcsPar.RefType == "P" && i.IcsPar.PostedDt == null)
                .OrderByDescending(i => i.IcsPar.InsertedDt)
                .FirstOrDefault();

            var existingPreselectedSerialIds = new List<Guid>();
            var existingPreselectedQuantities = new Dictionary<Guid, decimal>();
            Guid? existingParId = null;
            string existingParNo = null;
            Guid? existingReceivedById = null;
            string existingIssuedTo = null;
            Guid? existingLocationId = null;
            string existingLocation = null;
            string existingLocationCode = null;
            DateTime? existingRefDate = null;
            string existingReceivedBy = null;
            string existingReceivedByTitle = null;
            string existingReceivedByTitle2 = null;
            string existingReceivedByPosition = null;
            string existingReceivedDept = null;
            DateTime? existingReceivedDate = null;
            string existingDesignation = null;
            string existingIssuedBy = null;
            string existingIssuedByPosition = null;
            string existingIssuedDept = null;
            DateTime? existingIssuedDate = null;

            if (existingParItem != null && existingParItem.IcsPar != null)
            {
                var par = existingParItem.IcsPar;
                existingParId = par.Id;
                existingParNo = par.RefNo;
                existingRefDate = par.RefDate;
                existingReceivedById = par.ReceivedById;
                existingReceivedBy = par.ReceivedBy;
                existingReceivedByTitle = par.ReceivedByTitle;
                existingReceivedByTitle2 = par.ReceivedByTitle2;
                existingReceivedByPosition = par.ReceivedByPosition;
                existingReceivedDept = par.ReceivedDept;
                existingReceivedDate = par.ReceivedDate;
                existingIssuedTo = !string.IsNullOrWhiteSpace(existingParItem.IssuedTo) ? existingParItem.IssuedTo : par.ReceivedBy;
                existingDesignation = existingParItem.Designation;
                existingIssuedBy = par.IssuedBy;
                existingIssuedByPosition = par.IssuedByPosition;
                existingIssuedDept = par.IssuedDept;
                existingIssuedDate = par.IssuedDate;
                existingLocationId = par.LocationId;
                existingLocationCode = par.LocationCode;
                existingLocation = par.Location;

                if (existingParItem.IcsParItemComponents != null)
                {
                    foreach (var comp in existingParItem.IcsParItemComponents)
                    {
                        if (comp.PsCardItemExtnId.HasValue)
                        {
                            existingPreselectedSerialIds.Add(comp.PsCardItemExtnId.Value);
                        }
                        if (comp.PsCardSubItemId != null)
                        {
                            var sId = comp.PsCardSubItemId;
                            if (!existingPreselectedQuantities.ContainsKey(sId))
                            {
                                existingPreselectedQuantities[sId] = 0;
                            }
                            existingPreselectedQuantities[sId] += comp.Qty;
                        }
                    }
                }
            }

            Guid? draftParItemId = existingParItem != null ? (Guid?)existingParItem.Id : null;

            // Component requirements from PsCardSubItems
            var subItems = await _db.PsCardSubItems
                .Where(s => s.PsCardItemId == psCardItemId)
                .OrderBy(s => s.IsRequiredForBundle == true ? 0 : 1)
                .ThenBy(s => s.SubItemNo)
                .ToListAsync();

            var components = new List<ParComponentInventoryVM>();
            foreach (var sub in subItems)
            {
                var extns = await _db.PsCardItemExtns
                    .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == sub.Id)
                    .ToListAsync();
                extns = extns.OrderBy(e => GetSerialNo(e)).ToList();

                bool isSerialized = extns.Any();

                var vm = new ParComponentInventoryVM
                {
                    PsCardSubItemId = sub.Id,
                    SubItemNo = sub.SubItemNo,
                    ItemCode = sub.SubItemNo,
                    Description = sub.Description,
                    Unit = sub.Unit,
                    SourceType = sub.SourceType,
                    IsRequiredForBundle = sub.IsRequiredForBundle ?? false,
                    QtyPerParent = sub.QtyPerParent ?? 1,
                    TotalQty = sub.Qty,
                    IsSerialized = isSerialized
                };

                if (isSerialized)
                {
                    // Reserved components: those in active PAR items EXCEPT if assigned to THIS unit's existing draft PAR
                    var reservedExtnIds = await _db.IcsParItemComponents
                        .Where(c => c.PsCardSubItemId == sub.Id && c.PsCardItemExtnId != null && c.IcsParItem.IcsPar.RefType == "P"
                            && (!draftParItemId.HasValue || c.IcsParItemId != draftParItemId.Value))
                        .Select(c => c.PsCardItemExtnId.Value)
                        .ToListAsync();

                    var availableExtns = extns.Where(e => !reservedExtnIds.Contains(e.Id)).ToList();
                    vm.AllocatedQty = (decimal)reservedExtnIds.Count;
                    vm.AvailableQty = (decimal)availableExtns.Count;
                    vm.AvailableSerials = availableExtns.Select(e => new ParComponentSerialVM
                    {
                        PsCardItemExtnId = e.Id,
                        SerialNo = GetSerialNo(e),
                        PropNo = e.PropNo,
                        Brand = GetBrand(e, allField),
                        Model = GetModel(e, allField),
                        Condition = e.Condition
                    }).ToList();
                }
                else
                {
                    var reservedQty = await _db.IcsParItemComponents
                        .Where(c => c.PsCardSubItemId == sub.Id && c.PsCardItemExtnId == null && c.IcsParItem.IcsPar.RefType == "P"
                            && (!draftParItemId.HasValue || c.IcsParItemId != draftParItemId.Value))
                        .SumAsync(c => (decimal?)c.Qty) ?? 0;

                    vm.AllocatedQty = reservedQty;
                    vm.AvailableQty = Math.Max(0, sub.Qty - reservedQty);
                    vm.AvailableSerials = new List<ParComponentSerialVM>();
                }

                components.Add(vm);
            }

            var unitSerial = GetSerialNo(mainExtn);
            var unitCost = mainExtn.PsCardItem.UnitCost ?? cardItem.UnitCost ?? 0;
            var addCost = mainExtn.AddCost ?? 0;
            var acqCost = mainExtn.AcqCost ?? (unitCost + addCost);

            return new ParBundleBuilderInitVM
            {
                PsCardItemId = psCardItemId,
                MainPsCardItemExtnId = mainExtn.Id,
                SerialNo = unitSerial,
                PropNo = mainExtn.PropNo ?? "Unassigned",
                Description = cardItem.Description,
                StockNo = mainExtn.PsCardItem.PsCard != null ? mainExtn.PsCardItem.PsCard.PsNo : null,
                PoNo = cardItem.PoNo,
                UnitCost = unitCost,
                AddCost = addCost,
                AcqCost = acqCost,
                Unit = cardItem.Unit,
                Brand = allField != null ? allField.Brand : null,
                Model = allField != null ? allField.Model_ : null,
                Components = components,
                ExistingParId = existingParId,
                ExistingParNo = existingParNo,
                ExistingReceivedById = existingReceivedById,
                ExistingIssuedTo = existingIssuedTo,
                ExistingLocationId = existingLocationId,
                ExistingLocation = existingLocation,
                ExistingLocationCode = existingLocationCode,
                ExistingRefDate = existingRefDate,
                ExistingReceivedBy = existingReceivedBy,
                ExistingReceivedByTitle = existingReceivedByTitle,
                ExistingReceivedByTitle2 = existingReceivedByTitle2,
                ExistingReceivedByPosition = existingReceivedByPosition,
                ExistingReceivedDept = existingReceivedDept,
                ExistingReceivedDate = existingReceivedDate,
                ExistingDesignation = existingDesignation,
                ExistingIssuedBy = existingIssuedBy,
                ExistingIssuedByPosition = existingIssuedByPosition,
                ExistingIssuedDept = existingIssuedDept,
                ExistingIssuedDate = existingIssuedDate,
                ExistingPreselectedSerialIds = existingPreselectedSerialIds,
                ExistingPreselectedQuantities = existingPreselectedQuantities
            };
        }

        public async Task<ParBundleBuilderInitVM> GetBundleBuilderDataAsync(Guid psCardItemId)
        {
            var cardItem = await GetByIdAsync(psCardItemId);
            if (cardItem == null)
            {
                throw new RecordNotFoundException(psCardItemId);
            }

            // Load AllField for this PsCard so that Brand and Model can be read correctly.
            // Brand/Model must NOT be read from PsCardItemExtn; they live on PsCard.AllField.
            AllField allField = null;
            string cardNo = null;
            if (cardItem.PsCardId.HasValue)
            {
                var cardData = await _db.PsCards
                    .Include(c => c.AllField)
                    .FirstOrDefaultAsync(c => c.Id == cardItem.PsCardId.Value);
                if (cardData != null)
                {
                    cardNo = cardData.PsNo;
                    allField = cardData.AllField;
                }
            }

            // Available main physical units: PsCardSubItemId == null and not already in an active PAR.
            var activeParItemExtnIds = await _db.IcsParItems
                .Where(i => i.IcsPar.RefType == "P")
                .Select(i => i.PsCardItemExtnId)
                .ToListAsync();

            var mainUnits = await _db.PsCardItemExtns
                .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == null && !activeParItemExtnIds.Contains(e.Id))
                .OrderBy(e => e.PropSeq)
                .ToListAsync();

            // Brand and Model for all main units sharing this PsCardItem come from the same PsCard.AllField.
            var mainUnitVMs = mainUnits
                .OrderBy(e => e.PropSeq).ThenBy(e => GetSerialNo(e))
                .Select(e => new ParMainUnitInventoryVM
                {
                    PsCardItemExtnId = e.Id,
                    SerialNo = GetSerialNo(e),
                    PropNo = e.PropNo,
                    Brand = GetBrand(e, allField),
                    Model = GetModel(e, allField),
                    AcqCost = e.AcqCost ?? 0,
                    Condition = e.Condition,
                    Remarks = e.Remarks
                })
                .ToList();

            // Component requirements from PsCardSubItems for this PsCardItem.
            var subItems = await _db.PsCardSubItems
                .Where(s => s.PsCardItemId == psCardItemId)
                .OrderBy(s => s.IsRequiredForBundle == true ? 0 : 1)
                .ThenBy(s => s.SubItemNo)
                .ToListAsync();

            var components = new List<ParComponentInventoryVM>();
            foreach (var sub in subItems)
            {
                // Physical component units are PsCardItemExtns linked via PsCardSubItemId.
                var extns = await _db.PsCardItemExtns
                    .Where(e => e.PsCardItemId == psCardItemId && e.PsCardSubItemId == sub.Id)
                    .ToListAsync();
                extns = extns.OrderBy(e => GetSerialNo(e)).ToList();

                bool isSerialized = extns.Any();

                var vm = new ParComponentInventoryVM
                {
                    PsCardSubItemId = sub.Id,
                    SubItemNo = sub.SubItemNo,
                    ItemCode = sub.SubItemNo,
                    Description = sub.Description,
                    Unit = sub.Unit,
                    SourceType = sub.SourceType,
                    IsRequiredForBundle = sub.IsRequiredForBundle ?? false,
                    QtyPerParent = sub.QtyPerParent ?? 1,
                    TotalQty = sub.Qty,
                    IsSerialized = isSerialized
                };

                if (isSerialized)
                {
                    var reservedExtnIds = await _db.IcsParItemComponents
                        .Where(c => c.PsCardSubItemId == sub.Id && c.PsCardItemExtnId != null && c.IcsParItem.IcsPar.RefType == "P")
                        .Select(c => c.PsCardItemExtnId.Value)
                        .ToListAsync();

                    var availableExtns = extns.Where(e => !reservedExtnIds.Contains(e.Id)).ToList();
                    vm.AllocatedQty = (decimal)reservedExtnIds.Count;
                    vm.AvailableQty = (decimal)availableExtns.Count;
                    // Component serials share the same card-level AllField for Brand/Model display.
                    vm.AvailableSerials = availableExtns.Select(e => new ParComponentSerialVM
                    {
                        PsCardItemExtnId = e.Id,
                        SerialNo = GetSerialNo(e),
                        PropNo = e.PropNo,
                        Brand = GetBrand(e, allField),
                        Model = GetModel(e, allField),
                        Condition = e.Condition
                    }).ToList();
                }
                else
                {
                    var reservedQty = await _db.IcsParItemComponents
                        .Where(c => c.PsCardSubItemId == sub.Id && c.PsCardItemExtnId == null && c.IcsParItem.IcsPar.RefType == "P")
                        .SumAsync(c => (decimal?)c.Qty) ?? 0;

                    vm.AllocatedQty = reservedQty;
                    vm.AvailableQty = Math.Max(0, sub.Qty - reservedQty);
                    vm.AvailableSerials = new List<ParComponentSerialVM>();
                }

                components.Add(vm);
            }

            return new ParBundleBuilderInitVM
            {
                PsCardItemId = psCardItemId,
                Description = cardItem.Description,
                StockNo = cardNo,
                UnitCost = cardItem.UnitCost ?? 0,
                Unit = cardItem.Unit,
                Brand = allField != null ? allField.Brand : null,
                Model = allField != null ? allField.Model_ : null,
                AvailableMainUnits = mainUnitVMs,
                Components = components
            };
        }
        public ValueTask<GenerateIcsParVM> GeneratePAR(GenerateIcsParVM model, string user, DateTime date)
        {
            return _generateParExceptionService.TryCatch(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }
            if (model.Date == default(DateTime))
            {
                throw new InvalidValueException("Field PAR Date is required!");
            }
            if (model.IcsPar == null)
            {
                throw new InvalidValueException("Accountability and Assignment details are required.");
            }
            if (!model.IcsPar.ReceivedById.HasValue || model.IcsPar.ReceivedById.Value == Guid.Empty)
            {
                throw new InvalidValueException("Field Received By is required!");
            }
            if (string.IsNullOrWhiteSpace(model.IcsPar.ReceivedByPosition))
            {
                throw new InvalidValueException("Field Received By Position is required!");
            }
            if (string.IsNullOrWhiteSpace(model.IcsPar.ReceivedDept))
            {
                throw new InvalidValueException("Field Received By Department is required!");
            }
            if (!model.IcsPar.ReceivedDate.HasValue)
            {
                throw new InvalidValueException("Field Received Date is required!");
            }
            if (string.IsNullOrWhiteSpace(model.IcsPar.IssuedBy))
            {
                throw new InvalidValueException("Field Issued By is required!");
            }
            if (string.IsNullOrWhiteSpace(model.IcsPar.IssuedByPosition))
            {
                throw new InvalidValueException("Field Issued By Position is required!");
            }
            if (string.IsNullOrWhiteSpace(model.IcsPar.IssuedDept))
            {
                throw new InvalidValueException("Field Issued By Department is required!");
            }
            if (!model.IcsPar.IssuedDate.HasValue)
            {
                throw new InvalidValueException("Field Issued Date is required!");
            }

            // Resolve bundles from model.Bundles, BundleDataJson, model.MainPsCardItemExtnId, or SelectedIds
            List<ParBundleItemAllocationVM> bundles = model.Bundles;
            if ((bundles == null || !bundles.Any()) && !string.IsNullOrWhiteSpace(model.BundleDataJson))
            {
                bundles = JsonConvert.DeserializeObject<List<ParBundleItemAllocationVM>>(model.BundleDataJson);
            }

            if ((bundles == null || !bundles.Any()) && model.MainPsCardItemExtnId.HasValue && model.MainPsCardItemExtnId.Value != Guid.Empty)
            {
                bundles = new List<ParBundleItemAllocationVM>
                {
                    new ParBundleItemAllocationVM
                    {
                        MainPhysicalItemId = model.MainPsCardItemExtnId.Value,
                        Components = new List<ParBundleComponentAllocationVM>()
                    }
                };
            }

            if ((bundles == null || !bundles.Any()) && !string.IsNullOrWhiteSpace(model.SelectedIds))
            {
                var ids = model.SelectedIds.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                bundles = ids.Select(s => new ParBundleItemAllocationVM
                {
                    MainPhysicalItemId = Guid.Parse(s.Trim()),
                    Components = new List<ParBundleComponentAllocationVM>()
                }).ToList();
            }

            if (bundles == null || !bundles.Any())
            {
                throw new InvalidValueException("Please select a main physical unit before generating the PAR.");
            }

            // Fallback for PsCardItemId if only MainPsCardItemExtnId was passed
            if (!model.PsCardItemId.HasValue && bundles.Any())
            {
                var firstExtnId = bundles.First().MainPhysicalItemId;
                var extn = await _db.PsCardItemExtns.FirstOrDefaultAsync(e => e.Id == firstExtnId);
                if (extn != null)
                {
                    model.PsCardItemId = extn.PsCardItemId;
                }
            }

            var cardItem = await GetByIdAsync(model.PsCardItemId);
            if (cardItem == null)
            {
                throw new RecordNotFoundException((Guid)(model.PsCardItemId ?? Guid.Empty));
            }

            if (cardItem.IsForICS == true)
            {
                throw new InvalidValueException("Item is marked for ICS, cannot generate PAR!");
            }

            var mainExtnIds = bundles.Select(b => b.MainPhysicalItemId).ToList();

            if (model.ExistingParId.HasValue && bundles.Count != 1)
            {
                throw new InvalidValueException("A Draft PAR can only be updated for its existing main physical unit.");
            }

            var currentDraftParItemIds = await _db.IcsParItems
                .Where(i => mainExtnIds.Contains(i.PsCardItemExtnId.Value) && i.IcsPar.RefType == "P" && i.IcsPar.PostedDt == null)
                .Select(i => i.Id)
                .ToListAsync();

            var currentDraftCount = currentDraftParItemIds.Count;
            var effectiveBalance = (cardItem.ParBalance ?? 0) + currentDraftCount;

            if (effectiveBalance <= 0)
            {
                throw new InvalidValueException("All Items have PARs.");
            }

            if (bundles.Count > effectiveBalance)
            {
                throw new InvalidValueException("Cannot generate more than the available balance.");
            }

            var existingItem = await _db.IcsParItems.Include(it => it.IcsPar).AsNoTracking().Where(w => w.PsCardItemExtn.PsCardItemId == model.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync();
            var refType = existingItem != null && existingItem.IcsPar != null ? existingItem.IcsPar.RefType : null;
            if (!string.IsNullOrWhiteSpace(refType))
            {
                throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
            }

            string acqYear = "";
            if (cardItem.AcqDate != null)
            {
                acqYear = cardItem.AcqDate.Value.Year.ToString();
            }
            else if (cardItem.AirDate != null)
            {
                acqYear = cardItem.AirDate.Value.Year.ToString();
            }
            else if (cardItem.PoDate != null)
            {
                acqYear = cardItem.PoDate.Value.Year.ToString();
            }

            if (string.IsNullOrEmpty(acqYear))
            {
                throw new InvalidValueException("Acquisition Date is Required!");
            }

            // Load subitems / component definitions for this CardItem
            var subItems = await _db.PsCardSubItems
                .Where(s => s.PsCardItemId == model.PsCardItemId)
                .ToListAsync();
            var subItemMap = subItems.ToDictionary(s => s.Id);
            var requiredSubItems = subItems.Where(s => s.IsRequiredForBundle == true).ToList();

            // 1. Validate Main Physical Items
            // Load active reservations in DB
            var activeParExtnIds = await _db.IcsParItems
                .Where(i => i.IcsPar.RefType == "P")
                .Select(i => i.PsCardItemExtnId)
                .ToListAsync();

            var activeReservedComponentExtnIds = await _db.IcsParItemComponents
                .Where(c => c.PsCardItemExtnId != null && c.IcsParItem.IcsPar.RefType == "P" && !currentDraftParItemIds.Contains(c.IcsParItemId))
                .Select(c => c.PsCardItemExtnId.Value)
                .ToListAsync();

            if (mainExtnIds.Count != mainExtnIds.Distinct().Count())
            {
                throw new InvalidValueException("Duplicate main physical items specified in request.");
            }

            var mainExtns = await _db.PsCardItemExtns
                .Where(e => mainExtnIds.Contains(e.Id))
                .ToListAsync();

            if (mainExtns.Count != mainExtnIds.Count)
            {
                throw new InvalidValueException("One or more selected main physical units could not be found.");
            }

            if (model.ExistingParId.HasValue)
            {
                var lockedDraftItem = await _db.IcsParItems
                    .Include(i => i.IcsPar)
                    .AsNoTracking()
                    .SingleOrDefaultAsync(i => i.IcsParId == model.ExistingParId.Value && i.IcsPar.RefType == "P");

                if (lockedDraftItem == null)
                {
                    throw new InvalidValueException("The Draft PAR could not be found. Refresh the page and try again.");
                }
                if (lockedDraftItem.IcsPar.PostedDt != null)
                {
                    throw new InvalidValueException("This PAR has already been posted and can no longer be edited.");
                }
                if (!lockedDraftItem.PsCardItemExtnId.HasValue || lockedDraftItem.PsCardItemExtnId.Value != mainExtnIds.Single())
                {
                    throw new InvalidValueException("The main accountable physical unit of a Draft PAR cannot be changed. Delete the Draft and generate a new PAR for the correct unit.");
                }
            }

            foreach (var mainExtn in mainExtns)
            {
                if (mainExtn.PsCardItemId != model.PsCardItemId)
                {
                    throw new InvalidValueException(string.Format("Physical unit '{0}' does not belong to this card item.", GetSerialNo(mainExtn) ?? mainExtn.PropNo ?? mainExtn.Id.ToString()));
                }

                if (mainExtn.PsCardSubItemId != null)
                {
                    throw new InvalidValueException(string.Format("Physical unit '{0}' is a component and cannot be used as a main item.", GetSerialNo(mainExtn) ?? mainExtn.PropNo ?? mainExtn.Id.ToString()));
                }

                // Check if already assigned to a POSTED PAR
                if (activeParExtnIds.Contains(mainExtn.Id))
                {
                    var activePar = await _db.IcsPars
                        .Where(p => p.RefType == "P" && p.IcsParItems.Any(i => i.PsCardItemExtnId == mainExtn.Id))
                        .FirstOrDefaultAsync();
                    if (activePar != null && activePar.PostedDt != null)
                    {
                        throw new InvalidValueException(string.Format("This physical unit is already assigned to posted PAR {0}.", activePar.RefNo));
                    }
                }
            }

            // 2. Validate Serialized Components Reservations
            var allSerializedComponents = bundles
                .SelectMany(b => b.Components ?? Enumerable.Empty<ParBundleComponentAllocationVM>())
                .Where(c => c.PsCardItemExtnId.HasValue && c.PsCardItemExtnId.Value != Guid.Empty)
                .ToList();

            var allSubmittedComponents = bundles
                .SelectMany(b => b.Components ?? Enumerable.Empty<ParBundleComponentAllocationVM>())
                .ToList();

            if (allSubmittedComponents.Any(c => c.Qty <= 0))
            {
                throw new InvalidValueException("Component quantities must be greater than zero.");
            }
            if (allSubmittedComponents.Any(c => !subItemMap.ContainsKey(c.PsCardSubItemId)))
            {
                throw new InvalidValueException("One or more submitted components do not belong to this property item.");
            }

            var serializedSubItemIds = await _db.PsCardItemExtns
                .Where(e => e.PsCardItemId == model.PsCardItemId && e.PsCardSubItemId != null)
                .Select(e => e.PsCardSubItemId.Value)
                .Distinct()
                .ToListAsync();

            if (allSubmittedComponents.Any(c => serializedSubItemIds.Contains(c.PsCardSubItemId) && !c.PsCardItemExtnId.HasValue))
            {
                throw new InvalidValueException("A serialized component must be selected by its physical serial record.");
            }
            if (allSubmittedComponents.Any(c => !serializedSubItemIds.Contains(c.PsCardSubItemId) && c.PsCardItemExtnId.HasValue))
            {
                throw new InvalidValueException("A non-serialized component cannot reference a physical serial record.");
            }
            if (allSerializedComponents.Any(c => c.Qty != 1))
            {
                throw new InvalidValueException("Each serialized component selection must have a quantity of one.");
            }

            var hasDuplicateBulkComponent = bundles.Any(b =>
                (b.Components ?? new List<ParBundleComponentAllocationVM>())
                    .Where(c => !c.PsCardItemExtnId.HasValue || c.PsCardItemExtnId.Value == Guid.Empty)
                    .GroupBy(c => c.PsCardSubItemId)
                    .Any(g => g.Count() > 1));
            if (hasDuplicateBulkComponent)
            {
                throw new InvalidValueException("A non-serialized component may only be submitted once per bundle.");
            }

            var serializedExtnIds = allSerializedComponents.Select(c => c.PsCardItemExtnId.Value).ToList();
            if (serializedExtnIds.Count != serializedExtnIds.Distinct().Count())
            {
                throw new InvalidValueException("A serialized component cannot be assigned more than once across bundles.");
            }

            foreach (var sId in serializedExtnIds)
            {
                if (activeReservedComponentExtnIds.Contains(sId))
                {
                    var compExtn = await _db.PsCardItemExtns.FirstOrDefaultAsync(e => e.Id == sId);
                    throw new InvalidValueException(string.Format("Component serial '{0}' is already reserved in another PAR.", compExtn != null ? GetSerialNo(compExtn) : sId.ToString()));
                }
            }

            // Verify each serialized component belongs to its subitem & carditem
            foreach (var c in allSerializedComponents)
            {
                var compExtn = await _db.PsCardItemExtns.FirstOrDefaultAsync(e => e.Id == c.PsCardItemExtnId.Value);
                if (compExtn == null)
                {
                    throw new RecordNotFoundException(c.PsCardItemExtnId.Value);
                }
                if (compExtn.PsCardItemId != model.PsCardItemId || compExtn.PsCardSubItemId != c.PsCardSubItemId)
                {
                    var subDesc = subItemMap.ContainsKey(c.PsCardSubItemId) ? subItemMap[c.PsCardSubItemId].Description : "component";
                    throw new InvalidValueException(string.Format("Serial '{0}' does not belong to component '{1}'.", GetSerialNo(compExtn), subDesc));
                }
            }

            // 3. Validate Non-Serialized Quantities across entire request
            var nonSerializedComponents = bundles
                .SelectMany(b => b.Components ?? Enumerable.Empty<ParBundleComponentAllocationVM>())
                .Where(c => !c.PsCardItemExtnId.HasValue || c.PsCardItemExtnId.Value == Guid.Empty)
                .GroupBy(c => c.PsCardSubItemId)
                .ToList();

            foreach (var group in nonSerializedComponents)
            {
                var subId = group.Key;
                if (!subItemMap.ContainsKey(subId))
                {
                    throw new RecordNotFoundException(subId);
                }
                var sub = subItemMap[subId];
                var totalRequested = group.Sum(x => x.Qty);

                var reservedQty = await _db.IcsParItemComponents
                    .Where(c => c.PsCardSubItemId == subId && c.PsCardItemExtnId == null && c.IcsParItem.IcsPar.RefType == "P"
                        && !currentDraftParItemIds.Contains(c.IcsParItemId))
                    .SumAsync(c => (decimal?)c.Qty) ?? 0;

                var availableQty = Math.Max(0, sub.Qty - reservedQty);
                if (totalRequested > availableQty)
                {
                    throw new InvalidValueException(string.Format("Insufficient quantity for component '{0}'. Available: {1:G29}, Requested: {2:G29}.", sub.Description, availableQty, totalRequested));
                }
            }

            // 4. Validate Per-Bundle Completeness (Section 10)
            foreach (var bundle in bundles)
            {
                var mainExtn = mainExtns.First(m => m.Id == bundle.MainPhysicalItemId);
                var bundleComps = bundle.Components ?? new List<ParBundleComponentAllocationVM>();

                foreach (var reqSub in requiredSubItems)
                {
                    var requiredQty = reqSub.QtyPerParent ?? 1;
                    var allocatedQty = bundleComps
                        .Where(c => c.PsCardSubItemId == reqSub.Id)
                        .Sum(c => c.Qty);

                    if (allocatedQty < requiredQty)
                    {
                        throw new InvalidValueException(string.Format("Bundle for unit '{0}' is incomplete: Required component '{1}' requires {2:G29} but only {3:G29} was allocated.", GetSerialNo(mainExtn) ?? mainExtn.PropNo ?? "Unit", reqSub.Description, requiredQty, allocatedQty));
                    }
                }
            }

            // 5. Transactional Execution
            using (var transaction = _db.Database.BeginTransaction())
            {
                try
                {
                    var conflictingSerializedComponent = await _db.IcsParItemComponents
                        .Where(c => c.PsCardItemExtnId != null &&
                            serializedExtnIds.Contains(c.PsCardItemExtnId.Value) &&
                            c.IcsParItem.IcsPar.RefType == "P" &&
                            !currentDraftParItemIds.Contains(c.IcsParItemId))
                        .Select(c => c.PsCardItemExtnId)
                        .FirstOrDefaultAsync();
                    if (conflictingSerializedComponent.HasValue)
                    {
                        throw new InvalidValueException("A selected serialized component was reserved by another PAR. Refresh the component list and try again.");
                    }

                    foreach (var group in nonSerializedComponents)
                    {
                        var sub = subItemMap[group.Key];
                        var reservedQty = await _db.IcsParItemComponents
                            .Where(c => c.PsCardSubItemId == group.Key && c.PsCardItemExtnId == null &&
                                c.IcsParItem.IcsPar.RefType == "P" &&
                                !currentDraftParItemIds.Contains(c.IcsParItemId))
                            .SumAsync(c => (decimal?)c.Qty) ?? 0;
                        if (group.Sum(c => c.Qty) > Math.Max(0, sub.Qty - reservedQty))
                        {
                            throw new InvalidValueException(string.Format("Component '{0}' was reserved by another PAR. Refresh the component list and try again.", sub.Description));
                        }
                    }

                    foreach (var bundle in bundles)
                    {
                        var psCardItemExtn = mainExtns.First(m => m.Id == bundle.MainPhysicalItemId);
                        var existingDraftQuery = _db.IcsParItems
                            .Include(it => it.IcsPar)
                            .Include(it => it.IcsParItemComponents);
                        var existingDraftParItem = model.ExistingParId.HasValue
                            ? await existingDraftQuery.SingleOrDefaultAsync(it => it.IcsParId == model.ExistingParId.Value && it.PsCardItemExtnId == psCardItemExtn.Id && it.IcsPar.RefType == "P")
                            : await existingDraftQuery.FirstOrDefaultAsync(it => it.PsCardItemExtnId == psCardItemExtn.Id && it.IcsPar.RefType == "P" && it.IcsPar.PostedDt == null);

                        IcsPar icsPar = null;
                        IcsParItem icsParItem = null;

                        var locCode = !string.IsNullOrWhiteSpace(model.LocationCode) ? model.LocationCode.Trim() : (model.Location != null ? model.Location.Trim() : null);
                        var assignedTo = !string.IsNullOrWhiteSpace(model.IssuedTo) ? model.IssuedTo.Trim() : (model.IcsPar != null && model.IcsPar.ReceivedBy != null ? model.IcsPar.ReceivedBy.Trim() : null);
                        var assignedDesig = !string.IsNullOrWhiteSpace(model.Designation) ? model.Designation.Trim() : (model.IcsPar != null && model.IcsPar.ReceivedByPosition != null ? model.IcsPar.ReceivedByPosition.Trim() : null);

                        if (existingDraftParItem != null)
                        {
                            icsPar = existingDraftParItem.IcsPar;
                            icsParItem = existingDraftParItem;

                            if (icsPar.PostedDt != null)
                            {
                                throw new InvalidValueException("This PAR has already been posted and can no longer be edited.");
                            }

                            icsPar.LocationId = model.LocationId;
                            icsPar.LocationCode = locCode;
                            icsPar.Location = model.Location;
                            icsPar.RefDate = model.Date;
                            icsPar.ReceivedById = model.IcsPar != null ? model.IcsPar.ReceivedById : null;
                            icsPar.ReceivedBy = (model.IcsPar != null && model.IcsPar.ReceivedBy != null) ? model.IcsPar.ReceivedBy.Trim() : null;
                            icsPar.ReceivedByTitle = (model.IcsPar != null && model.IcsPar.ReceivedByTitle != null) ? model.IcsPar.ReceivedByTitle.Trim() : null;
                            icsPar.ReceivedByTitle2 = (model.IcsPar != null && model.IcsPar.ReceivedByTitle2 != null) ? model.IcsPar.ReceivedByTitle2.Trim() : null;
                            icsPar.ReceivedByPosition = (model.IcsPar != null && model.IcsPar.ReceivedByPosition != null) ? model.IcsPar.ReceivedByPosition.Trim() : null;
                            icsPar.ReceivedDate = model.IcsPar != null ? model.IcsPar.ReceivedDate : null;
                            icsPar.ReceivedDept = (model.IcsPar != null && model.IcsPar.ReceivedDept != null) ? model.IcsPar.ReceivedDept.Trim() : null;
                            icsPar.IssuedBy = (model.IcsPar != null && model.IcsPar.IssuedBy != null) ? model.IcsPar.IssuedBy.Trim() : null;
                            icsPar.IssuedByPosition = (model.IcsPar != null && model.IcsPar.IssuedByPosition != null) ? model.IcsPar.IssuedByPosition.Trim() : null;
                            icsPar.IssuedDate = model.IcsPar != null ? model.IcsPar.IssuedDate : null;
                            icsPar.IssuedDept = (model.IcsPar != null && model.IcsPar.IssuedDept != null) ? model.IcsPar.IssuedDept.Trim() : null;
                            icsPar.UpdatedBy = user;
                            icsPar.UpdatedDt = date;

                            icsParItem.IssuedTo = assignedTo;
                            icsParItem.Designation = assignedDesig;
                            icsParItem.UpdatedBy = user;
                            icsParItem.UpdatedDt = date;

                            psCardItemExtn.LocationId = model.LocationId;
                            psCardItemExtn.UpdatedBy = user;
                            psCardItemExtn.UpdatedDt = date;

                            var bundleComps = bundle.Components ?? new List<ParBundleComponentAllocationVM>();
                            var submittedSerialized = bundleComps
                                .Where(c => c.PsCardItemExtnId.HasValue && c.PsCardItemExtnId.Value != Guid.Empty)
                                .ToDictionary(c => c.PsCardItemExtnId.Value);
                            var submittedBulk = bundleComps
                                .Where(c => !c.PsCardItemExtnId.HasValue || c.PsCardItemExtnId.Value == Guid.Empty)
                                .ToDictionary(c => c.PsCardSubItemId);

                            foreach (var existingComponent in existingDraftParItem.IcsParItemComponents.ToList())
                            {
                                ParBundleComponentAllocationVM submittedComponent = null;
                                if (existingComponent.PsCardItemExtnId.HasValue)
                                {
                                    submittedSerialized.TryGetValue(existingComponent.PsCardItemExtnId.Value, out submittedComponent);
                                    if (submittedComponent != null)
                                    {
                                        submittedSerialized.Remove(existingComponent.PsCardItemExtnId.Value);
                                    }
                                }
                                else
                                {
                                    submittedBulk.TryGetValue(existingComponent.PsCardSubItemId, out submittedComponent);
                                    if (submittedComponent != null)
                                    {
                                        submittedBulk.Remove(existingComponent.PsCardSubItemId);
                                    }
                                }

                                if (submittedComponent == null)
                                {
                                    _db.IcsParItemComponents.Remove(existingComponent);
                                    continue;
                                }

                                existingComponent.PsCardSubItemId = submittedComponent.PsCardSubItemId;
                                existingComponent.PsCardItemExtnId = submittedComponent.PsCardItemExtnId;
                                existingComponent.Qty = submittedComponent.Qty;
                                existingComponent.UpdatedBy = user;
                                existingComponent.UpdatedDt = date;
                            }

                            foreach (var comp in submittedSerialized.Values.Concat(submittedBulk.Values))
                            {
                                _db.IcsParItemComponents.Add(new IcsParItemComponent
                                {
                                    Id = Guid.NewGuid(),
                                    IcsParItemId = icsParItem.Id,
                                    PsCardSubItemId = comp.PsCardSubItemId,
                                    PsCardItemExtnId = comp.PsCardItemExtnId,
                                    Qty = comp.Qty,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                });
                            }
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            var refNo = await NextRefNoAsync(model.Date, model.RefType);

                            icsPar = new IcsPar()
                            {
                                Id = Guid.NewGuid(),
                                UpdateCode = "N",
                                LocationId = model.LocationId,
                                LocationCode = locCode,
                                Location = model.Location,
                                RefNo = refNo,
                                RefDate = model.Date,
                                RefType = model.RefType,
                                ReceivedById = model.IcsPar != null ? model.IcsPar.ReceivedById : null,
                                ReceivedBy = (model.IcsPar != null && model.IcsPar.ReceivedBy != null) ? model.IcsPar.ReceivedBy.Trim() : null,
                                ReceivedByTitle = (model.IcsPar != null && model.IcsPar.ReceivedByTitle != null) ? model.IcsPar.ReceivedByTitle.Trim() : null,
                                ReceivedByTitle2 = (model.IcsPar != null && model.IcsPar.ReceivedByTitle2 != null) ? model.IcsPar.ReceivedByTitle2.Trim() : null,
                                ReceivedByPosition = (model.IcsPar != null && model.IcsPar.ReceivedByPosition != null) ? model.IcsPar.ReceivedByPosition.Trim() : null,
                                ReceivedDate = model.IcsPar != null ? model.IcsPar.ReceivedDate : null,
                                ReceivedDept = (model.IcsPar != null && model.IcsPar.ReceivedDept != null) ? model.IcsPar.ReceivedDept.Trim() : null,
                                IssuedBy = (model.IcsPar != null && model.IcsPar.IssuedBy != null) ? model.IcsPar.IssuedBy.Trim() : null,
                                IssuedByPosition = (model.IcsPar != null && model.IcsPar.IssuedByPosition != null) ? model.IcsPar.IssuedByPosition.Trim() : null,
                                IssuedDate = model.IcsPar != null ? model.IcsPar.IssuedDate : null,
                                IssuedDept = (model.IcsPar != null && model.IcsPar.IssuedDept != null) ? model.IcsPar.IssuedDept.Trim() : null,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            icsParItem = new IcsParItem()
                            {
                                Id = Guid.NewGuid(),
                                IcsParId = icsPar.Id,
                                PsCardItemExtnId = psCardItemExtn.Id,
                                Qty = 1,
                                AddCost = psCardItemExtn.AddCost,
                                Amount = psCardItemExtn.AcqCost,
                                IssuedTo = assignedTo,
                                Designation = assignedDesig,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };
                            icsPar.IcsParItems.Add(icsParItem);
                            _db.IcsPars.Add(icsPar);
                            _db.Entry(icsPar).State = EntityState.Added;
                            await _db.SaveChangesAsync();

                            var bundleComps = bundle.Components ?? new List<ParBundleComponentAllocationVM>();
                            foreach (var comp in bundleComps)
                            {
                                if (comp.Qty <= 0) continue;

                                var itemComponent = new IcsParItemComponent()
                                {
                                    Id = Guid.NewGuid(),
                                    IcsParItemId = icsParItem.Id,
                                    PsCardSubItemId = comp.PsCardSubItemId,
                                    PsCardItemExtnId = (comp.PsCardItemExtnId.HasValue && comp.PsCardItemExtnId.Value != Guid.Empty) ? comp.PsCardItemExtnId : null,
                                    Qty = comp.Qty,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                _db.IcsParItemComponents.Add(itemComponent);
                                _db.Entry(itemComponent).State = EntityState.Added;
                            }
                            await _db.SaveChangesAsync();
                        }
                        psCardItemExtn.LocationId = model.LocationId;
                        if (existingDraftParItem == null)
                        {
                            var propNo = NextPropNo(acqYear, cardItem.StockNo, locCode, model.RefType);
                            var propSplit = propNo.Split('/');
                            psCardItemExtn.PropYear = acqYear;
                            psCardItemExtn.PropNo = propNo;
                            psCardItemExtn.PropSeq = propSplit[propSplit.Length - 2];
                        }
                        psCardItemExtn.UpdatedBy = user;
                        psCardItemExtn.UpdatedDt = date;

                        await _db.SaveChangesAsync();
                        await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "PAR", user, date);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }

            return model;
        });
        }

        public ValueTask<GenerateIcsParVM> GenerateParSet(GenerateIcsParVM model, string user, DateTime date)
        {
            return _generateParExceptionService.TryCatch(async () =>
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

                if (cardItem.IsForICS == true)
                {
                    throw new InvalidValueException("Item is marked for ICS, cannot generate PAR!");
                }

                if (cardItem.ParBalance == 0)
                {
                    throw new InvalidValueException(string.Format("All Items have PARs."));
                }

                if (model.Qty > cardItem.ParBalance)
                {
                    throw new InvalidValueException(string.Format("Cannot generate more than the available balance."));
                }

            var existingItem = await _db.IcsParItems.Include(it => it.IcsPar).AsNoTracking().Where(w => w.PsCardItemExtn.PsCardItemId == psCardItemExtn.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync();
            var refType = existingItem != null && existingItem.IcsPar != null ? existingItem.IcsPar.RefType : null;
                if (!string.IsNullOrWhiteSpace(refType))
                {
                    throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
                }
            }


            var selectedItemGroups = selectedItems.GroupBy(g => new { g.SetLotNo, g.SetLotQtyNo })
                .Select(s => new
                {
                    SetLotNo = s.Key.SetLotNo,
                    SetLotQtyNo = s.Key.SetLotQtyNo
                }).ToList();
            foreach (var selectedItemGroup in selectedItemGroups)
            {
                var refNo = await NextRefNoAsync(model.Date, model.RefType);
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
                    ReceivedByTitle = model.IcsPar.ReceivedByTitle != null ? model.IcsPar.ReceivedByTitle.Trim() : null,
                    ReceivedByTitle2 = model.IcsPar.ReceivedByTitle2 != null ? model.IcsPar.ReceivedByTitle2.Trim() : null,
                    ReceivedByPosition = model.IcsPar.ReceivedByPosition != null ? model.IcsPar.ReceivedByPosition.Trim() : null,
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

                _db.IcsPars.Add(icsPar);
                _db.Entry(icsPar).State = EntityState.Added;
                await _db.SaveChangesAsync();

                var selectedSetItems = selectedItems.Where(w => w.SetLotNo == selectedItemGroup.SetLotNo && w.SetLotQtyNo == selectedItemGroup.SetLotQtyNo).ToList();
                foreach (var selectedSetItem in selectedSetItems)
                {
                    var psCardItemExtnId = selectedSetItem.Id;
                    var psCardItemExtn = await _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId).FirstOrDefaultAsync();
                    var cardItem = await GetByIdAsync(psCardItemExtn.PsCardItemId);
                    var addCost = psCardItemExtn.AddCost;
                    var amount = psCardItemExtn.AcqCost;
                    string acqYear = "";

                    if (cardItem.AcqDate != null)
                    {
                        acqYear = cardItem.AcqDate.Value.Year.ToString();
                    }
                    else if (cardItem.AirDate != null)
                    {
                        acqYear = cardItem.AirDate.Value.Year.ToString();
                    }
                    else
                    {
                        acqYear = cardItem.PoDate.Value.Year.ToString();
                    }

                    if (string.IsNullOrEmpty(acqYear))
                    {
                        throw new InvalidValueException("Acquisition Date is Required!");
                    }

                    IcsParItem icsParItem = new IcsParItem()
                    {
                        Id = Guid.NewGuid(),
                        IcsParId = icsPar.Id,
                        PsCardItemExtnId = selectedSetItem.Id,
                        Qty = 1,
                        AddCost = addCost,
                        Amount = amount,
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

                    await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "PAR", user, date);
                }

                // insert unit groups after creating the par/ics

                var psCardItemId = selectedItems.FirstOrDefault().PsCardItemId;
                var poNo = selectedItems.FirstOrDefault().PoNo;
                //var unitGroups = await _db.PsCardItemUnitGroups.AsNoTracking()
                //    .Include(i => i.PsCardItemUnitGroupDescriptions)
                //    .Where(w => w.PoNo == poNo)
                //    .ToListAsync();

                //foreach (var unitGroup in unitGroups)
                //{
                //    for (int q = 1; q <= unitGroup.Qty; q++)
                //    {
                //        var selectedItemList = selectedSetItems.Where(w => w.SetLotNo == unitGroup.SetLotNo && w.SetLotQtyNo == q).ToList();
                //        var selectedCount = selectedItemList.Count();
                //        var groupItemCount = _db.PsCardItemUnitGroupDescriptionItems
                //            .Where(w => w.PsCardItemUnitGroupDescription.UnitGroupId == unitGroup.Id)
                //            .Sum(s => s.PoQty) ?? 0;

                //        if (selectedCount == groupItemCount) // all items in the set were selected
                //        {
                //            // create set record
                //            int qty = 1;
                //            decimal? addCost = 0;
                //            var icsParUnitGroup = new IcsParUnitGroup()
                //            {
                //                Id = Guid.NewGuid(),
                //                IcsParId = icsPar.Id,
                //                SetLotNo = unitGroup.SetLotNo,
                //                Qty = qty,
                //                Unit = unitGroup.Unit,
                //                UnitCost = unitGroup.UnitCost,
                //                TotalCost = unitGroup.UnitCost * qty,
                //                InsertedBy = user,
                //                InsertedDt = date,
                //                Updatedby = user,
                //                UpdatedDt = date
                //            };

                //            foreach (var unitGroupDescription in unitGroup.PsCardItemUnitGroupDescriptions)
                //            {
                //                var icsParUnitGroupDescription = new IcsPartUnitGroupDescription()
                //                {
                //                    Id = Guid.NewGuid(),
                //                    UnitGroupId = icsParUnitGroup.Id,
                //                    Description = unitGroupDescription.Description,
                //                    InsertedBy = user,
                //                    InsertedDt = date,
                //                    UpdatedBy = user,
                //                    UpdatedDt = date
                //                };

                //                var unitGroupDescriptionItems = _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.UnitGroupDescriptionId == unitGroupDescription.Id).ToList();
                //                foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems)
                //                {
                //                    var unitGroupDescriptionItemSelections = selectedItemList.Where(w => w.PsCardItemId == unitGroupDescriptionItem.PsCardItemId);
                //                    foreach (var selectedItem in unitGroupDescriptionItemSelections) // selecteditemList from GenerateParSet Selection
                //                    {
                //                        var icsParItem = await _db.IcsParItems.Where(w => w.PsCardItemExtnId == selectedItem.Id && w.IcsPar.RefNo == refNo).FirstOrDefaultAsync();
                //                        var icsParItemId = icsParItem.Id;
                //                        var icsParUnitGroupDescriptionItems = new IcsParUnitGroupDescriptionItem()
                //                        {
                //                            Id = Guid.NewGuid(),
                //                            UnitGroupDescriptionId = icsParUnitGroupDescription.Id,
                //                            IcsParItemId = icsParItemId,
                //                            InsertedBy = user,
                //                            InsertedDt = date,
                //                            UpdatedBy = user,
                //                            UpdatedDt = date
                //                        };
                //                        icsParUnitGroupDescription.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescriptionItems);
                //                        addCost += (icsParItem.AddCost ?? 0);
                //                    }
                //                }
                //                icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDescription);
                //            }

                //            icsParUnitGroup.AddCost = addCost;
                //            icsParUnitGroup.GTotalCost = icsParUnitGroup.TotalCost + addCost;
                //            _db.IcsParUnitGroups.Add(icsParUnitGroup);
                //            _db.Entry(icsParUnitGroup).State = EntityState.Added;
                //            await _db.SaveChangesAsync();
                //        }
                //    }
                //}
            }

            return model;
        });
        }

        public ValueTask<IcsPar> PostAsync(string parNo, string user, DateTime date)
        {
            return _icsParExceptionService.TryCatch(() => _icsParSharedService.PostAsync(parNo, "P", user, date));
        }

        public ValueTask<IcsPar> UnPostAsync(string parNo, string user, DateTime date)
        {
            return _icsParExceptionService.TryCatch(() => _icsParSharedService.UnPostAsync(parNo, "P", user, date));
        }


        private string NextPropNo(string acqYear, string stockNo, string locationCode, string refType)
        {
            var propNo = _db.Database.SqlQuery<string>("Exec PoIssuance_GetNextSeqNo {0}, {1}, {2}, {3}", acqYear, stockNo, locationCode, refType).ToList();
            return propNo.LastOrDefault();
        }

        public async ValueTask<string> NextRefNoAsync(DateTime parDate, string refType)
        {
            string yyyy = parDate.Year.ToString().Trim();
            string mm = parDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            //var data = await _db.RisIssueds.Where(w => w.RefType == refType && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            var data = await _db.IcsPars.Where(w => w.RefType == refType && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            if (data == null)
            {
                return keyName + "-" + "00001";
            }
            else
            {
                var sequence = (int.Parse(data.RefNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(5, '0');
            }
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

        private async Task ValidateUploadAsync(Guid? groupId, string parNo)
        {
            if (!await IsWwithUploadAsync(groupId))
            {
                throw new InvalidValueException(string.Format("No uploaded files found PAR No. {0}, cannot post!", parNo));
            }
        }

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
                var msg = string.Format("Record already posted by {0} on {1}, cannot update!", entity.PostedBy, entity.PostedDt);
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(PsCardItem entity)
        {
            if (entity.PostedDt == null)
            {
                throw new RecordNotYetPostedException("Record is not yet posted!");
            }
        }
    }
}

