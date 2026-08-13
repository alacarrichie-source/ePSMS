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

        IQueryable<ParIcsPOGroupVM> GetAllPo(int? forYear);
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo();
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text);

        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);

        ValueTask<ParVM> GetByIdAsync(Guid? id);

        ValueTask<GenerateIcsParVM> GeneratePAR(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateParSet(GenerateIcsParVM model, string user, DateTime date);

        ValueTask<IcsPar> PostAsync(string refNo, string user, DateTime date);
        ValueTask<IcsPar> UnPostAsync(string refNo, string user, DateTime date);

        ValueTask<string> NextRefNoAsync(DateTime parDate, string refType);

        IParItemService ParItem { get; }
    }

    internal class ParService : BaseValidator, IParService
    {
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

        public IParItemService ParItem => _parItemService;

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        public IQueryable<ParVM> GetAll()
        {
            var priceCap = GetPriceCap();
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null
                    && (w.OrderItemRequest.OrderItem.OrderItemUnitGroupDescriptionItems
                        .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= priceCap)
                            || w.UnitCost >= priceCap))
                .Select(s => new ParVM
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
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
                    OrderItemUnitGroupDescriptionItem = s.OrderItemRequest.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemRequest.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            var priceCap = GetPriceCap();
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'P', {0}", priceCap).AsQueryable();
            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo(int? forYear)
        {
            var priceCap = GetPriceCap();
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'P', {0}, {1}", priceCap, forYear).AsQueryable();
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
                    && !w.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == w.Id)
                )
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
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

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo)
        {
            return GetItemSetsByPoNo(poNo, null, null);
        }

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId)
        {
            var priceCap = GetPriceCap();
            var data = _db.PsCardItemUnitGroups.AsNoTracking()
                .Where(w => w.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroupDescriptionItems
                    .Any(b => b.PsCardItem.PoNo == (string.IsNullOrEmpty(poNo) ? b.PsCardItem.PoNo : poNo)
                        && b.PsCardItem.PoDate == (poDate == null ? b.PsCardItem.PoDate : poDate)
                        && b.PsCardItem.DeptId == (deptId == null ? b.PsCardItem.DeptId : deptId)
                        ))
                && w.UnitCost >= priceCap)
                .Select(s => new ParIcsItemSetVm
                {
                    Id = s.Id,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    AddCost = _db.PsCardItemExtns.Where(w => s.PsCardItemUnitGroupDescriptions
                        .Any(a => a.PsCardItemUnitGroupDescriptionItems.Any(b => b.PsCardItemId == w.PsCardItemId))).Sum(x => x.AddCost) ?? 0,
                    GTotalCost = _db.PsCardItemExtns.Where(w => s.PsCardItemUnitGroupDescriptions
                        .Any(a => a.PsCardItemUnitGroupDescriptionItems.Any(b => b.PsCardItemId == w.PsCardItemId))).Sum(x => x.AcqCost) ?? 0,
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
                .Where(w => w.PsCardItemUnitGroupDescriptionItems.Any(a => a.UnitGroupDescriptionId == unitGroupDescriptionId && a.PsCardItemId == w.Id))
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
                    //AddCost = s.AddCost,
                    //GTotalCost = s.GTotalCost,
                    AddCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AddCost) ?? 0,
                    GTotalCost = _db.PsCardItemExtns.Where(w => w.PsCardItemId == s.Id).Sum(x => x.AcqCost) ?? 0,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = (_db.IcsParItems.Where(w => !w.IcsPar.IcsParUpdates.Any() && w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    ParPostedBy = s.ParPostedBy,
                    ParPostedDt = s.ParPostedDt
                }).AsQueryable();
            return data;
        }

        public async ValueTask<ParVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems.Where(w => w.Id == id)
                .Select(s => new ParVM
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
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
                    OrderItemUnitGroupDescriptionItem = s.OrderItemRequest.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemRequest.OrderItemId),
                    IsForICS = s.IsForICS,
                    AcqDate = s.AcqDate
                }).FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<GenerateIcsParVM> GeneratePAR(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatch(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            var cardItem = await GetByIdAsync(model.PsCardItemId);

            if (cardItem == null)
            {
                throw new RecordNotFoundException((Guid)model.PsCardItemId);
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

            if (model.SelectedIds == null)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }

            var selectedIds = model.SelectedIds.Split(',');
            if (selectedIds.Count() == 0)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }

            var refType = (await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking().Where(w => w.PsCardItemExtn.PsCardItemId == model.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync())?.IcsPar.RefType;
            if (!string.IsNullOrWhiteSpace(refType))
            {
                throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
            }

            Guid? icsParId = null;
            IcsPar icsPar = null;
            bool icsSw = true;
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

            // generate per itemextn
            foreach (var selectedId in selectedIds)
            {
                var psCardItemExtnId = Guid.Parse(selectedId);
                var psCardItemExtn = await _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId).FirstOrDefaultAsync();
                var refNo = await NextRefNoAsync(model.Date, model.RefType);
                icsPar = new IcsPar()
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

                IcsParItem icsParItem = new IcsParItem()
                {
                    Id = Guid.NewGuid(),
                    IcsParId = icsPar.Id,
                    PsCardItemExtnId = psCardItemExtnId,
                    Qty = 1,
                    AddCost = psCardItemExtn.AddCost,
                    Amount = psCardItemExtn.AcqCost,
                    IssuedTo = model.IssuedTo,
                    Designation = model.Designation,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                icsPar.IcsParItems.Add(icsParItem);
                _db.IcsPars.Add(icsPar);
                _db.Entry(icsPar).State = EntityState.Added;
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

                await _db.SaveChangesAsync();
                await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "PAR", user, date);
            }

            return model;
        });

        public ValueTask<GenerateIcsParVM> GenerateParSet(GenerateIcsParVM model, string user, DateTime date) =>
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

                var refType = (await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking()
                    .Where(w => w.PsCardItemExtn.PsCardItemId == psCardItemExtn.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync())?.IcsPar.RefType;
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
                var unitGroups = await _db.PsCardItemUnitGroups.AsNoTracking()
                    .Include(i => i.PsCardItemUnitGroupDescriptions)
                    .Where(w => w.PoNo == poNo)
                    .ToListAsync();

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
                            decimal? addCost = 0;
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
                                        var icsParItem = await _db.IcsParItems.Where(w => w.PsCardItemExtnId == selectedItem.Id && w.IcsPar.RefNo == refNo).FirstOrDefaultAsync();
                                        var icsParItemId = icsParItem.Id;
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
                                        addCost += (icsParItem.AddCost ?? 0);
                                    }
                                }
                                icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDescription);
                            }

                            icsParUnitGroup.AddCost = addCost;
                            icsParUnitGroup.GTotalCost = icsParUnitGroup.TotalCost + addCost;
                            _db.IcsParUnitGroups.Add(icsParUnitGroup);
                            _db.Entry(icsParUnitGroup).State = EntityState.Added;
                            await _db.SaveChangesAsync();
                        }
                    }
                }
            }

            return model;
        });

        public ValueTask<IcsPar> PostAsync(string parNo, string user, DateTime date) => _icsParExceptionService.TryCatch(async () =>
        {
            return await _icsParSharedService.PostAsync(parNo, "P", user, date);
        });

        public ValueTask<IcsPar> UnPostAsync(string parNo, string user, DateTime date) => _icsParExceptionService.TryCatch(async () =>
        {
            return await _icsParSharedService.UnPostAsync(parNo, "P", user, date);
        });

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
                throw new InvalidValueException($"No uploaded files found PAR No. {parNo}, cannot post!");
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