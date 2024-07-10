using iLgs.Exceptions;
using iLgs.Exceptions.PARs;
using iLgs.Models;
using iLgs.Services.Interfaces;
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
        IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo);
        IQueryable<PsCardItemUnitGroupDescription> GetItemSetDescriptionsByUnitGroupId(Guid? unitGroupId);
        //IQueryable<PsCardItemUnitGroupDescriptionItem> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        IQueryable<ParIcsItemVm> GetItemSetDescriptionItemsByUnitGroupDescriptionId(Guid? unitGroupDescriptionId);
        ValueTask<IcsVM> GetByIdAsync(Guid? id);
        IIcsParItemService IcsParItem { get; }
        IPsCardItemService PsCardItem { get; }
        IPsCardItemIssuanceService PsCardItemIssaunce { get; }
        ValueTask<GenerateIcsParVM> GenerateIcs(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<GenerateIcsParVM> GenerateIcsBatch(GenerateIcsParVM model, string user, DateTime date);
        ValueTask<PsCardItemUnitGroupDescriptionItem> UpdateNoICSAsync(PsCardItemUnitGroupDescriptionItem model, string user, DateTime date);
    }

    public class IcsService : IIcsService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private decimal _parPrice = 50000;

        private IIcsParItemService _icsParItemService;
        private IPsCardItemService _psCardItemService;
        private IPsCardItemIssuanceService _psCardItemIssaunceService;
        private readonly IExceptionService<GenerateIcsParVM> _generateParExceptionService = new ExceptionService<GenerateIcsParVM>();
        private readonly IExceptionService<PsCardItemUnitGroupDescriptionItem> _psCardItemUnitGroupDescriptionItemService = new ExceptionService<PsCardItemUnitGroupDescriptionItem>();        

        public IcsService(AppManEntities db)
        {
            _db = db;
            _icsParItemService = new IcsParItemService(db);
            _psCardItemService = new PsCardItemService(db);
            _psCardItemIssaunceService = new PsCardItemIssuanceService(db);
        }

        public IIcsParItemService IcsParItem { get { return _icsParItemService = _icsParItemService ?? new IcsParItemService(_db); } }
        public IPsCardItemService PsCardItem { get { return _psCardItemService = _psCardItemService ?? new PsCardItemService(_db); } }
        public IPsCardItemIssuanceService PsCardItemIssaunce { get { return _psCardItemIssaunceService = _psCardItemIssaunceService ?? new PsCardItemIssuanceService(_db); } }

        public IQueryable<IcsVM> GetAll()
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => !w.OrderItem.OrderItemUnitGroupDescriptionItems.Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice) 
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
                    Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
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
                    IcsBalance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) - (s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
                }).AsQueryable();
            //var data = _db.Database.SqlQuery<ParVM>("Exec PARS_GetAll {0}", "").AsQueryable();

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPo()
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.OrderItem.OrderItemUnitGroupDescriptionItems
                    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)

                    || (w.UnitCost >= _parPrice && w.IsForICS == true)
                    || w.UnitCost < _parPrice)
                .Select(s => new
                {
                    s.PoNo,
                    s.PoDate,
                    s.AirDate,
                    s.AirNo,
                    s.DeptId,
                    s.Codextn.Description
                }).GroupBy(g => new { g.DeptId, g.Description, g.PoNo, g.PoDate, g.AirNo, g.AirDate })
                .Select(s => new ParIcsPOGroupVM
                {
                    Id = Guid.NewGuid(),
                    PoNo = s.Key.PoNo,
                    PoDate = s.Key.PoDate,
                    AirNo = s.Key.AirNo,
                    AirDate = s.Key.AirDate,
                    DeptId = s.Key.DeptId,
                    Department = s.Key.Description
                })
                .AsQueryable();

            //DeptDisplay = s.DeptDisplay,
            //LocCode = s.Codextn1.Code,
            //Location = s.Codextn1.Description,
            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPoCombo()
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.OrderItem.OrderItemUnitGroupDescriptionItems
                    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                    || (w.UnitCost >= _parPrice && w.IsForICS == true)
                    || w.UnitCost < _parPrice)
                .Select(s => new
                {
                    s.PoNo,
                    s.PoDate,
                    s.AirDate,
                    s.AirNo,
                    s.DeptId,
                    s.Codextn.Description
                }).GroupBy(g => new { g.DeptId, g.Description, g.PoNo, g.PoDate, g.AirNo, g.AirDate })
                .Select(s => new ParIcsPOGroupVM
                {
                    Id = Guid.NewGuid(),
                    PoNo = s.Key.PoNo,
                    PoDate = s.Key.PoDate,
                    AirNo = s.Key.AirNo,
                    AirDate = s.Key.AirDate,
                    DeptId = s.Key.DeptId,
                    Department = s.Key.Description
                })
                .AsQueryable().Take(100);

            return data;
        }

        public IQueryable<ParIcsPOGroupVM> GetAllPoCombo(string text)
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => (w.OrderItem.OrderItemUnitGroupDescriptionItems
                    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)                    
                    || (w.UnitCost >= _parPrice && w.IsForICS == true)
                    || w.UnitCost < _parPrice) 
                    && (w.PoNo.Contains(text) || w.AirNo.Contains(text) || w.Codextn.Description.Contains(text)))
                .Select(s => new
                {
                    s.PoNo,
                    s.PoDate,
                    s.AirDate,
                    s.AirNo,
                    s.DeptId,
                    s.Codextn.Description
                }).GroupBy(g => new { g.DeptId, g.Description, g.PoNo, g.PoDate, g.AirNo, g.AirDate })
                .Select(s => new ParIcsPOGroupVM
                {
                    Id = Guid.NewGuid(),
                    PoNo = s.Key.PoNo,
                    PoDate = s.Key.PoDate,
                    AirNo = s.Key.AirNo,
                    AirDate = s.Key.AirDate,
                    DeptId = s.Key.DeptId,
                    Department = s.Key.Description
                })
                .AsQueryable().Take(100);

            return data;
        }

        public async ValueTask<ParIcsItemVm> GetItemByIdAsync(Guid? psCardItemId)
        {
            var data = await _db.PsCardItems
                .Where(w => w.Id == psCardItemId)
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    Balance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) - (s.IcsParItems.Where(w => w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = s.IcsParItems.Count(),
                    InsertedDt = s.InsertedDt
                }).FirstOrDefaultAsync();
            return data;
        }

        public IQueryable<ParIcsItemVm> GetItemsByPoNo(string poNo)
        {
            var data = _db.PsCardItems.AsNoTracking()
                .Where(w => w.PoNo == poNo 
                    && !w.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == w.Id)
                    && ((w.UnitCost >= _parPrice && w.IsForICS == true) || w.UnitCost < _parPrice))
                .Select(s => new ParIcsItemVm
                {
                    Id = s.Id,
                    Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    Balance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) - (s.IcsParItems.Where(w => w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = s.IcsParItems.Count(),
                    InsertedDt = s.InsertedDt
                }).AsQueryable();
            return data;
        }

        public IQueryable<ParIcsItemSetVm> GetItemSetsByPoNo(string poNo)
        {
            var data = _db.PsCardItemUnitGroups.AsNoTracking()
                .Where(w => w.PsCardItemUnitGroupDescriptions.Any(a => a.PsCardItemUnitGroupDescriptionItems
                    .Any(b => b.PsCardItem.PoNo == poNo))
                    && (w.UnitCost < _parPrice)
                    )
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
                    Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    TotalCost = s.Amount,
                    Article = s.PsCard.ItemCode.Description,
                    Description = s.Description,
                    StockNo = s.PsCard.PsNo,
                    Balance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) - (s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    IsForICS = s.IsForICS,
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks,
                    GeneratedItems = s.IcsParItems.Count(),
                    InsertedDt = s.InsertedDt
                }).AsQueryable();
            return data;
        }

        
        public async ValueTask<IcsVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.PsCardItems.Where(w => w.Id == id)
                .Select(s => new IcsVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    Qty = s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0),
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
                    IcsBalance = (s.Qty + (s.TransferIn ?? 0) - (s.TransferOut ?? 0)) - (s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId),
                    IsConsumable = s.IsConsumable,
                    IsIncorporated = s.IsIncorporated,
                    IsOthers = s.IsOthers,
                    OtherRemarks = s.OtherRemarks
                }).FirstOrDefaultAsync();
            return data;
        }

        public ValueTask<GenerateIcsParVM> GenerateIcs(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatchAsync(async () =>
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

            var refType = (await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking().Where(w => w.PsCardItemId == model.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync())?.IcsPar.RefType;
            if (!string.IsNullOrWhiteSpace(refType))
            {
                throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
            }

            Guid? icsParId = null;
            IcsPar icsPar = null;
            bool icsSw = true;
            string acqYear;

            if (cardItem.AirDate != null)
            {
                acqYear = cardItem.AirDate.Value.Year.ToString();
            }
            else
            {
                acqYear = cardItem.PoDate.Value.Year.ToString();
            }

            // generate par per qty 
            for (var qty = 0; qty < model.Qty; ++qty)
            {

                if (icsSw == true)
                {
                    //var existingIcs = await _db.IcsParItems.FirstOrDefaultAsync(f => f.PsCardItemId == model.PsCardItemId);
                    var existingIcs = await _db.IcsParItems.Where(w => w.PsCardItem.PoNo == cardItem.PoNo && w.PsCardItem.PoDate == cardItem.PoDate).FirstOrDefaultAsync();
                    if (existingIcs == null)
                    {
                        //// check other PO for existing ICS
                        //var psCardList = await _db.PsCardItems.Where(w => w.Id != model.PsCardItemId 
                        //   && w.PoNo == cardItem.PoNo && w.PoDate == cardItem.PoDate
                        //   && (w.PoNo != "" || w.PoNo != null)).ToListAsync();
                        //if (psCardList.Any())
                        //{

                        //}

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

                var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                var propSplit = propNo.Split('/');
                var propSeq = propSplit[propSplit.Length - 2];
                IcsParItem icsParItem = new IcsParItem()
                {
                    Id = Guid.NewGuid(),
                    IcsParId = icsParId,
                    PsCardItemId = model.PsCardItemId,
                    Qty = 1,
                    Amount = cardItem.UnitCost,
                    LocationId = model.LocationId,
                    PropNo = propNo,
                    PropYear = acqYear,
                    PropSeq = propSeq,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                _db.IcsParItems.Add(icsParItem);
                _db.Entry(icsParItem).State = EntityState.Added;
                await _db.SaveChangesAsync();
            }
            return model;
        });

        public ValueTask<GenerateIcsParVM> GenerateIcsBatch(GenerateIcsParVM model, string user, DateTime date) =>
        _generateParExceptionService.TryCatchAsync(async () =>
        {
            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            var cardItemIdList = await _db.PsCardItems.Where(w => w.PoNo == model.PoNo && !w.IcsParItems.Any(a => a.PsCardItemId == w.Id)).AsNoTracking()
                .Select(s => s.Id).ToListAsync();

            //var refType = (await _db.IcsParItems.Include(i => i.IcsPar).AsNoTracking().Where(w => w.PsCardItemId == model.PsCardItemId && w.IcsPar.RefType != model.RefType).FirstOrDefaultAsync())?.IcsPar.RefType;
            //if (!string.IsNullOrWhiteSpace(refType))
            //{
            //    throw new InvalidValueException(string.Format("{0} already exists, cannot add {1} as new reference type.", RefTypeDesc(refType), RefTypeDesc(model.RefType)));
            //}

            Guid? icsParId = null;
            IcsPar icsPar = null;
            bool icsSw = true;
            string acqYear;

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

                if (cardItem.AirDate != null)
                {
                    acqYear = cardItem.AirDate.Value.Year.ToString();
                }
                else
                {
                    acqYear = cardItem.PoDate.Value.Year.ToString();
                }

                // generate par per qty 
                for (var qty = 0; qty < cardItem.IcsBalance; ++qty)
                {

                    if (icsSw == true)
                    {
                        //var existingIcs = await _db.IcsParItems.FirstOrDefaultAsync(f => f.PsCardItemId == model.PsCardItemId);
                        var existingIcs = await _db.IcsParItems.Where(w => w.PsCardItem.PoNo == cardItem.PoNo && w.PsCardItem.PoDate == cardItem.PoDate).FirstOrDefaultAsync();
                        if (existingIcs == null)
                        {
                            //// check other PO for existing ICS
                            //var psCardList = await _db.PsCardItems.Where(w => w.Id != model.PsCardItemId 
                            //   && w.PoNo == cardItem.PoNo && w.PoDate == cardItem.PoDate
                            //   && (w.PoNo != "" || w.PoNo != null)).ToListAsync();
                            //if (psCardList.Any())
                            //{

                            //}

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

                    var propNo = NextPropNo(acqYear, cardItem.StockNo, model.LocationCode, model.RefType);
                    var propSplit = propNo.Split('/');
                    var propSeq = propSplit[propSplit.Length - 2];
                    IcsParItem icsParItem = new IcsParItem()
                    {
                        Id = Guid.NewGuid(),
                        IcsParId = icsParId,
                        PsCardItemId = cardItemId,
                        Qty = 1,
                        Amount = cardItem.UnitCost,
                        LocationId = model.LocationId,
                        PropNo = propNo,
                        PropYear = acqYear,
                        PropSeq = propSeq,
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
            // yyyy-mm-9999
            // 123456789012

            //var data = await _db.RisIssueds.Where(w => w.RefType == refType && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            var data = await _db.IcsPars.Where(w => w.RefType == refType && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.RefNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
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
        
    }
}