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
    public interface IParService
    {
        IQueryable<PAR_VM> GetAll();
        IQueryable<PsCardItemVM> GetAllAsync();
        IQueryable<PARAcknowledgementVM> GetAcknowledgedOrderItems(Guid? orderItemId);
        Task<PARAcknowledgementVM> GetAcknowledgedOrderItemByItemId(Guid? parItemId);
        Task<Models.PAR> GetByIdAsync(Guid parId);
        Task<Models.PAR> GetByParNoAsync(string parNo);
        Task<int?> GetRemainingQty(Guid? orderItemId, Guid? parItemId);
        Task<bool> IsAnyParNoAsync(Guid parId, string parNo);
        Task<bool> IsPostedAsync(Guid parId);

        Task<PAR_VM> CreateAsync(PAR_VM model, string user, DateTime date);
        Task<PAR_VM> UpdateAsync(PAR_VM model, string user, DateTime date);
        Task<PAR_VM> DeleteAsync(PAR_VM model, string user, DateTime date);

        Task PostAsync(Guid parId, string user, DateTime date);
        Task UnpostAsync(Guid parId, string user, DateTime date);
        Task GeneratePAR(GenerateParVM model, string user, DateTime date);

        Task<PARAcknowledgementVM> CreateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
        Task<PARAcknowledgementVM> UpdateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
        Task<PARAcknowledgementVM> DeleteAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date);
    }

    public class ParService : IParService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private decimal _parPrice = 50000;

        public ParService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<PAR_VM> GetAll()
        {
            var data = _db.PARs
                .Select(s => new PAR_VM
                {
                    Id = s.Id,
                    OrderId = s.OrderId,
                    ParNo = s.ParNo,
                    ParDate = s.ParDate,
                    ReceivedBy = s.ReceivedBy,
                    ReceivedByPosition = s.ReceivedByPosition,
                    ReceivedDate = s.ReceivedDate,
                    IssuedBy = s.IssuedBy,
                    IssuedByPosition = s.IssuedByPosition,
                    IssuedDate = s.IssuedDate,
                    PoNo = s.Order.PoNo,
                    PoDate = s.Order.PoDate,
                    Fund = s.Order.Request.RISs.Fund,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt
                })
                .AsQueryable();
            return data;
        }

        public IQueryable<PsCardItemVM> GetAllAsync()
        {
            var data = _db.PsCardItems.AsNoTracking()            
                .Where(w => w.OrderItem.OrderItemUnitGroupDescriptionItems
                    .Any(a => a.OrderItemUnitGroupDescription.OrderItemUnitGroup.UnitCost >= _parPrice)
                    || w.UnitCost >= _parPrice)
                .Select(s => new PsCardItemVM
                {
                    Id = s.Id,
                    PsCardId = s.PsCardId,
                    OrderItemId = s.OrderItemId,
                    PoNo = s.PoNo,
                    PoDate = s.PoDate,
                    AirDate = s.AirDate,
                    AirNo = s.AirNo,
                    AirIssueDate = s.AirIssueDate,
                    Qty = s.Qty,
                    QtyIss = s.QtyIss,
                    QtyBal = s.QtyBal,
                    TransferIn = s.TransferIn,
                    TransferOut = s.TransferOut,
                    TranType = s.TranType,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount,
                    Days = s.Days,
                    Remarks = s.Remarks,
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
                    ParBalance = s.QtyBal - (s.IcsParItems.Where(w => w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    IcsBalance = s.QtyBal - (s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0),
                    RemBalance = s.QtyBal,
                    OrderItemUnitGroupDescriptionItem = s.OrderItem.OrderItemUnitGroupDescriptionItems.FirstOrDefault(f => f.OrderItemId == s.OrderItemId)
                   
                    //Id = s.Id,
                    //PoNo = s.PoNo,
                    //PoDate = s.PoDate,
                    //OrderItemId = s.OrderItemId,
                    //AirNo = s.AirNo,
                    //AirDate = s.AirDate,
                    //Department = s.Codextn.Description,
                    //PrNo = s.OrderItem.RequestItem.Request.PrNo,
                    //RisNo = s.OrderItem.RequestItem.RisItem.RISs.RisNo,
                    //Qty = s.Qty,
                    //QtyIss = s.QtyIss,
                    //TransferIn = s.TransferIn,
                    //TransferOut = s.TransferOut,
                    //Balance = s.QtyBal,
                    //UnitCost = s.UnitCost,
                    //Unit = s.Unit,
                    //StockNo = s.PsCard.PsNo,
                    //ItemName = s.PsCard.ItemCode.Description,                    
                    //Description = s.Description,
                    //IsWithPar = s.UnitCost >= _parPrice,
                    //IsWithIcs = s.UnitCost < _parPrice,
                    //ParBalance = s.Qty - (s.IcsParItems.Where(w => w.IcsPar.RefType == "P").Sum(x => x.Qty) ?? 0),
                    //IcsBalance = s.Qty - (s.IcsParItems.Where(w => w.IcsPar.RefType == "I").Sum(x => x.Qty) ?? 0)
                }).AsQueryable();
            return data;
        }


        public IQueryable<PARAcknowledgementVM> GetAcknowledgedOrderItems(Guid? orderItemId)
        {
            var data = _db.PARItems
                .Where(w => w.OrderItemId == orderItemId)
                .Select(s => new PARAcknowledgementVM
                {
                    ParId = s.PAR.Id,
                    ParItemId = s.Id,
                    OrderId = s.OrderItem.OrderId,
                    OrderItemId = s.OrderItemId,
                    ParNo = s.PAR.ParNo,
                    ParDate = s.PAR.ParDate,
                    ReceivedBy = s.PAR.ReceivedBy,
                    ReceivedByPosition = s.PAR.ReceivedByPosition,
                    ReceivedDate = s.PAR.ReceivedDate,
                    IssuedBy = s.PAR.IssuedBy,
                    IssuedByPosition = s.PAR.IssuedByPosition,
                    IssuedDate = s.PAR.IssuedDate,
                    PostedBy = s.PAR.PostedBy,
                    PostedDt = s.PAR.PostedDt,
                    Unit = s.OrderItem.RequestItem.RisItem.Unit,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    DateAcquired = s.OrderItem.Order.PoDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    SerialNo = s.SerialNo
                })
                .AsQueryable();
            return data;
        }

        public async Task<PARAcknowledgementVM> GetAcknowledgedOrderItemByItemId(Guid? parItemId)
        {
            var data = await _db.PARItems
                .Where(w => w.Id == parItemId)
                .Select(s => new PARAcknowledgementVM
                {
                    ParId = s.PAR.Id,
                    ParItemId = s.Id,
                    OrderId = s.OrderItem.OrderId,
                    OrderItemId = s.OrderItemId,
                    ParNo = s.PAR.ParNo,
                    ParDate = s.PAR.ParDate,
                    ReceivedBy = s.PAR.ReceivedBy,
                    ReceivedByPosition = s.PAR.ReceivedByPosition,
                    ReceivedDate = s.PAR.ReceivedDate,
                    IssuedBy = s.PAR.IssuedBy,
                    IssuedByPosition = s.PAR.IssuedByPosition,
                    IssuedDate = s.PAR.IssuedDate,
                    PostedBy = s.PAR.PostedBy,
                    PostedDt = s.PAR.PostedDt,
                    Unit = s.OrderItem.RequestItem.RisItem.Unit,
                    PsNo = s.OrderItem.RequestItem.RisItem.PsNo,
                    DateAcquired = s.OrderItem.Order.PoDate,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    SerialNo = s.SerialNo
                })
                .FirstOrDefaultAsync();
            return data;
        }

        public async Task<PAR> GetByIdAsync(Guid parId)
        {
            return await _db.PARs.FindAsync(parId);
        }

        public async Task<PAR> GetByParNoAsync(string parNo)
        {
            return await _db.PARs.Where(w => w.ParNo == parNo).FirstOrDefaultAsync();
        }

        public async Task<int?> GetRemainingQty(Guid? orderItemId, Guid? parItemId)
        {
            var orderItem = await _db.OrderItems.Where(w => w.Id == orderItemId)
                .Select(s => new { Remaining = s.Qty - s.PARItems.Where(w => w.Id != parItemId).Sum(x => x.Qty) }).FirstOrDefaultAsync();
            if (orderItem == null)
            {
                return null;
            }
            return (int?)orderItem.Remaining;
        }

        public async Task<bool> IsAnyParNoAsync(Guid parId, string parNo)
        {
            return await _db.PARs.AnyAsync(a => a.Id != parId && a.ParNo == parNo);
        }
        public async Task<bool> IsPostedAsync(Guid parId)
        {
            var entity = await _db.PARs.FindAsync(parId);
            if (entity != null)
            {
                return !string.IsNullOrWhiteSpace(entity.PostedBy);
            }
            return false;
        }
        

        public async Task<PAR_VM> CreateAsync(PAR_VM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.ParNo))
            {
                model.PoNo = NextParNo((DateTime)model.ParDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PAR()
            {
                Id = model.Id,
                OrderId = model.OrderId,
                ParNo = model.ParNo,
                ParDate = model.ParDate,
                ReceivedBy = model.ReceivedBy ?? "",
                ReceivedByPosition = model.ReceivedByPosition ?? "",
                ReceivedDate = model.ReceivedDate,
                IssuedBy = model.IssuedBy ?? "",
                IssuedByPosition = model.IssuedByPosition ?? "",
                IssuedDate = model.IssuedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };
            
            // include items during add, ORDER Items not yet in PAR Items
            var orderItems = await _db.OrderItems
                .Where(w => w.OrderId == model.OrderId && !w.PARItems.Any()).ToListAsync();
            foreach (var orderItem in orderItems)
            {
                var parItem = new PARItem()
                {
                    Id = Guid.NewGuid(),
                    ParId = entity.Id,
                    OrderItemId = orderItem.Id,
                    Qty = (int)orderItem.Qty,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                
                entity.PARItems.Add(parItem);
            }

            _db.PARs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<PAR_VM> UpdateAsync(PAR_VM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PARs.FindAsync(model.Id);

            // if there's a change of item
            if (entity.OrderId != model.OrderId)
            {
                var items = _db.PARItems.Where(w => w.ParId == model.Id);
                await items.ForEachAsync(f => {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await _db.SaveChangesAsync();

                _db.PARItems.RemoveRange(items);
                await _db.SaveChangesAsync();

                // include items during add, ORDER Items not yet in PAR Items
                var orderItems = await _db.OrderItems
                    .Where(w => w.OrderId == model.OrderId && !w.PARItems.Any()).ToListAsync();
                foreach (var orderItem in orderItems)
                {
                    var parItem = new PARItem()
                    {
                        Id = Guid.NewGuid(),
                        ParId = entity.Id,
                        OrderItemId = orderItem.Id,
                        Qty = (int)orderItem.Qty,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };
                    entity.PARItems.Add(parItem);
                }
            }

            entity.OrderId = model.OrderId;
            entity.ParNo = model.ParNo;
            entity.ParDate = model.ParDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByPosition = model.ReceivedByPosition ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByPosition = model.IssuedByPosition ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PARs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<PAR_VM> DeleteAsync(PAR_VM model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PARs.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PARs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PARs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task PostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await _db.PARs.FindAsync(orderId);
            entity.PostedBy = user;
            entity.PostedDt = date;

            _db.PARs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task UnpostAsync(Guid orderId, string user, DateTime date)
        {
            var entity = await _db.PARs.FindAsync(orderId);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PARs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
        }

        public async Task GeneratePAR(GenerateParVM model, string user, DateTime date)
        {
            var orderItem = await _db.OrderItems.FindAsync(model.OrderItemId);
            if (orderItem == null)
            {
                throw new RecordNotFoundException((Guid)model.OrderItemId);
            }

            if (_db.PARItems.Any(a => a.OrderItemId == model.OrderItemId))
            {
                throw new ParsAlreadyExistsException();
            }

            // generate par per qty
            for (var qty = 0; qty < orderItem.Qty; ++qty)
            {
                var par = new PAR()
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderItem.OrderId,
                    ParNo = NextParNo((DateTime)model.ParDate),
                    ParDate = model.ParDate,
                    ReceivedBy = "",
                    ReceivedByPosition = "",
                    ReceivedDate = null,
                    IssuedBy = "",
                    IssuedByPosition = "",
                    IssuedDate = null,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date,
                    PostedBy = "",
                    PostedDt = null
                };
                _db.PARs.Add(par);
                _db.Entry(par).State = EntityState.Added;
                await _db.SaveChangesAsync();

                var parItem = new PARItem()
                {
                    Id = Guid.NewGuid(),
                    ParId = par.Id,
                    OrderItemId = model.OrderItemId,
                    Qty = 1,
                    Amount = orderItem.UnitCost,
                    SerialNo = "",
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                _db.PARItems.Add(parItem);
                _db.Entry(parItem).State = EntityState.Added;
                await _db.SaveChangesAsync();
            }

            //var entity = await db.PARs.FindAsync(orderId);
            //entity.PostedBy = user;
            //entity.PostedDt = date;

            //db.PARs.Attach(entity);
            //db.Entry(entity).State = EntityState.Modified;
            //await db.SaveChangesAsync();
        }

        public async Task<PARAcknowledgementVM> CreateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date)
        {
            //model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.ParNo))
            {
                model.ParNo = NextParNo((DateTime)model.ParDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PAR()
            {
                Id = model.ParId,
                OrderId = model.OrderId,
                ParNo = model.ParNo,
                ParDate = model.ParDate,
                ReceivedBy = model.ReceivedBy ?? "",
                ReceivedByPosition = model.ReceivedByPosition ?? "",
                ReceivedDate = model.ReceivedDate,
                IssuedBy = model.IssuedBy ?? "",
                IssuedByPosition = model.IssuedByPosition ?? "",
                IssuedDate = model.IssuedDate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            var parItem = new PARItem()
            {
                Id = Guid.NewGuid(),
                ParId = entity.Id,
                OrderItemId = model.OrderItemId,
                Qty = model.Qty,
                Amount = model.Amount,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            entity.PARItems.Add(parItem);

            _db.PARs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }
        public async Task<PARAcknowledgementVM> UpdateAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PARs.Where(w => w.Id == model.ParId).FirstOrDefaultAsync();
            
            entity.ParNo = model.ParNo;
            entity.ParDate = model.ParDate;
            entity.ReceivedBy = model.ReceivedBy ?? "";
            entity.ReceivedByPosition = model.ReceivedByPosition ?? "";
            entity.ReceivedDate = model.ReceivedDate;
            entity.IssuedBy = model.IssuedBy ?? "";
            entity.IssuedByPosition = model.IssuedByPosition ?? "";
            entity.IssuedDate = model.IssuedDate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            var parItem = await _db.PARItems.FirstOrDefaultAsync(f => f.Id == model.ParItemId);
            if (parItem != null)
            {
                parItem.Qty = model.Qty;
                parItem.Amount = model.Amount;
                parItem.SerialNo = model.SerialNo ?? "";
                parItem.UpdatedBy = model.UpdatedBy;
                parItem.UpdatedDt = model.UpdatedDt;

                _db.PARItems.Attach(parItem);
                _db.Entry(parItem).State = EntityState.Modified;
            }

            _db.PARs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }
        public async Task<PARAcknowledgementVM> DeleteAcknowledgementAsync(PARAcknowledgementVM model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            PARItem entity = await _db.PARItems.FindAsync(model.ParItemId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PARItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PARItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            var par = await _db.PARs.Where(w => w.Id == model.ParId && !w.PARItems.Any()).FirstOrDefaultAsync();
            if (par != null)
            {
                par.UpdatedBy = model.UpdatedBy;
                par.UpdatedDt = model.UpdatedDt;

                _db.PARs.Attach(par);
                _db.Entry(par).State = EntityState.Modified;
                await _db.SaveChangesAsync();

                _db.PARs.Remove(par);
                _db.Entry(par).State = EntityState.Deleted;
                await _db.SaveChangesAsync();
            }

            return model;
        }

        private string NextParNo(DateTime parDate)
        {
            string yyyy = parDate.Year.ToString().Trim();
            string mm = parDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.PARs.Where(w => w.ParDate.Value.Year == parDate.Year).OrderByDescending(o => o.ParNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.ParNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
        
    }
}