using FluentValidation;
using FluentValidation.Internal;
using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface IAirItemExtnVehicleService
    {
        IQueryable<AIRItemExtnVehicle> GetByAirItemId(Guid? airItemId);
        ValueTask<AIRItemExtnVehicle> GetByIdAsync(Guid? id);

        //ValueTask<AIRItemExtnVehicle> CreateAsync(AIRItemExtnVehicle model, string user, DateTime date);
        ValueTask<ServiceResult<AIRItemExtnVehicle>> CreateAsync(AIRItemExtnVehicle model, string user, DateTime date);
        ValueTask<ServiceResult<AIRItemExtnVehicle>> UpdateAsync(AIRItemExtnVehicle model, string user, DateTime date);
        ValueTask<AIRItemExtnVehicle> DeleteAsync(AIRItemExtnVehicle model, string user, DateTime date);

        ValueTask<bool> IsPostedAsync(Guid? airItemId);
        ValueTask<bool> IsUniquePlateNoAddAsync(Guid? airItemId, string plateNo);
        ValueTask<bool> IsUniquePlateNoUpdateAsync(Guid? id, Guid? airItemId, string plateNo);
        ValueTask<bool> IsValidItemQty(Guid? airItemId);
        void ValidateItemExtnVechiles(Guid? airItemId);
    }

    public class AirItemExtnVehicleService : IAirItemExtnVehicleService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IExceptionService<AIRItemExtnVehicle> _exceptionService = new ExceptionService<AIRItemExtnVehicle>();
        private readonly IValidationService validationService = new ValidationService();
        private readonly AirItemExtnVehicleValator _validator;

        public AirItemExtnVehicleService(AppManEntities db)
        {
            _db = db;
            _validator = new AirItemExtnVehicleValator(this);
        }

        public IQueryable<AIRItemExtnVehicle> GetByAirItemId(Guid? airItemId)
        {
            var data = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == airItemId);
            return data;
        }

        public ValueTask<AIRItemExtnVehicle> GetByIdAsync(Guid? id) => _exceptionService.TryCatchAsync(async () =>
        {
            var data = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.Id == id).FirstOrDefaultAsync();
            return data;
        });

        public void ValidateItemExtnVechiles(Guid? airItemId)
        {
            if (_db.AIRItems.Any(a => a.Id == airItemId && a.AIRItemExtns.OfType<AIRItemExtnVehicle>().Count() < a.Qty))
            {
                throw new InvalidValueException("Incomplete item quantitny contents detected.");
            }
        }        

        //private void ValidateFields(AIRItemExtnVehicle model)
        //{
        //    if (string.IsNullOrWhiteSpace(model.SeriesNo))
        //    {
        //        throw new InvalidValueException("Series Number is required!");
        //    }
        //    if (string.IsNullOrWhiteSpace(model.PlateNo))
        //    {
        //        throw new InvalidValueException("Plate Number is required!");
        //    }
        //}

        public async ValueTask<ServiceResult<AIRItemExtnVehicle>> CreateAsync(AIRItemExtnVehicle model, string user, DateTime date)
        {
            try
            {
                var context = new ValidationContext<AIRItemExtnVehicle>(model, new PropertyChain(),
                    new CombinedValidatorSelector(new[] { "Create" }));

                var validationResult = await _validator.ValidateAsync(context);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .ToDictionary(
                            e => Utility.GetDisplayName(typeof(AIRItemExtnVehicle), e.PropertyName), // Get display name if available
                            e => e.ErrorMessage
                        );
                    return ServiceResult<AIRItemExtnVehicle>.Failure(errors);
                }

                //var airItemQty = (int)_db.AIRItems.FirstOrDefault(f => f.Id == model.AIRItemId).Qty;
                //var airItemExtnCount = _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == model.AIRItemId).Count();

                //if (airItemQty == airItemExtnCount)
                //{
                //    throw new InvalidValueException($"Cannot create more than {airItemQty} record(s).");
                //}

                //validationService.ValidateEntity(model);

                model.Id = Guid.NewGuid();
                model.InsertedBy = user;
                model.UpdatedBy = user;
                model.InsertedDt = date;
                model.UpdatedDt = date;

                model.Id = Guid.NewGuid();

                var entity = new AIRItemExtnVehicle()
                {
                    Id = model.Id,
                    ContentNo = model.ContentNo,
                    CustItemNo = model.CustItemNo,
                    AIRItemId = model.AIRItemId,
                    SeriesNo = model.SeriesNo,
                    YearModel = model.YearModel,
                    PlateNo = model.PlateNo,
                    BodyNo = model.BodyNo,
                    EngineNo = model.EngineNo,
                    ChasisNo = model.ChasisNo,
                    Color = model.Color,
                    CRN = model.CRN,
                    CRDate = model.CRDate,
                    MVFileNo = model.MVFileNo,
                    OrNo = model.OrNo,
                    OrDate = model.OrDate,
                    NetWeight = model.NetWeight,
                    InsPolicyNo = model.InsPolicyNo,
                    SubLocation = model.SubLocation,
                    ConductionNo = model.ConductionNo,
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                _db.AIRItemExtns.Add(entity);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) // Catch any unexpected exceptions
            {
                // Convert the exception details to a dictionary
                var errorDetails = new Dictionary<string, string>
                {
                    { "ExceptionMessage", ex.Message },
                    { "StackTrace", ex.StackTrace },
                    { "Source", ex.Source }
                    // Add more details if necessary
                };

                return ServiceResult<AIRItemExtnVehicle>.Failure(errorDetails, HttpStatusCode.InternalServerError);
            }
            //return model;
            return ServiceResult<AIRItemExtnVehicle>.Success(model);
        }

        public ValueTask<AIRItemExtnVehicle> DeleteAsync(AIRItemExtnVehicle model, string user, DateTime date) => _exceptionService.TryCatchAsync(async () =>
        {
            if (await IsPostedAsync(model.AIRItemId))
            {
                throw new RecordAlreadyPostedException("Record already posted, cannot delete!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.AIRItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.AIRItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public async ValueTask<ServiceResult<AIRItemExtnVehicle>> UpdateAsync(AIRItemExtnVehicle model, string user, DateTime date)
        {
            try
            {
                var context = new ValidationContext<AIRItemExtnVehicle>(model, new PropertyChain(),
                    new CombinedValidatorSelector(new[] { "Update" }));

                var validationResult = await _validator.ValidateAsync(context);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .ToDictionary(
                            e => Utility.GetDisplayName(typeof(AIRItemExtnVehicle), e.PropertyName), // Get display name if available
                            e => e.ErrorMessage
                        );
                    return ServiceResult<AIRItemExtnVehicle>.Failure(errors);
                }

                model.UpdatedBy = user;
                model.UpdatedDt = date;

                var entity = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().FirstOrDefaultAsync(f => f.Id == model.Id);

                validationService.ValidateEntity(entity);

                entity.ContentNo = model.ContentNo;
                entity.CustItemNo = model.CustItemNo;
                entity.SeriesNo = model.SeriesNo;
                entity.YearModel = model.YearModel;
                entity.PlateNo = model.PlateNo;
                entity.BodyNo = model.BodyNo;
                entity.EngineNo = model.EngineNo;
                entity.ChasisNo = model.ChasisNo;
                entity.Color = model.Color;
                entity.CRN = model.CRN;
                entity.CRDate = model.CRDate;
                entity.MVFileNo = model.MVFileNo;
                entity.OrNo = model.OrNo;
                entity.OrDate = model.OrDate;
                entity.NetWeight = model.NetWeight;
                entity.InsPolicyNo = model.InsPolicyNo;
                entity.SubLocation = model.SubLocation;
                entity.ConductionNo = model.ConductionNo;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                _db.AIRItemExtns.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
                await _db.SaveChangesAsync();
            }
            catch (Exception ex) // Catch any unexpected exceptions
            {
                // Convert the exception details to a dictionary
                var errorDetails = new Dictionary<string, string>
                {
                    { "ExceptionMessage", ex.Message },
                    { "StackTrace", ex.StackTrace },
                    { "Source", ex.Source }
                    // Add more details if necessary
                };

                return ServiceResult<AIRItemExtnVehicle>.Failure(errorDetails, HttpStatusCode.InternalServerError);
            }
            //return model;
            return ServiceResult<AIRItemExtnVehicle>.Success(model);
        }

        public async ValueTask<bool> IsPostedAsync(Guid? airItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == airItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public async ValueTask<bool> IsUniquePlateNoAddAsync(Guid? airItemId, string plateNo)
        {
            return (!await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == airItemId && w.PlateNo == plateNo).AnyAsync());
        }

        public async ValueTask<bool> IsUniquePlateNoUpdateAsync(Guid? id, Guid? airItemId, string plateNo)
        {
            return (!await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.Id != id && w.AIRItemId == airItemId && w.PlateNo == plateNo).AnyAsync());
        }

        public async ValueTask<bool> IsValidItemQty(Guid? airItemId)
        {
            var airItemQty = (int)(await _db.AIRItems.FirstOrDefaultAsync(f => f.Id == airItemId)).Qty;
            var airItemExtnCount = await _db.AIRItemExtns.OfType<AIRItemExtnVehicle>().Where(w => w.AIRItemId == airItemId).CountAsync();

            return !(airItemExtnCount >= airItemQty);
        }
    }
}