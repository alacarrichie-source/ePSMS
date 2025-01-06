//using iLgs.Models;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Web;
//using System.Threading.Tasks;

//namespace iLgs.Services
//{
//    public class OrderItemExtnService : IOrderItemExtnService
//    {
//        private readonly AppManEntities _db;

//        public OrderItemExtnService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public IQueryable<OrderItemExtnVM> GetAll()
//        {
//            var data = _db.OrderItemExtns
//                .Select(s => new OrderItemExtnVM
//                {
//                    Id = s.Id,
//                    OrderItemId = s.OrderItemId,
//                    ItemNo = s.ItemNo,
//                    ItemKey = s.ItemKey,
//                    ItemValue = s.ItemValue
//                }).AsQueryable();
//            return data;
//        }

//        public IQueryable<OrderItemExtnVM> GetBatchInfo(string mode, Guid? requestItemId, Guid? orderItemId, string psType)
//        {
//            IQueryable<OrderItemExtnVM> orderItemExtns = null;
//            IQueryable<RequestItemExtnVM> requestItemExtns = null;
//            if (mode == "E")
//            {
//                orderItemExtns = _db.Database.SqlQuery<OrderItemExtnVM>("Exec OrderItemExtnService_GetBatchInfo {0}, {1}", orderItemId, psType).AsQueryable();
//            }
//            else
//            {
//                var orderItemExtnList = new List<OrderItemExtnVM>();
//                requestItemExtns = _db.Database.SqlQuery<RequestItemExtnVM>("Exec RequestItemExtnService_GetBatchInfo {0}, {1}", requestItemId, psType).AsQueryable();
//                foreach(var rix in requestItemExtns)
//                {
//                    OrderItemExtnVM orderItemExtn = new OrderItemExtnVM()
//                    {
//                        Id = rix.Id,
//                        OrderItemId = orderItemId,
//                        ItemNo = rix.ItemNo,
//                        ItemKey = rix.ItemKey,
//                        ItemValue = rix.ItemValue,
//                        InsertedBy = rix.InsertedBy,
//                        InsertedDt = rix.InsertedDt,
//                        UpdatedBy = rix.UpdatedBy,
//                        UpdatedDt = rix.UpdatedDt,
//                        Sequence = rix.Sequence
//                    };
//                    orderItemExtnList.Add(orderItemExtn);
//                }
//                orderItemExtns = orderItemExtnList.AsQueryable();
//            }
//            return orderItemExtns;
//        }

//        public IQueryable<OrderItemExtnVM> GetBatchInfo(Guid? orderItemId, string psType)
//        {            
//            var orderItemExtns = _db.Database.SqlQuery<OrderItemExtnVM>("Exec OrderItemExtnService_GetBatchInfo {0}, {1}", orderItemId, psType).AsQueryable();
//            return orderItemExtns;
//        }

//        public async Task SaveAsync(Guid orderItemId, List<OrderItemExtnVM> orderItemExtnList, string user, DateTime date)
//        {
//            // log updates
//            var existingOrderItemExtns = _db.OrderItemExtns.Where(w => w.OrderItemId == orderItemId).ToList();
//            foreach (var orderItemExtn in existingOrderItemExtns)
//            {
//                var entity = await _db.OrderItemExtns.FindAsync(orderItemExtn.Id);
//                if (entity != null)
//                {
//                    entity.UpdatedBy = user;
//                    entity.UpdatedDt = date;                    
//                    _db.OrderItemExtns.Attach(entity);
//                    _db.Entry(entity).State = EntityState.Modified;
//                    await _db.SaveChangesAsync();

//                    _db.OrderItemExtns.Remove(entity);
//                    _db.Entry(entity).State = EntityState.Deleted;
//                    await _db.SaveChangesAsync();
//                }
//            }

//            foreach (var orderItemExtn in orderItemExtnList)
//            {
//                var entity = await _db.OrderItemExtns.Where(w => w.OrderItemId == orderItemId && w.ItemKey == orderItemExtn.ItemKey).FirstOrDefaultAsync();
//                if (entity == null)
//                {
//                    entity = new iLgs.Models.OrderItemExtn()
//                    {
//                        Id = Guid.NewGuid(),
//                        OrderItemId = orderItemId,
//                        ItemNo = orderItemExtn.ItemNo,
//                        ItemKey = orderItemExtn.ItemKey,
//                        ItemValue = orderItemExtn.ItemValue ?? "",
//                        Sequence = orderItemExtn.Sequence,
//                        InsertedBy = user,
//                        InsertedDt = date,
//                        UpdatedBy = user,
//                        UpdatedDt = date
//                    };

//                    _db.OrderItemExtns.Add(entity);
//                }
//                else
//                {
//                    entity.ItemValue = orderItemExtn.ItemValue ?? "";
//                    entity.Sequence = orderItemExtn.Sequence;
//                    entity.UpdatedBy = user;
//                    entity.UpdatedDt = date;

//                    _db.OrderItemExtns.Attach(entity);
//                    _db.Entry(entity).State = EntityState.Modified;
//                }
//            }
//            await _db.SaveChangesAsync();
//        }

//        public Task UpdateBatchAsync(List<OrderItemExtnVM> orderExtnList, string user, DateTime date)
//        {
//            throw new NotImplementedException();
//        }        
//    }
//}