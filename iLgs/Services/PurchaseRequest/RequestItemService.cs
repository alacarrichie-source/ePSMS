using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestItemService
    {
        IQueryable<RequestItemVM> GetByPrId(Guid? prId);
        Task<RequestItemVM> GetVmByIdAsync(Guid? id);
        Task<RequestItem> GetByIdAsync(Guid? id);

        ValueTask<RequestItemVM> CreateAsync(RequestItemVM model, string user, DateTime date);
        ValueTask<RequestItemVM> UpdateAsync(RequestItemVM model, string user, DateTime date);
        ValueTask<RequestItemVM> DeleteAsync(RequestItemVM model, string user, DateTime date);
    }

    public class RequestItemService : BaseValidator, IRequestItemService
    {
        private readonly AppManEntities _db;
        private readonly IRequestSharedService _requestSharedService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IExceptionService<RequestItemVM> _vmExceptionService;

        public RequestItemService(AppManEntities db)
        {
            _db = db;
            _requestSharedService = new RequestSharedService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<RequestItemVM>(propertyName);
            _vmExceptionService = new ExceptionService<RequestItemVM>();
        }

        private Expression<Func<RequestItem, RequestItemVM>> Projection()
        {
            return s => new RequestItemVM
            {
                Id = s.Id,
                PrId = s.PrId,
                ItemNo = s.ItemNo,
                Description = s.Description,
                Qty = s.Qty,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                TotalCost = s.TotalCost,
                PriceRate = s.PriceRate,
                PpmpCode = s.PpmpCode,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                SetLotNo = s.RequestItemUnitGroupDescriptionItems.FirstOrDefault().RequestItemUnitGroupDescription.RequestItemUnitGroup.SetLotNo
            };
        }

        public IQueryable<RequestItemVM> GetByPrId(Guid? prId)
        {
            var data = _db.RequestItems.AsNoTracking().Where(w => w.PrId == prId)
                .Select(Projection());
            return data;
        }

        public async Task<RequestItemVM> GetVmByIdAsync(Guid? id)
        {
            var data = await _db.RequestItems.AsNoTracking().Where(w => w.Id == id)
                .Select(Projection()).FirstOrDefaultAsync();
            return data;
        }

        public async Task<RequestItem> GetByIdAsync(Guid? id)
        {
            var data = await _db.RequestItems.FindAsync(id);                
            return data;
        }

        public ValueTask<RequestItemVM> CreateAsync(RequestItemVM model, string user, DateTime date) => 
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            await ValidateFieldsAsync(model);
            await _requestSharedService.ValidateStatusAsync((Guid)model.PrId);

            model.Id = Guid.NewGuid(); 
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            if (string.IsNullOrWhiteSpace(model.ItemNo))
            {
                model.ItemNo = await NextItemNoAsync(model.PrId);
            }

            if (await _db.RequestItems.AnyAsync(a => a.PrId == model.PrId && a.ItemNo == model.ItemNo))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            RequestItem entity = new RequestItem()
            {
                Id = model.Id,
                PrId = model.PrId,
                Qty = model.Qty,
                UnitCost = model.UnitCost,
                TotalCost = model.TotalCost,
                PriceRate = model.PriceRate,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RequestItems.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemVM> DeleteAsync(RequestItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            RequestItem entity = await _db.RequestItems.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            await _requestSharedService.ValidateStatusAsync((Guid)entity.PrId);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RequestItems.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RequestItemVM> UpdateAsync(RequestItemVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            if (string.IsNullOrWhiteSpace(model.ItemNo))
            {
                model.ItemNo = await NextItemNoAsync(model.PrId);
            }

            if (await _db.RequestItems.AnyAsync(a => a.PrId == model.PrId && a.ItemNo == model.ItemNo && a.Id != model.Id))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            var entity = await _db.RequestItems.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            await _requestSharedService.ValidateStatusAsync((Guid)model.PrId);

            entity.Qty = model.Qty;
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.TotalCost;
            entity.PriceRate = model.PriceRate;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RequestItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;

            await _db.SaveChangesAsync();
            return model;
        });

        private async Task<string> NextItemNoAsync(Guid? prId)
        {
            var nextItemNo = await _db.RequestItems.Where(w => w.PrId == prId)
                    .Select(i => int.Parse(i.ItemNo))
                    .MaxAsync();
            return (nextItemNo + 1).ToString();
        }

        private void ValidateIfNull(RequestItemVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(RequestItem entity, Guid id)
        {
            if (entity == null)
            {
                throw new NotFoundException(id);
            }
        }

        private async Task ValidateFieldsAsync(RequestItemVM model)
        {
            _imex = new InvalidModelException();
            
            if (!string.IsNullOrWhiteSpace(model.ItemNo))
            {
                var regex = new Regex(@"^(0|[1-9]\d*)(\.(0|[1-9]\d*))*$");
                if (!regex.IsMatch(model.ItemNo))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), "Invalid Value.");
                }
            }            
            _imex.ThrowIfContainsErrors();
        }
    }
}