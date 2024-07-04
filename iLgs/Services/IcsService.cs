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
        ValueTask<IcsVM> GetByIdAsync(Guid? id);
        
        IIcsParItemService IcsParItem { get; }
        IPsCardItemIssuanceService PsCardItemIssaunce { get; }

        ValueTask<GenerateIcsParVM> GenerateIcs(GenerateIcsParVM model, string user, DateTime date);
    }

    public class IcsService : IIcsService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private decimal _parPrice = 50000;

        private IIcsParItemService _icsParItemService;
        private IPsCardItemIssuanceService _psCardItemIssaunce;
        private readonly IExceptionService<GenerateIcsParVM> _generateParExceptionService = new ExceptionService<GenerateIcsParVM>();

        public IcsService(AppManEntities db)
        {
            _db = db;
            _icsParItemService = new IcsParItemService(db);
            _psCardItemIssaunce = new PsCardItemIssuanceService(db);
        }

        public IIcsParItemService IcsParItem { get { return _icsParItemService = _icsParItemService ?? new IcsParItemService(_db); } }
        public IPsCardItemIssuanceService PsCardItemIssaunce { get { return _psCardItemIssaunce = _psCardItemIssaunce ?? new PsCardItemIssuanceService(_db); } }

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
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
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

            if (cardItem.IcsBalance == 0)
            {
                throw new InvalidValueException(string.Format("All Items have PARs."));
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
    }
}