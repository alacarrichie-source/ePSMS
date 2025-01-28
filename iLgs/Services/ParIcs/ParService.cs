using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.PropertyCard;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
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
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate,  Guid? deptId);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);

        ValueTask<PsCardItem> PostAsync(Guid? groupId, string user, DateTime date);
        ValueTask<PsCardItem> UnPostAsync(Guid? groupId, string user, DateTime date);

        ValueTask<ParVM> GetByIdAsync(Guid? id);

        //IQueryable<PARAcknowledgementVM> GetAcknowledgedOrderItems(Guid? orderItemId);
        //Task<PARAcknowledgementVM> GetAcknowledgedOrderItemByItemId(Guid? parItemId);
        //Task<Models.PAR> GetByIdAsync(Guid parId);
        //Task<Models.PAR> GetByParNoAsync(string parNo);
        //Task<int?> GetRemainingQty(Guid? orderItemId, Guid? parItemId);
        //Task<bool> IsAnyParNoAsync(Guid parId, string parNo);
        //Task<bool> IsPostedAsync(Guid parId);

        //Task<PAR_VM> CreateAsync(PAR_VM model, string user, DateTime date);
        //Task<PAR_VM> UpdateAsync(PAR_VM model, string user, DateTime date);
        //Task<PAR_VM> DeleteAsync(PAR_VM model, string user, DateTime date);

        //Task PostAsync(Guid parId, string user, DateTime date);
        //Task UnpostAsync(Guid parId, string user, DateTime date);
        //Task GeneratePAR(GenerateParVM model, string user, DateTime date);

        //Task<PARAcknowledgementVM> CreateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
        //Task<PARAcknowledgementVM> UpdateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
        //Task<PARAcknowledgementVM> DeleteAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GeneratePAR(GenerateIcsParVM model, string user, DateTime date);

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

        private IIcsParItemService _icsParItemService;
        private IPsCardService _psCardService;
        private IPsCardItemService _psCardItemService;
        private IPsCardItemExtnService _psCardItemExtnService;
        private IPsCardItemIssuanceService _psCardItemIssaunceService;
        private readonly IExceptionService<GenerateIcsParVM> _generateParExceptionService = new ExceptionService<GenerateIcsParVM>();
        private readonly IExceptionService<PsCardItem> _postExceptionService = new ExceptionService<PsCardItem>();
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;

        public ParService(AppManEntities db)
        {
            _db = db;
            _icsParItemService = new IcsParItemService(_db);
            _psCardService = new PsCardService(_db);
            _psCardItemService = new PsCardItemService(_db);
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _psCardItemIssaunceService = new PsCardItemIssuanceService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
        }

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
                    ParBalance = s.Qty -(_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            //var data = _db.PsCardItems.AsNoTracking()
            //    .Where(w => w.TransferRefId == null
            //        && (w.OrderItem.OrderItemUnitGroupDescriptionItems
            //            .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
            //                || w.UnitCost >= _parPrice))
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
            //    .AsQueryable();

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
                    //&& w.UnitCost >= _parPrice
                    //&& !w.OrderItem.OrderItemUnitGroupDescriptionItems
                    //    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                    && (w.UnitCost >= _parPrice
                        || w.OrderItem.OrderItemUnitGroupDescriptionItems
                            .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                    )
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
                    ParPostedDt = s.ParPostedDt,
                    SetLotNo = _db.OrderItemUnitGroups.Where(w => w.OrderItemUnitGroupDescriptions.Any(a => a.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == s.OrderItemId))).FirstOrDefault().SetLotNo ?? "",
                    SetLotDesc = _db.OrderItemUnitGroupDescriptions.Where(w => w.OrderItemUnitGroupDescriptionItems.Any(b => b.OrderItemId == s.OrderItemId)).FirstOrDefault().Description ?? ""
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
                    InsertedDt = s.InsertedDt,
                    UnitGroupDescriptions = s.PsCardItemUnitGroupDescriptions
                }).AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId)
        {
            var data = _db.PsCardItemUnitGroupDescriptions.AsNoTracking()
                .Where(w => w.UnitGroupId == unitGroupId).AsQueryable();                
            return data;
        }

        //public IQueryable<PsCardItemUnitGroupDescriptionItem> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId)
        //{
        //    var data = _db.PsCardItemUnitGroupDescriptionItems.Include(i => i.PsCardItem.PsCard.ItemCode.ItemType).AsNoTracking()
        //        .Where(w => w.UnitGroupDescriptionId == unitGroupDescriptionId).AsQueryable();
        //    return data;
        //}

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

            if (model.SelectedIds == null) {
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
                    LocationId = model.LocationId,
                    LocationCode = model.LocationCode,
                    Location = model.Location,
                    RefNo = refNo,
                    RefDate = model.Date,
                    RefType = model.RefType,
                    ReceivedBy = model.IcsPar.ReceivedBy,
                    ReceivedByPosition = model.IcsPar.ReceivedByPosition,
                    ReceivedDate = model.IcsPar.ReceivedDate,
                    ReceivedDept = model.IcsPar.ReceivedDept,
                    IssuedBy = model.IcsPar.IssuedBy,
                    IssuedByPosition = model.IcsPar.IssuedByPosition,
                    IssuedDate = model.IcsPar.IssuedDate,
                    IssuedDept = model.IcsPar.IssuedDept,
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

        private async Task ValidateUploadAsync(Guid? groupId)
        {
            if (!await IsWwithUploadAsync(groupId))
            {
                throw new InvalidValueException("No uploaded files found, cannot post!");
            }
        }

        public ValueTask<PsCardItem> PostAsync(Guid? groupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        {
            var entity = _db.PsCardItems.Where(w => w.GroupId == groupId);
            if (entity.Count() == 0)
            {
                throw new NotFoundException((Guid)groupId);
            }

            var psCardItem = entity.FirstOrDefault(f => f.TransferRefId == null);

            if (psCardItem.IsForICS != true)
            {
                var partItems = _icsParItemService.GetAllParItems(groupId);
                if (partItems.Count() < psCardItem.Qty)
                {
                    throw new InvalidValueException("Insufficient PAR Item created.");
                }
            }

            await ValidateUploadAsync(groupId);
            //return entity.FirstOrDefault();

            //await ValidateOnPost(entity);

            await entity.ForEachAsync(f =>
            {
                f.ParPostedBy = user;
                f.ParPostedDt = date;
                f.UpdatedBy = user;
                f.UpdatedDt = date;
            });
            await _db.SaveChangesAsync();
            return entity.FirstOrDefault();
        });

        public ValueTask<PsCardItem> UnPostAsync(Guid? groupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        {
            var entity = _db.PsCardItems.Where(w => w.GroupId == groupId);

            if (entity.Count() == 0)
            {
                throw new NotFoundException((Guid)groupId);
            }

            //await ValidateOnUnpost(entity);

            await entity.ForEachAsync(f =>
            {
                f.ParPostedBy = null;
                f.ParPostedDt = null;
                f.UpdatedBy = user;
                f.UpdatedDt = date;
            });
            await _db.SaveChangesAsync();
            return entity.FirstOrDefault();
        });

        private void ValidateRecord(PsCardItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        //private void ValidateIfPosted(PsCardItem entity)
        //{
        //    if (entity.PostedDt != null)
        //    {
        //        var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
        //        throw new RecordAlreadyPostedException(msg);
        //    }
        //}

        //private void ValidateIfNotPosted(PsCardItem entity)
        //{
        //    if (entity.PostedDt == null)
        //    {
        //        throw new RecordNotYetPostedException($"Record is not yet posted!");
        //    }
        //}

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