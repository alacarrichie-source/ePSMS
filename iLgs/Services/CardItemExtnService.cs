//using iLgs.Models;
//using iLgs.Services.Interfaces;
//using System;
//using System.Collections.Generic;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web;

//namespace iLgs.Services
//{
//    public interface ICardItemExtnService
//    {
//        IQueryable<CardItemExtnVM> GetAll();
//        IQueryable<CardItemExtnVM> GetBatchInfo(Guid? cardId, string psType);
//        ValueTask SaveAsync(Guid cardId, List<CardItemExtnVM> itemExtnList, string user, DateTime date);
//    }

//    public class CardItemExtnService : ICardItemExtnService
//    {
//        private readonly AppManEntities _db = new AppManEntities();
//        private readonly IExceptionService<CardItemExtnVM> _vmExceptionService = new ExceptionService<CardItemExtnVM>();

//        public CardItemExtnService(AppManEntities db)
//        {
//            _db = db;
//        }

//        public IQueryable<CardItemExtnVM> GetAll() =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.CardItemExtns
//                .Select(s => new CardItemExtnVM
//                {
//                    Id = s.Id,
//                    CardId = s.CardId,
//                    ItemNo = s.ItemNo,
//                    ItemKey = s.ItemKey,
//                    ItemValue = s.ItemValue,
//                    Sequence = s.Sequence
//                }).AsQueryable();
//            return data;
//        });

//        public IQueryable<CardItemExtnVM> GetBatchInfo(Guid? cardId, string psType) =>
//        _vmExceptionService.TryCatch(() =>
//        {
//            var data = _db.Database.SqlQuery<CardItemExtnVM>("Exec CardItemExtnService_GetBatchInfo {0}, {1}", cardId, psType).AsQueryable();
//            return data;
//        });

//        public ValueTask SaveAsync(Guid cardId, List<CardItemExtnVM> itemExtnList, string user, DateTime date) =>
//        _vmExceptionService.TryCatch(async () =>
//        {
//            // log updates
//            var existingtemExtns = _db.CardItemExtns.Where(w => w.CardId == cardId).ToList();
//            foreach (var itemExtn in existingtemExtns)
//            {
//                var entity = await _db.CardItemExtns.FindAsync(itemExtn.Id);
//                if (entity != null)
//                {
//                    entity.UpdatedBy = user;
//                    entity.UpdatedDt = date;
//                    _db.CardItemExtns.Attach(entity);
//                    _db.Entry(entity).State = EntityState.Modified;
//                    await _db.SaveChangesAsync();

//                    _db.CardItemExtns.Remove(entity);
//                    _db.Entry(entity).State = EntityState.Deleted;
//                    await _db.SaveChangesAsync();
//                }
//            }

//            foreach (var itemExtn in itemExtnList)
//            {
//                var entity = await _db.CardItemExtns.Where(w => w.CardId == cardId && w.ItemKey == itemExtn.ItemKey).FirstOrDefaultAsync();
//                if (entity == null)
//                {
//                    entity = new CardItemExtn()
//                    {
//                        Id = Guid.NewGuid(),
//                        CardId = cardId,
//                        ItemNo = itemExtn.ItemNo,
//                        ItemKey = itemExtn.ItemKey,
//                        ItemValue = itemExtn.ItemValue ?? "",
//                        Sequence = itemExtn.Sequence,
//                        InsertedBy = user,
//                        InsertedDt = date,
//                        UpdatedBy = user,
//                        UpdatedDt = date
//                    };

//                    _db.CardItemExtns.Add(entity);
//                }
//                else
//                {
//                    entity.ItemValue = itemExtn.ItemValue ?? "";
//                    entity.UpdatedBy = user;
//                    entity.UpdatedDt = date;

//                    _db.CardItemExtns.Attach(entity);
//                    _db.Entry(entity).State = EntityState.Modified;
//                }
//            }
//            await _db.SaveChangesAsync();
//        });
//    }
//}