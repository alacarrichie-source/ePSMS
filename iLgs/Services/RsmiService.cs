using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Codes;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IRsmiService
    {
        IQueryable<RsmiVM> GetAll();
        ValueTask<RSMIProcessVM> GenerateAsync(RSMIProcessVM model, string user, DateTime date);
        ValueTask<RsmiVM> UpdateAsync(RsmiVM model, string user, DateTime date);
        ValueTask<RsmiVM> DeleteAsync(RsmiVM model, string user, DateTime date);

        ValueTask<RSMI> PostAsync(Guid? id, string user, DateTime date);
        ValueTask<RSMI> UnPostAsync(Guid? id, string user, DateTime date);
    }

    public class RsmiService : IRsmiService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RSMI> _exceptionService;
        private readonly IExceptionService<RsmiVM> _vmExceptionService;
        private readonly IExceptionService<RSMIProcessVM> _processExceptionService;
        private readonly IPriceCapService _priceCapService;

        private decimal? _priceCap;

        public RsmiService(AppManEntities db, 
            ICreateAndLogExceptions exceptions,
            IExceptionService<RSMI> exceptionService,
            IExceptionService<RsmiVM> vmExceptionService,
            IExceptionService<RSMIProcessVM> processExceptionService,
            IPriceCapService priceCapService)
        {
            _db = db;
            _db.Database.CommandTimeout = 3000;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _vmExceptionService = vmExceptionService;
            _processExceptionService = processExceptionService;
            _priceCapService = priceCapService;
        }

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        public IQueryable<RsmiVM> GetAll() =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RSMIs
                .Select(s => new RsmiVM
                {
                    Id = s.Id,
                    Date = s.Date,
                    SerialNo = s.SerialNo,
                    Fund = s.Fund,
                    Custodian = s.Custodian,
                    Qty = s.RSMIItems.Sum(x => x.Qty),
                    Amount = s.RSMIItems.Sum(x => x.Amount),
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public ValueTask<RSMIProcessVM> GenerateAsync(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            var priceCap = GetPriceCap();
            var rsmiItemList = await _db.PsCardItemTransferIssuances.AsNoTracking()
                        .Where(w => w.IssuedDate >= model.DateFrom && w.IssuedDate <= model.DateTo
                            && 
                            (
                                _db.PsCardItemUnitGroups.Any(a => a.PoNo == w.PsCardItemTransfer.PsCardItem.PoNo && a.UnitCost < priceCap)
                             || 
                                (
                                !_db.PsCardItemUnitGroups.Any(a => a.PoNo == w.PsCardItemTransfer.PsCardItem.PoNo && a.UnitCost < priceCap)
                                && 
                                w.PsCardItemTransfer.PsCardItem.UnitCost < priceCap
                                )
                            )
                        )
                        .Select(s => new RSMIItemVM
                        {
                            ItemCodeId = s.PsCardItemTransfer.PsCardItem.PsCard.ItemCodeId,
                            RisNo = s.PsCardItemTransfer.PsCardItem.OrderItem.RequestItem.RisItem.RISs.RisNo,
                            Date = s.IssuedDate,
                            Fund = s.PsCardItemTransfer.PsCardItem.PsCard.Fund,
                            RCC = s.PsCardItemTransfer.PsCardItem.FPP,
                            PoNo = s.PsCardItemTransfer.PsCardItem.PoNo,
                            Department = s.PsCardItemTransfer.PsCardItem.DeptDisplay,
                            LocationCode = s.Codextn1.Code,
                            Location = s.Codextn1.Description,
                            ItemCode = s.PsCardItemTransfer.PsCardItem.PsCard.ItemCode.Code,
                            StockNo = s.PsCardItemTransfer.PsCardItem.PsCard.PsNo,
                            ItemName = s.PsCardItemTransfer.PsCardItem.Description,
                            Unit = s.PsCardItemTransfer.PsCardItem.Unit,
                            UnitCost = s.PsCardItemTransfer.PsCardItem.UnitCost,
                            Qty = (int?)s.Qty,
                            Amount = s.Amount,
                            AccountCode = s.PsCardItemTransfer.PsCardItem.PsCard.ItemCode.AccountCode
                        }).ToListAsync();

            if (!rsmiItemList.Any())
            {
                throw new InvalidValueException("Period", "No Issuances found on period entered.");
            }

            DateTime? groupDate = null;
            string serialNo = "";
            var rsmiDateList = rsmiItemList.GroupBy(g => new { g.Date, g.Fund })
                .Select(s => new { s.Key.Date, s.Key.Fund }).OrderBy(o => o.Fund).ThenBy(o => o.Date).ToList();

            foreach (var rsmiDate in rsmiDateList)
            {
                if (groupDate != rsmiDate.Date)
                {
                    serialNo = NextSerialNo(rsmiDate.Fund, rsmiDate.Date);
                    groupDate = rsmiDate.Date;
                }
                var entity = new RSMI()
                {
                    Id = Guid.NewGuid(),
                    Date = rsmiDate.Date,
                    Fund = rsmiDate.Fund,
                    SerialNo = serialNo,
                    Custodian = model.Custodian,
                    PostedBy = model.PostedBy,
                    PostedDt = model.PostedDt,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                var itemIssuedList = rsmiItemList
                    .Where(w => w.Fund == rsmiDate.Fund && w.Date == rsmiDate.Date).ToList();

                foreach (var itemIssued in itemIssuedList)
                {
                    var rsmiItem = new RSMIItem()
                    {
                        Id = Guid.NewGuid(),
                        RsmiId = entity.Id,
                        ItemCodeId = itemIssued.ItemCodeId,
                        RisNo = itemIssued.RisNo,
                        PoNo = itemIssued.PoNo,
                        Department = itemIssued.Department,
                        RCC = itemIssued.RCC,
                        LocationCode = itemIssued.LocationCode,
                        Location = itemIssued.Location,
                        ItemCode = itemIssued.ItemCode,
                        StockNo = itemIssued.StockNo,
                        ItemName = itemIssued.ItemName,
                        Unit = itemIssued.Unit,
                        UnitCost = itemIssued.UnitCost,
                        Qty = itemIssued.Qty,
                        Amount = itemIssued.Amount,
                        AccountCode = itemIssued.AccountCode,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    entity.RSMIItems.Add(rsmiItem);
                }

                var recapList = entity.RSMIItems.GroupBy(g => new { g.StockNo, g.AccountCode, g.UnitCost })
                    .Select(s => new
                    {
                        StockNo = s.Key.StockNo,
                        AccountCode = s.Key.AccountCode,
                        UnitCost = s.Key.UnitCost,
                        Qty = s.Sum(f => f.Qty),
                        TotalCost = s.Sum(f => f.Amount)
                    }).ToList();

                foreach (var recap in recapList)
                {
                    var rsmiRecap = new RSMIRecap()
                    {
                        Id = Guid.NewGuid(),
                        RsmiId = entity.Id,
                        StockNo = recap.StockNo,
                        Qty = recap.Qty,
                        UnitCost = recap.UnitCost,
                        TotalCost = recap.TotalCost,
                        AccountCode = recap.AccountCode,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    entity.RSMIRecaps.Add(rsmiRecap);
                }

                _db.RSMIs.Add(entity);
                await _db.SaveChangesAsync();
            }

            return model;
        });

        public ValueTask<RsmiVM> UpdateAsync(RsmiVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = _db.RSMIs.Find(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;
            
            entity.Custodian = model.Custodian;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RSMIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RsmiVM> DeleteAsync(RsmiVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = _db.RSMIs.Find(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RSMIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RSMIs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RSMI> PostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RSMIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}");
            }            

            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RSMIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RSMI> UnPostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RSMIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RSMIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        private string NextSerialNo(string fund, DateTime? date)
        {
            string yyyy = date.Value.Year.ToString().Trim();
            string mm = date.Value.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.RSMIs.Where(w => w.Fund == fund && w.Date.Value.Year == date.Value.Year && w.Date.Value.Month == date.Value.Month).OrderByDescending(o => o.SerialNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.SerialNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}