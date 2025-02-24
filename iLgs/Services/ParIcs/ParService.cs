using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
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
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo();
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text);

        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);

        //ValueTask<PsCardItem> PostAsync(Guid? groupId, string user, DateTime date);
        //ValueTask<PsCardItem> UnPostAsync(Guid? groupId, string user, DateTime date);        

        //ValueTask<PsCardItem> PostSetAsync(Guid? unitGroupId, string user, DateTime date);
        //ValueTask<PsCardItem> UnPostSetAsync(Guid? unitGroupId, string user, DateTime date);

        ValueTask<ParVM> GetByIdAsync(Guid? id);

        ValueTask<GenerateIcsParVM> GeneratePAR(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateParSet(GenerateIcsParVM model, string user, DateTime date);

        ValueTask<IcsPar> PostAsync(string refNo, string user, DateTime date);
        ValueTask<IcsPar> UnPostAsync(string refNo, string user, DateTime date);

        IIcsParService IcsPar { get; }
        IIcsParItemService IcsParItem { get; }
        IPsCardService PsCard { get; }
        IPsCardItemService PsCardItem { get; }
        IPsCardItemExtnService PsCardItemExtn { get; }
        IPsCardItemIssuanceService PsCardItemIssaunce { get; }
    }

    public class ParService : BaseValidator, IParService
    {
        private readonly AppManEntities _db;
        private decimal _parPrice = 50000;

        private IIcsParService _icsParService;
        private IIcsParItemService _icsParItemService;
        private IPsCardService _psCardService;
        private IPsCardItemService _psCardItemService;
        private IPsCardItemExtnService _psCardItemExtnService;
        private IPsCardItemIssuanceService _psCardItemIssaunceService;
        private readonly IExceptionService<GenerateIcsParVM> _generateParExceptionService = new ExceptionService<GenerateIcsParVM>();
        private readonly IExceptionService<PsCardItem> _postExceptionService = new ExceptionService<PsCardItem>();
        private readonly IExceptionService<IcsPar> _icsParExceptionService = new ExceptionService<IcsPar>();
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;

        public ParService(AppManEntities db)
        {
            _db = db;
            _icsParService = new IcsParService(_db);
            _icsParItemService = new IcsParItemService(_db);
            _psCardService = new PsCardService(_db);
            _psCardItemService = new PsCardItemService(_db);
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _psCardItemIssaunceService = new PsCardItemIssuanceService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
        }

        public IIcsParService IcsPar { get { return _icsParService = _icsParService ?? new IcsParService(_db); } }
        public IIcsParItemService IcsParItem { get { return _icsParItemService = _icsParItemService ?? new IcsParItemService(_db); } }
        public IPsCardService PsCard { get { return _psCardService = _psCardService ?? new PsCardService(_db); } }
        public IPsCardItemService PsCardItem { get { return _psCardItemService = _psCardItemService ?? new PsCardItemService(_db); } }
        public IPsCardItemExtnService PsCardItemExtn { get { return _psCardItemExtnService = _psCardItemExtnService ?? new PsCardItemExtnService(_db); } }
        public IPsCardItemIssuanceService PsCardItemIssaunce { get { return _psCardItemIssaunceService = _psCardItemIssaunceService ?? new PsCardItemIssuanceService(_db); } }

        public IQueryable<ParVM> GetAll()
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null
                    && (w.OrderItem.OrderItemUnitGroupDescriptionItems
                        .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                            || w.UnitCost >= _parPrice))
                .Select(s => new ParVM
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    //Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    AddCost = s.AddCost,
                    GTotalCost = s.GTotalCost,
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
                    ParBalance = s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo").AsQueryable();
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
        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _db.PsCardItems.Include(i => i.PsCard.ItemCode)
                .AsNoTracking()
                .Where(w => w.TransferRefId == null
                    && w.PoNo == (string.IsNullOrEmpty(poNo) ? w.PoNo : poNo)
                    && w.PoDate == (poDate == null ? w.PoDate : poDate)
                    && w.DeptId == (deptId == null ? w.DeptId : deptId)
                    && w.UnitCost >= _parPrice 
                    && !w.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == w.Id)
                    //&& w.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == w.Id) // not in set
                    //&& !w.OrderItem.OrderItemUnitGroupDescriptionItems
                    //    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                //&& (w.UnitCost >= _parPrice
                //    || w.OrderItem.OrderItemUnitGroupDescriptionItems
                //        .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                //)
                )
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    GroupId = s.GroupId,
                    PsCardId = s.PsCardId,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    AddCost = s.AddCost,
                    GTotalCost = s.GTotalCost,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    IsForICS = s.IsForICS,
                    GeneratedItems = (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution,
                    ParPostedBy = s.ParPostedBy,
                    ParPostedDt = s.ParPostedDt
                    //SetLotNo = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == s.OrderItemId))).FirstOrDefault().SetLotNo ?? "",
                    //SetLotDesc = _db.OrderItemUnitGroupDescriptions.Where(w => w.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == s.OrderItemId)).FirstOrDefault().Description ?? ""
                }).AsQueryable();
            return data;
        }

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo)
        {
            return GetItemSetsByPoNo(poNo, null, null);
        }

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _db.PsCardItemUnitGroups.AsNoTracking()
                .Where(w => w.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroupDescriptionItems
                    .Any(b => b.PsCardItem.TransferRefId == null
                        && b.PsCardItem.PoNo == (string.IsNullOrEmpty(poNo) ? b.PsCardItem.PoNo : poNo)
                        && b.PsCardItem.PoDate == (poDate == null ? b.PsCardItem.PoDate : poDate)
                        && b.PsCardItem.DeptId == (deptId == null ? b.PsCardItem.DeptId : deptId)
                        ))
                && w.UnitCost >= _parPrice)
                .Select(s => new ParIcsItemSetVm
                {
                    Id = s.Id,
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
                    AddCost = s.AddCost,
                    GTotalCost = s.GTotalCost,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
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
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    PriceRate = s.PriceRate,
                    AddCost = s.AddCost,
                    GTotalCost = s.GTotalCost,
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
                    ParBalance = s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId),
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
            //var psCardItemExtnList = await _db.PsCardItemExtns.Where(w => w.PsCardItemId == model.PsCardItemId && w.IcsParItems.Count() == 0).ToListAsync();                        
            //foreach(var psCardItemExtn in psCardItemExtnList)
            foreach (var selectedId in selectedIds)
            {
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
                    PsCardItemExtnId = Guid.Parse(selectedId),
                    Qty = 1,
                    Amount = cardItem.UnitCost,
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
                var psCardItemExtnId = Guid.Parse(selectedId);

                var psCardItemExtn = _db.PsCardItemExtns.Where(w => w.Id == psCardItemExtnId).FirstOrDefault();
                psCardItemExtn.LocationId = model.LocationId;
                psCardItemExtn.PropYear = acqYear;
                psCardItemExtn.PropNo = propNo;
                psCardItemExtn.PropSeq = propSeq;
                psCardItemExtn.UpdatedBy = user;
                psCardItemExtn.UpdatedDt = date;

                _db.PsCardItemExtns.Attach(psCardItemExtn);
                _db.Entry(psCardItemExtn).State = EntityState.Modified;
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

            //var selectedIds = model.SelectedIds.Split(',');
            //if (selectedIds.Count() == 0)
            //{
            //    throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            //}

            // Deserialize the JSON string to a List of your model
            var selectedItems = JsonConvert.DeserializeObject<List<PsCardItemExtnSetVM>>(model.SelectedIds);
            if (selectedItems.Count() == 0)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }

            // check if will process entire set
            //var unitGroupDescriptionItems = _db.PsCardItemUnitGroupDescriptionItems.Where(w => w.PsCardItemUnitGroupDescription.UnitGroupId == model.UnitGroupId).ToList();
            //var unitGroupDescriptions = _db.PsCardItemUnitGroupDescriptions
            //    .Include(i => i.PsCardItemUnitGroup)
            //    .Include(i => i.PsCardItemUnitGroupDescriptionItems).Where(w => w.UnitGroupId == model.UnitGroupId).ToList();
            //foreach(var unitGroupDescription in unitGroupDescriptions)
            //{
            //    foreach(var unitGroupDescriptionItem in unitGroupDescription.PsCardItemUnitGroupDescriptionItems)
            //    {
            //        selectedItems.Where(w => w.Id == unitGroupDescriptionItem.Id).Count();
            //    }
            //}            

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
                        Amount = cardItem.UnitCost,
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

                    _db.PsCardItemExtns.Attach(psCardItemExtn);
                    _db.Entry(psCardItemExtn).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    await _psCardItemTransactionService.LogUpdates(psCardItemExtn.Id, icsPar.Id, "PAR", user, date);
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
                                        var icsParItemId = _db.IcsParItems.Where(w => w.PsCardItemExtnId == selectedItem.Id && w.IcsPar.RefNo == refNo).FirstOrDefault().Id;
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
                            _db.SaveChanges();
                        }
                    }
                }
            }

            return model;
        });

        public ValueTask<IcsPar> PostAsync(string parNo, string user, DateTime date) => _icsParExceptionService.TryCatch(async () =>
        {
            return await _icsParService.PostAsync(parNo, "P", user, date);
        });

        public ValueTask<IcsPar> UnPostAsync(string parNo, string user, DateTime date) => _icsParExceptionService.TryCatch(async () =>
        {
            return await _icsParService.UnPostAsync(parNo, "P", user, date);
        });

        private string NextPropNo(string acqYear, string stockNo, string locationCode, string refType)
        {
            var propNo = _db.Database.SqlQuery<string>("Exec PoIssuance_GetNextSeqNo {0}, {1}, {2}, {3}", acqYear, stockNo, locationCode, refType).ToList();
            return propNo.LastOrDefault();
        }

        private async ValueTask<string> NextRefNoAsync(DateTime parDate, string refType)
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

        //public ValueTask<PsCardItem> PostAsync(Guid? groupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        //{
        //    var entity = _db.PsCardItems.Include(i => i.PsCard.ItemCode).Where(w => w.GroupId == groupId);
        //    if (entity.Count() == 0)
        //    {
        //        throw new NotFoundException((Guid)groupId);
        //    }

        //    var psCardItem = entity.FirstOrDefault(f => f.TransferRefId == null);
        //    if (psCardItem.IsForICS == true)
        //    {
        //        throw new InvalidValueException("Cannot post items for ICS.");
        //    }

        //    var partItems = _icsParItemService.GetAllParItems(groupId);
        //    if (partItems.Count() < psCardItem.Qty)
        //    {
        //        throw new InvalidValueException($"Insufficient PAR Item created for {psCardItem.PsCard.ItemCode.Description}.");
        //    }

        //    foreach (var parItem in partItems)
        //    {
        //        await ValidateUploadAsync(parItem.IcsParId, parItem.IcsPar.RefNo);
        //    }

        //    var icsPars = _db.IcsPars.Where(w => w.IcsParItems.Any(a => a.PsCardItemExtn.PsCardItem.GroupId == groupId));
        //    await PostPsCardItemAsync(entity, user, date);
        //    await PostIcsParAsync(icsPars, user, date);            
        //    await _db.SaveChangesAsync();

        //    return entity.FirstOrDefault();
        //});        

        //public ValueTask<PsCardItem> UnPostAsync(Guid? groupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        //{
        //    var entity = _db.PsCardItems.Where(w => w.GroupId == groupId);

        //    if (entity.Count() == 0)
        //    {
        //        throw new NotFoundException((Guid)groupId);
        //    }
        //    //await ValidateOnUnpost(entity);

        //    var icsPars = _db.IcsPars.Where(w => w.IcsParItems.Any(a => a.PsCardItemExtn.PsCardItem.GroupId == groupId));

        //    await UnPostPsCardItemAsync(entity, user, date);
        //    await UnPostIcsParAsync(icsPars, user, date);
        //    await _db.SaveChangesAsync();

        //    return entity.FirstOrDefault();
        //});

        //private async Task PostPsCardItemAsync(IQueryable<PsCardItem> psCardItems, string user, DateTime date)
        //{
        //    await psCardItems.ForEachAsync(f =>
        //    {
        //        f.ParPostedBy = user;
        //        f.ParPostedDt = date;
        //        //f.UpdatedBy = user;
        //        //f.UpdatedDt = date;
        //    });
        //}

        //private async Task PostIcsParAsync(IQueryable<IcsPar> icsPars, string user, DateTime date)
        //{
        //    await icsPars.ForEachAsync(f =>
        //    {
        //        f.PostedBy = user;
        //        f.PostedDt = date;
        //        //f.UpdatedBy = user;
        //        //f.UpdatedDt = date;
        //    });
        //}

        //private async Task UnPostPsCardItemAsync(IQueryable<PsCardItem> psCardItems, string user, DateTime date)
        //{
        //    await psCardItems.ForEachAsync(f =>
        //    {
        //        f.ParPostedBy = null;
        //        f.ParPostedDt = null;
        //        f.UpdatedBy = user;
        //        f.UpdatedDt = date;
        //    });                        
        //}

        //private async Task UnPostIcsParAsync(IQueryable<IcsPar> icsPars, string user, DateTime date)
        //{
        //    await icsPars.ForEachAsync(f =>
        //    {
        //        f.PostedBy = null;
        //        f.PostedDt = null;
        //        f.UpdatedBy = user;
        //        f.UpdatedDt = date;
        //    });            
        //}

        //public ValueTask<PsCardItem> PostSetAsync(Guid? unitGroupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        //{
        //    var unitGroupDescriptions = await _db.PsCardItemUnitGroupDescriptions
        //        .Include(i => i.PsCardItemUnitGroup)
        //        .Include(i => i.PsCardItemUnitGroupDescriptionItems).Where(w => w.UnitGroupId == unitGroupId).ToListAsync();
        //    if (!unitGroupDescriptions.Any())
        //    {
        //        throw new NotFoundException((Guid)unitGroupId);
        //    }

        //    // validate unit group items
        //    foreach (var unitGroupDescription in unitGroupDescriptions)
        //    {
        //        foreach (var unitGroupDescriptionItem in unitGroupDescription.PsCardItemUnitGroupDescriptionItems)
        //        {
        //            var entity = _db.PsCardItems.Include(i => i.PsCard.ItemCode).Where(w => w.GroupId == unitGroupDescriptionItem.PsCardItemId);
        //            if (entity.Count() == 0)
        //            {
        //                throw new NotFoundException((Guid)unitGroupDescriptionItem.PsCardItemId);
        //            }

        //            var psCardItem = entity.FirstOrDefault(f => f.TransferRefId == null);
        //            if (psCardItem.IsForICS == true)
        //            {
        //                throw new InvalidValueException("Cannot post items for ICS.");
        //            }

        //            var partItems = _icsParItemService.GetAllParItems(unitGroupDescriptionItem.PsCardItemId);
        //            if (partItems.Count() < psCardItem.Qty)
        //            {
        //                throw new InvalidValueException("Insufficient PAR Item created.");
        //            }

        //            foreach (var parItem in partItems)
        //            {
        //                await ValidateUploadAsync(parItem.IcsParId, parItem.IcsPar.RefNo);
        //            }                    
        //        }
        //    }

        //    foreach (var unitGroupDescription in unitGroupDescriptions)
        //    {
        //        foreach (var unitGroupDescriptionItem in unitGroupDescription.PsCardItemUnitGroupDescriptionItems)
        //        {
        //            var groupId = unitGroupDescriptionItem.PsCardItemId;
        //            var entity = _db.PsCardItems.Where(w => w.GroupId == groupId);
        //            var icsPars = _db.IcsPars.Where(w => w.IcsParItems.Any(a => a.PsCardItemExtn.PsCardItem.GroupId == groupId));
        //            await PostPsCardItemAsync(entity, user, date);
        //            await PostIcsParAsync(icsPars, user, date);
        //        }
        //    }

        //    var unitGroup = await _db.PsCardItemUnitGroups.FindAsync(unitGroupId);
        //    unitGroup.PostedBy = user;
        //    unitGroup.PostedDt = date;
        //    unitGroup.UpdatedBy = user;
        //    unitGroup.UpdatedDt = date;
        //    await _db.SaveChangesAsync();

        //    return new PsCardItem(); // just return null item
        //});

        //public ValueTask<PsCardItem> UnPostSetAsync(Guid? unitGroupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        //{
        //    var unitGroupDescriptions = await _db.PsCardItemUnitGroupDescriptions
        //        .Include(i => i.PsCardItemUnitGroup)
        //        .Include(i => i.PsCardItemUnitGroupDescriptionItems).Where(w => w.UnitGroupId == unitGroupId).ToListAsync();
        //    if (!unitGroupDescriptions.Any())
        //    {
        //        throw new NotFoundException((Guid)unitGroupId);
        //    }

        //    foreach (var unitGroupDescription in unitGroupDescriptions)
        //    {
        //        foreach (var unitGroupDescriptionItem in unitGroupDescription.PsCardItemUnitGroupDescriptionItems)
        //        {                    
        //            var groupId = unitGroupDescriptionItem.PsCardItemId;
        //            var entity = _db.PsCardItems.Where(w => w.GroupId == groupId);
        //            var icsPars = _db.IcsPars.Where(w => w.IcsParItems.Any(a => a.PsCardItemExtn.PsCardItem.GroupId == groupId));
        //            await UnPostPsCardItemAsync(entity, user, date);
        //            await UnPostIcsParAsync(icsPars, user, date);
        //        }
        //    }

        //    var unitGroup = await _db.PsCardItemUnitGroups.FindAsync(unitGroupId);
        //    unitGroup.PostedBy = null;
        //    unitGroup.PostedDt = null;
        //    unitGroup.UpdatedBy = user;
        //    unitGroup.UpdatedDt = date;
        //    await _db.SaveChangesAsync();

        //    return new PsCardItem(); // just return null item
        //});

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

        //private void ValidateFields(PsCardItem model)
        //{
        //    //if (string.IsNullOrWhiteSpace(model.PhaseNo))
        //    //{
        //    //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseNo)), "Field is required.");
        //    //}

        //    //if (!model.PhaseAmountCo.HasValue)
        //    //{
        //    //    _imex.UpsertDataList(_getDisplayName(nameof(model.PhaseAmountCo)), "Field is required.");
        //    //}

        //    //_imex.ThrowIfContainsErrors();
        //}
    }
}