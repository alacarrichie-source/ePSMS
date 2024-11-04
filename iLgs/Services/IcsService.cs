using iLgs.Exceptions;
using iLgs.Exceptions.PARs;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IIcsService
    {
        IQueryable<IcsVM> GetAll();
        IQueryable<ParIcsPOGroupVM> GetAllPo();
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo();
        IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text);
        ValueTask<ParIcsItemVm> GetItemByIdAsync(Guid? psCardItemId);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo);
        IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo);
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo, DateTime? poDate, Guid? deptId);
        IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<PsCardItem> PostAsync(Guid? groupId, string user, DateTime date);
        ValueTask<PsCardItem> UnPostAsync(Guid? groupId, string user, DateTime date);
        ValueTask<IcsVM> GetByIdAsync(Guid? id);
        
        ValueTask<GenerateIcsParVM> GenerateIcs(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateIcsBatch(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<PsCardItemUnitGroupDescriptionItem> UpdateNoICSAsync(PsCardItemUnitGroupDescriptionItem model, string user, DateTime date);

        IIcsParItemService IcsParItem { get; }
        IPsCardService PsCard { get; }
        IPsCardItemService PsCardItem { get; }
        IPsCardItemExtnService PsCardItemExtn { get; }
        IPsCardItemIssuanceService PsCardItemIssaunce { get; }
    }

    public class IcsService : BaseValidator, IIcsService
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
        private readonly IExceptionService<PsCardItemUnitGroupDescriptionItem> _psCardItemUnitGroupDescriptionItemService = new ExceptionService<PsCardItemUnitGroupDescriptionItem>();
        private readonly GetDisplayNameDelegate _getDisplayName;

        public IcsService(AppManEntities db)
        {
            _db = db;
            _icsParItemService = new IcsParItemService(_db);
            _psCardService = new PsCardService(_db);
            _psCardItemService = new PsCardItemService(_db);
            _psCardItemExtnService = new PsCardItemExtnService(_db);
            _psCardItemIssaunceService = new PsCardItemIssuanceService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianReportBldgItemVM>(propertyName);
        }

        public IIcsParItemService IcsParItem { get { return _icsParItemService = _icsParItemService ?? new IcsParItemService(_db); } }
        public IPsCardService PsCard { get { return _psCardService = _psCardService ?? new PsCardService(_db); } }
        public IPsCardItemService PsCardItem { get { return _psCardItemService = _psCardItemService ?? new PsCardItemService(_db); } }
        public IPsCardItemExtnService PsCardItemExtn { get { return _psCardItemExtnService = _psCardItemExtnService ?? new PsCardItemExtnService(_db); } }
        public IPsCardItemIssuanceService PsCardItemIssaunce { get { return _psCardItemIssaunceService = _psCardItemIssaunceService ?? new PsCardItemIssuanceService(_db); } }

        public IQueryable<IcsVM> GetAll()
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null 
                    && !w.OrderItem.OrderItemUnitGroupDescriptionItems
                        .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                    && w.UnitCost < _parPrice)
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
                    //IcsBalance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) -
                    //    (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    IcsBalance = s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == s.Id && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            //var data = _db.PsCardItems.AsNoTracking()
            //    .Where(w => w.TransferRefId == null 
            //        &&  (w.OrderItem.OrderItemUnitGroupDescriptionItems
            //            .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
            //        || (w.UnitCost >= _parPrice && w.IsForICS == true)
            //        || w.UnitCost < _parPrice))
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
            //return data;

            var data = _db.Database.SqlQuery<ParIcsPOGroupVM>("Exec ParIcs_GetAllPo 'I'").AsQueryable();

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
                    Qty = s.Qty,
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
                    GeneratedItems = (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    InvDist = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo)
        {
            return GetItemsByPoNo(poNo, null, null);
        }

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo, DateTime? poDate, Guid? deptId)
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.TransferRefId == null                
                    && w.DeptId == (deptId == null ? w.DeptId : deptId)
                    && w.PoNo == (string.IsNullOrEmpty(poNo) ? w.PoNo : poNo)
                    && w.PoDate == (poDate == null ? w.PoDate : poDate)
                    && !w.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == w.Id)
                    && ((w.UnitCost >= _parPrice && w.IsForICS == true) || w.UnitCost < _parPrice)
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
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    //GeneratedItems = s.PsCardItemExtns.Count(c => c.IcsParItems.Any()),                    
                    GeneratedItems = (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    InvDist = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution
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
                    && w.UnitCost < _parPrice)                    
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
        //    var data = _db.PsCardItemUnitGroupDescriptionItems
        //        .Include(i => i.PsCardItem.PsCard.ItemCode.ItemType)
        //        .Include(i => i.PsCardItem.IcsParItems)
        //        .AsNoTracking()
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
                    Qty = s.Qty,
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
                    GeneratedItems = (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    InsertedDt = s.InsertedDt,
                    InvDist = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    IsConsumableSetup = s.PsCard.ItemCode.IsConsumable,
                    IsIncorporatedSetup = s.PsCard.ItemCode.IsIncorporated,
                    ForDistributionSetup = s.PsCard.ItemCode.ForDistribution
                }).AsQueryable();
            return data;
        }
        
        public async ValueTask<IcsVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems.Where(w => w.Id == id)
                .Select(s => new IcsVM
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
                    IcsBalance = s.Qty - (_db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.GroupId == s.GroupId && w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId),
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    AcqDate = s.AcqDate
                }).FirstOrDefaultAsync();
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

            if (cardItem.IcsBalance == 0)
            {
                throw new InvalidValueException(string.Format("All Items have ICS."));
            }

            
            if (model.Qty > cardItem.IcsBalance)
            {
                throw new InvalidValueException(string.Format("Cannot generate more than the available balance."));
            }


            // -----------
            var selectedIds = model.SelectedIds.Split(',');
            if (selectedIds.Count() == 0)
            {
                throw new InvalidValueException(string.Format("No selected items, cannot generate."));
            }            
            // -----------


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

            // generate par per item 
            
            //foreach (var psCardItemExtn in psCardItemExtnList)
            foreach(var selectedId in selectedIds)
            {

                if (icsSw == true)
                {
                    var existingIcs = await _db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == model.PsCardItemId).FirstOrDefaultAsync();
                    if (existingIcs == null)
                    {
                        icsParId = Guid.NewGuid();
                        var refNo = await NextRefNoAsync(model.Date, model.RefType);
                        icsPar = new IcsPar()
                        {
                            Id = (Guid)icsParId,
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
                        icsSw = false;
                        _db.IcsPars.Add(icsPar);
                        _db.Entry(icsPar).State = EntityState.Added;
                        await _db.SaveChangesAsync();
                    }
                    else
                    {
                        icsParId = existingIcs.IcsParId;
                    }
                }

                //var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                //var propSplit = propNo.Split('/');
                //var propSeq = propSplit[propSplit.Length - 2];

                IcsParItem icsParItem = new IcsParItem()
                {
                    Id = Guid.NewGuid(),
                    IcsParId = icsParId,
                    //PsCardItemExtnId = psCardItemExtn.Id,
                    PsCardItemExtnId = Guid.Parse(selectedId),
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

                // TO DO:
                // Save to PsCardItemLocations


            }
            return model;
        });

        /*
         * Generate Batch ICS for each Item of same PO Number (Contained in cardItemIdList)
         * 
         */
        public ValueTask<GenerateIcsParVM> GenerateIcsBatch(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatch(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            //var cardItemIdList = await _db.PsCardItems.Where(w => w.PoNo == model.PoNo && w.DeptId == model.DeptId
            //&& w.PsCardItemExtns.Any(a => a.IcsParItems.Count() == 0)).AsNoTracking()
            //    //&& !w.IcsParItems.Any(a => a.PsCardItemId == w.Id)).AsNoTracking()
            //    .Select(s => s.Id).ToListAsync();

            var cardItemIdList = await _db.PsCardItems.Where(w => w.PoNo == model.PoNo && w.DeptId == model.DeptId).AsNoTracking()
                .Select(s => s.Id).ToListAsync();

            Guid? icsParId = null;
            IcsPar icsPar = null;
            bool icsSw = true;

            foreach (var cardItemId in cardItemIdList)
            {
                var cardItem = await GetByIdAsync(cardItemId);
                if (cardItem == null)
                {
                    continue;
                }

                if (cardItem.IsConsumable == true)
                {
                    continue;
                }

                if (cardItem.IsIncorporated == true)
                {
                    continue;
                }

                if (cardItem.IsOthers == true)
                {
                    continue;
                }

                if (cardItem.IcsBalance == 0)
                {
                    continue;
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
                else
                {
                    acqYear = cardItem.PoDate.Value.Year.ToString();
                }


                if (string.IsNullOrEmpty(acqYear))
                {
                    continue;
                }

                var psCardItemExtnList = await _db.PsCardItemExtns.Where(w => w.PsCardItemId == cardItemId && w.IcsParItems.Count() == 0).ToListAsync();
                if (psCardItemExtnList.Count() == 0)
                {
                    for (var qty = 0; qty < cardItem.IcsBalance; ++qty)
                    {
                        var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                        var propSplit = propNo.Split('/');
                        var propSeq = propSplit[propSplit.Length - 2];

                        PsCardItemExtn psCardItemExtn = new PsCardItemExtn()
                        {
                            Id = Guid.NewGuid(),
                            PsCardItemId = cardItemId,
                            LocationId = model.LocationId,
                            PropNo = propNo,
                            PropYear = acqYear,
                            PropSeq = propSeq,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        _db.PsCardItemExtns.Add(psCardItemExtn);
                        _db.Entry(psCardItemExtn).State = EntityState.Added;
                        await _db.SaveChangesAsync();

                        psCardItemExtnList.Add(psCardItemExtn);
                    }
                }

                // generate ics per cardItemExtn 
                //for (var qty = 0; qty < cardItem.IcsBalance; ++qty)
                foreach (var psCardItemExtn in psCardItemExtnList)
                {

                    if (icsSw == true)
                    {
                        //var existingIcs = await _db.IcsParItems.Where(w => w.PsCardItem.PoNo == cardItem.PoNo && w.PsCardItem.PoDate == cardItem.PoDate).FirstOrDefaultAsync();
                        var existingIcs = await _db.IcsParItems.Where(w => w.PsCardItemExtn.PsCardItem.Id == model.PsCardItemId).FirstOrDefaultAsync();
                        if (existingIcs == null)
                        {
                            icsParId = Guid.NewGuid();
                            var refNo = await NextRefNoAsync(model.Date, model.RefType);
                            icsPar = new IcsPar()
                            {
                                Id = (Guid)icsParId,
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
                            icsSw = false;
                            _db.IcsPars.Add(icsPar);
                            _db.Entry(icsPar).State = EntityState.Added;
                            await _db.SaveChangesAsync();
                        }
                        else
                        {
                            icsParId = existingIcs.IcsParId;
                        }
                    }

                    //var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                    //var propSplit = propNo.Split('/');
                    //var propSeq = propSplit[propSplit.Length - 2];

                    IcsParItem icsParItem = new IcsParItem()
                    {
                        Id = Guid.NewGuid(),
                        IcsParId = icsParId,
                        PsCardItemExtnId = psCardItemExtn.Id,
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
                }
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
            // yyyy-mm-99999
            // 1234567890123

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

        public ValueTask<PsCardItemUnitGroupDescriptionItem> UpdateNoICSAsync(PsCardItemUnitGroupDescriptionItem model, string user, DateTime date)
        => _psCardItemUnitGroupDescriptionItemService.TryCatch(async () =>
        {
            var parIcsItemVm = await GetItemByIdAsync(model.PsCardItemId);
            await _psCardItemService.UpdateNoICSAsync(parIcsItemVm, user, date);
            return model;
        });

        public ValueTask<PsCardItem> PostAsync(Guid? groupId, string user, DateTime date) => _postExceptionService.TryCatch(async () =>
        {
            var entity = _db.PsCardItems.Where(w => w.GroupId == groupId);
            if (entity.Count() == 0)
            {
                throw new NotFoundException((Guid)groupId);
            }

            var psCardItem = entity.FirstOrDefault(f => f.TransferRefId == null);

            if (psCardItem.IsForICS == false)
            {
                var partItems = _icsParItemService.GetAllParItems(groupId);
                if (partItems.Count() < psCardItem.Qty)
                {
                    throw new InvalidValueException("Insufficient ICS Item created.");
                }
            }

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

    }
}