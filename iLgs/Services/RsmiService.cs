using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
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
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RSMI> _exceptionService;
        private readonly IExceptionService<RsmiVM> _vmExceptionService;
        private readonly IExceptionService<RSMIProcessVM> _processExceptionService;
        private readonly IPriceCapService _priceCapService;
        private readonly ISemiExpendableService _semiExpendableService;

        private decimal? _priceCap;
        private decimal? _SPHV;

        public RsmiService(AppManEntities db, 
            IAppManEntitiesFactory appManEntitiesFactory,
            ICreateAndLogExceptions exceptions,
            IExceptionService<RSMI> exceptionService,
            IExceptionService<RsmiVM> vmExceptionService,
            IExceptionService<RSMIProcessVM> processExceptionService,
            IPriceCapService priceCapService,
            ISemiExpendableService semiExpendableService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _db.Database.CommandTimeout = 3000;
            _exceptions = exceptions;
            _exceptionService = exceptionService;
            _vmExceptionService = vmExceptionService;
            _processExceptionService = processExceptionService;
            _priceCapService = priceCapService;
            _semiExpendableService = semiExpendableService;
        }

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        private decimal GetSPHV()
        {
            return _SPHV ?? (_SPHV = _semiExpendableService.GetSPHV()).Value;
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
                    QtyCons = s.RSMIItems.Where(w => w.ItemType == "C").Sum(x => x.Qty),
                    AmountCons = s.RSMIItems.Where(w => w.ItemType == "C").Sum(x => x.Amount),
                    QtySPHV = s.RSMIItems.Where(w => w.ItemType == "SPHV").Sum(x => x.Qty),
                    AmountSPHV = s.RSMIItems.Where(w => w.ItemType == "SPHV").Sum(x => x.Amount),
                    QtySPLV = s.RSMIItems.Where(w => w.ItemType == "SPLV").Sum(x => x.Qty),
                    AmountSPLV = s.RSMIItems.Where(w => w.ItemType == "SPLV").Sum(x => x.Amount),
                    QtySE = s.RSMIItems.Where(w => w.ItemType.Substring(0, 1) == "S").Sum(x => x.Qty),
                    AmountSE = s.RSMIItems.Where(w => w.ItemType.Substring(0, 1) == "S").Sum(x => x.Amount),
                    Qty = s.RSMIItems.Sum(x => x.Qty),
                    Amount = s.RSMIItems.Sum(x => x.Amount),
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public ValueTask<RSMIProcessVM> GenerateAsyncOld(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            if (model.DateFrom.Value.Year != model.DateTo.Value.Year)
            {
                throw new InvalidValueException("Year or date range must be the same.");
            }

            var priceCap = _priceCapService.GetPriceCap(model.DateFrom);
            var sphv = _semiExpendableService.GetSPHV(model.DateFrom);

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var rsmiItemList = await ctx.PsCardItemTransferIssuances.AsNoTracking()
                            .Where(w => w.IssuedDate >= model.DateFrom && w.IssuedDate <= model.DateTo
                                &&
                                (
                                    ctx.PsCardItemUnitGroups.Any(a => a.PoNo == w.PsCardItemTransfer.PsCardItem.PoNo && a.UnitCost < priceCap)
                                 ||
                                    (
                                    !ctx.PsCardItemUnitGroups.Any(a => a.PoNo == w.PsCardItemTransfer.PsCardItem.PoNo && a.UnitCost < priceCap)
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
                                AccountCode = s.PsCardItemTransfer.PsCardItem.PsCard.ItemCode.AccountCode,
                            ItemType = s.PsCardItemTransfer.PsCardItem.IsConsumable == true ? "C" :
                                    s.PsCardItemTransfer.PsCardItem.IsConsumable == false
                                    ? (s.PsCardItemTransfer.PsCardItem.UnitCost >= sphv ? "SPHV" : "SPLV") :
                                    s.PsCardItemTransfer.PsCardItem.PsCard.ItemCode.IsConsumable == "Y" ? "C" :
                                    s.PsCardItemTransfer.PsCardItem.PsCard.ItemCode.IsConsumable == "N"
                                    ? (s.PsCardItemTransfer.PsCardItem.UnitCost >= sphv ? "SPHV" : "SPLV") : ""
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
                            ItemType = itemIssued.ItemType,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        entity.RSMIItems.Add(rsmiItem);
                    }

                    var recapList = entity.RSMIItems.GroupBy(g => new { g.ItemType, g.ItemCodeId, g.StockNo, g.AccountCode, g.UnitCost })
                        .Select(s => new
                        {
                            ItemType = s.Key.ItemType,
                            ItemCodeId = s.Key.ItemCodeId,
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
                            ItemCodeId = recap.ItemCodeId,
                            ItemType = recap.ItemType,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        entity.RSMIRecaps.Add(rsmiRecap);
                    }

                    ctx.RSMIs.Add(entity);
                    await ctx.SaveChangesAsync();
                }
            }

            return model;
        });

        public ValueTask<RSMIProcessVM> GenerateAsync(RSMIProcessVM model, string user, DateTime date) =>
        _processExceptionService.TryCatch(async () =>
        {
            if (model.DateFrom.Value.Year != model.DateTo.Value.Year)
            {
                throw new InvalidValueException("Year or date range must be the same.");
            }

            //var priceCap = _priceCapService.GetPriceCap(model.DateFrom);
            var sphv = _semiExpendableService.GetSPHV(model.DateFrom);

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                if (!(await ctx.PsCardItemTransferIssuances
                            .Where(w => w.IssuedDate >= model.DateFrom && w.IssuedDate <= model.DateTo).AnyAsync()))
                {
                    throw new InvalidValueException("Period", "No Issuances found on period entered.");
                }

                await ctx.Database.ExecuteSqlCommandAsync("Exec RSMI_Generate {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}",
                    model.DateFrom, model.DateTo, sphv, model.Custodian, model.PostedBy, model.PostedDt, user, date);
            }

            return model;
        });

        public ValueTask<RsmiVM> UpdateAsync(RsmiVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.RSMIs.FindAsync(model.Id);
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

                //_db.RSMIs.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();
            }

            return model;
        });

        public ValueTask<RsmiVM> DeleteAsync(RsmiVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.RSMIs.FindAsync(model.Id);
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

                //_db.RSMIs.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.RSMIs.Remove(entity);
                //_db.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();
            }

            return model;
        });

        public ValueTask<RSMI> PostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.RSMIs.FindAsync(id);
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

                //_db.RSMIs.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                return entity;
            }
        });

        public ValueTask<RSMI> UnPostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.RSMIs.FindAsync(id);
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

                //_db.RSMIs.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                return entity;
            }
        });

        private string NextSerialNoOld(string fund, DateTime? date)
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

        private string NextSerialNo(string fund, DateTime? date)
        {
            return _db.Database.SqlQuery<string>("Select dbo.fn_NextRsmiNo({0}, {1})", fund, date).FirstOrDefault();
        }
    }
}