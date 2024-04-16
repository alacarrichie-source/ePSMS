using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IPropertyCardService
    {
        IQueryable<PropertyCardVM> GetAll();
        IQueryable<PropertyCardVM> GetAllByItemCodeId(Guid? itemCodeId);
        ValueTask<PropertyCardVM> GetVmByIdAsync(Guid? id);
        ValueTask<PropertyCard> GetByIdAsync(Guid id);
        ValueTask<PropertyCard> GetByPropNoAsync(string propNo);
        ValueTask<bool> GetAnyPropNoAsync(Guid id, string propNo);
        ValueTask<PropertyCardVM> CreateAsync(PropertyCardVM model, string user, DateTime date);
        ValueTask<PropertyCardVM> UpdateAsync(PropertyCardVM model, string user, DateTime date);
        ValueTask<PropertyCardVM> DeleteAsync(PropertyCardVM model, string user, DateTime date);
    }

    public class PropertyCardService : IPropertyCardService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<PropertyCardVM> _vmExceptionService = new ExceptionService<PropertyCardVM>();
        private readonly IExceptionService<PropertyCard> _exceptionService = new ExceptionService<PropertyCard>();

        public PropertyCardService(AppManEntities db)
        {
            _db = db;
        }

        public IQueryable<PropertyCardVM> GetAll() => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PropertyCards
                .Select(s => new PropertyCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    Description = s.Description,
                    Fund = s.Fund,
                    InsertedDt = s.InsertedDt                    
                });
            return data;
        });

        public IQueryable<PropertyCardVM> GetAllByItemCodeId(Guid? itemCodeId) => _vmExceptionService.TryCatch(() =>
        {
            var data = _db.PropertyCards.Where(w => w.ItemCodeId == itemCodeId)
                .Select(s => new PropertyCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    Description = s.Description,
                    Fund = s.Fund,
                    InsertedDt = s.InsertedDt                   
                });
            return data;
        });

        public ValueTask<PropertyCardVM> GetVmByIdAsync(Guid? id) => _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.PropertyCards.Where(w => w.Id == id)
                .Select(s => new PropertyCardVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    Item = s.ItemCode.Description,
                    ItemCode = s.ItemCode.Code,
                    ItemType = s.ItemCode.ItemType.Description,
                    ItemTypeCode = s.ItemCode.ItemType.Code,
                    Description = s.Description,
                    Fund = s.Fund,
                    InsertedDt = s.InsertedDt                 
                }).FirstOrDefaultAsync();
            return data;
        });

        public ValueTask<PropertyCard> GetByIdAsync(Guid id) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PropertyCards.FindAsync(id);
        });

        public async ValueTask<bool> GetAnyPropNoAsync(Guid id, string propNo)
        {
            return await _db.PropertyCards.AnyAsync(a => a.Id != id && a.PropNo == propNo);
        }

        public ValueTask<PropertyCard> GetByPropNoAsync(string propNo) => _exceptionService.TryCatch(async () =>
        {
            return await _db.PropertyCards.Where(w => w.PropNo == propNo).FirstOrDefaultAsync();
        });

        public ValueTask<PropertyCardVM> CreateAsync(PropertyCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            if (model.ItemCodeId == null)
            {
                throw new InvalidValueException("PPE is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                throw new InvalidValueException("Fund is Required!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = new PropertyCard
            {
                Id = model.Id,
                ItemCodeId = model.ItemCodeId,
                Fund = model.Fund,
                Description = model.Description,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.PropertyCards.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PropertyCardVM> UpdateAsync(PropertyCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            var entity = _db.PropertyCards.Find(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (model.ItemCodeId == null)
            {
                throw new InvalidValueException("Item is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            if (string.IsNullOrWhiteSpace(model.Fund))
            {
                throw new InvalidValueException("Fund is Required!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.ItemCodeId = model.ItemCodeId;
            entity.Fund = model.Fund;
            entity.Description = model.Description;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PropertyCards.Attach(entity);
            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PropertyCardVM> DeleteAsync(PropertyCardVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PropertyCards.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PropertyCards.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PropertyCards.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        private string NextPropertyNo(string propNo)
        {
            string keyName = propNo;

            var data = _db.PropertyCards.Where(w => w.PropNo == propNo)
                .OrderByDescending(o => o.PropNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "001";
            }
            else
            {
                var sequence = (int.Parse(data.PropNo.Split('-')[1]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(3, '0');
            }
        }
    }
}