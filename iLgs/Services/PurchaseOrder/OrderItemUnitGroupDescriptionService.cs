using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderItemUnitGroupDescriptionService
    {
        IQueryable<OrderItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId);
        ValueTask<OrderItemUnitGroupDescription> GetByIdAsync(Guid? id);
        ValueTask<OrderItemUnitGroupDescriptionVM> CreateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionVM> UpdateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date);
        ValueTask<OrderItemUnitGroupDescriptionVM> DeleteAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date);
    }

    public class OrderItemUnitGroupDescriptionService : IOrderItemUnitGroupDescriptionService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<OrderItemUnitGroupDescriptionVM> _vmExceptionService;
        private readonly IExceptionService<OrderItemUnitGroupDescription> _exceptionService;

        public OrderItemUnitGroupDescriptionService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<OrderItemUnitGroupDescriptionVM> vmExceptionService,
            IExceptionService<OrderItemUnitGroupDescription> exceptionService)
        {
            _db = db;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _exceptionService = exceptionService;
        }


        public ValueTask<OrderItemUnitGroupDescription> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.OrderItemUnitGroupDescriptions.FindAsync(id);
            return data;
        });

        public IQueryable<OrderItemUnitGroupDescriptionVM> GetByUnitGroupId(Guid? unitGroupId) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.OrderItemUnitGroupDescriptions.AsNoTracking().Where(w => w.OrderItemUnitGroupId == unitGroupId).AsNoTracking()
                .Select(s => new OrderItemUnitGroupDescriptionVM
                {
                    Id = s.Id,
                    OrderItemUnitGroupId = s.OrderItemUnitGroupId,
                    RequestItemUnitGroupDescriptionId = s.RequestItemUnitGroupDescriptionId,
                    //Description = s.RequestItemUnitGroupDescription.RisItemUnitGroupDescription.Description,                    
                    Description = s.Description,
                    InsertedDt = s.InsertedDt
                });
            return data;
        });

        public ValueTask<OrderItemUnitGroupDescriptionVM> CreateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new OrderItemUnitGroupDescription()
            {
                Id = model.Id,
                OrderItemUnitGroupId = model.OrderItemUnitGroupId,
                RequestItemUnitGroupDescriptionId = model.RequestItemUnitGroupDescriptionId,
                Description = model.Description,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.OrderItemUnitGroupDescriptions.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionVM> DeleteAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.OrderItemUnitGroupDescriptions.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<OrderItemUnitGroupDescriptionVM> UpdateAsync(OrderItemUnitGroupDescriptionVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.OrderItemUnitGroupDescriptions.FindAsync(model.Id);

            entity.OrderItemUnitGroupId = model.OrderItemUnitGroupId;
            entity.RequestItemUnitGroupDescriptionId = model.RequestItemUnitGroupDescriptionId;
            entity.Description = model.Description;
            entity.OtherParticulars = model.OtherParticulars;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.OrderItemUnitGroupDescriptions.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}