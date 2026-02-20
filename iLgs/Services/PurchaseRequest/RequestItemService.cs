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
using static iLgs.Models.Enums;

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

    internal class RequestItemService : BaseValidator, IRequestItemService
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
                ItemNoIndex = s.ItemNoIndex,
                Description = s.Description,
                Remarks = s.Remarks,
                Qty = s.Qty,
                Unit = s.Unit,
                UnitCost = s.UnitCost,
                TotalCost = s.TotalCost,
                PriceRate = s.PriceRate,
                PpmpCode = s.PpmpCode,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                Padding = (s.ItemNo.Length - s.ItemNo.Replace(".", "").Length) * 20
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

            if (string.IsNullOrWhiteSpace(model.ItemNo))
            {
                model.ItemNo = await NextItemNoAsync(model.PrId);
            }

            await ValidateFieldsAsync(model);
            await _requestSharedService.ValidateStatusAsync((Guid)model.PrId);

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;            

            if (await _db.RequestItems.AnyAsync(a => a.PrId == model.PrId && a.ItemNo == model.ItemNo))
            {
                _imex = new InvalidModelException();
                _imex.UpsertDataList(_getDisplayName(nameof(model.ItemNo)), $"Already Exits.");
                _imex.ThrowIfContainsErrors();
            }

            var entity = new RequestItem();
            MapModelToEntityFields(entity, model, Mode.ADD);

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
            await ValidateFieldsAsync(model);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();
            return model;
        });

        private void MapModelToEntityFields(RequestItem entity, RequestItemVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.PrId = model.PrId;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.ItemNo = model.ItemNo;
            entity.ItemNoIndex = Utility.GetItemNoIndex(model.ItemNo);
            entity.Description = model.Description?.Trim() ?? "";
            entity.Remarks = model.Remarks?.Trim() ?? "";
            entity.Qty = model.Qty;
            entity.Unit = model.Unit?.Trim() ?? "";
            entity.UnitCost = model.UnitCost;
            entity.TotalCost = model.TotalCost;
            entity.PriceRate = model.PriceRate;
            entity.PpmpCode = model.PpmpCode?.Trim();
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private async Task<string> NextItemNoAsync(Guid? prId)
        {
            var itemNos = await _db.RequestItems
                .Where(w => w.PrId == prId)
                .Select(i => i.ItemNo)
                .ToListAsync();

            if (!itemNos.Any())
                return "1";

            var maxItemNo = itemNos
                .OrderByDescending(x => x.Split('.')
                    .Select(n => int.Parse(n))
                    .ToArray(), new ItemNoSequenceUtil())
                .First();

            var parts = maxItemNo.Split('.');
            int lastIndex = parts.Length - 1;

            int lastNumber;
            if (int.TryParse(parts[lastIndex], out lastNumber))
            {
                parts[lastIndex] = (lastNumber + 1).ToString();
            }

            return string.Join(".", parts);
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

            if (!string.IsNullOrWhiteSpace(model.Unit))
            {
                if (!(await _db.Codextns.Where(w => w.CodeMast.Code == "UNIT" || w.CodeMast.Code == "UNIT-GROUP").AnyAsync()))
                {
                    _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid Value.");
                }                
            }

            //if (string.IsNullOrWhiteSpace(model.Description))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Description)), "Field is required.");
            //}

            //if (string.IsNullOrWhiteSpace(model.Unit))
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Field is required.");
            //}
            //else
            //{
            //    if (!(await _db.Codextns.Where(w => w.CodeMast.Code == "UNIT" || w.CodeMast.Code == "UNIT-GROUP").AnyAsync()))
            //    {
            //        _imex.UpsertDataList(_getDisplayName(nameof(model.Unit)), "Invalid Value.");
            //    }
            //    else
            //    {
            //        if (model.Unit == "Set" || model.Unit == "Lot") 
            //        {
            //            if (model.UnitCost == null || model.UnitCost == 0)
            //            {
            //                _imex.UpsertDataList(_getDisplayName(nameof(model.UnitCost)), "Field is required for Set/Lot items.");
            //            }                        )
            //        }
            //        else
            //        {
            //            if (string.IsNullOrWhiteSpace(model.Description))
            //            {
            //                _imex.UpsertDataList(_getDisplayName(nameof(model.UnitCost)), "Field is required for non Set/Lot items.");
            //            }
            //        }
            //    }
            //}

            //if (model.Qty == null || model.Qty == 0)
            //{
            //    _imex.UpsertDataList(_getDisplayName(nameof(model.Qty)), "Field is required.");
            //}

            _imex.ThrowIfContainsErrors();
        }
    }
}