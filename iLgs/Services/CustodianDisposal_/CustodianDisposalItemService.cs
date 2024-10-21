using ClosedXML.Excel;
using iLgs.Controllers;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.DollarRate_;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.CustodianDisposal_
{
    public interface ICustodianDisposalItemService
    {
        IQueryable<CustodianDisposalItem> GetAllByCustodianDisposalId(Guid? custodianDisposalId);
        ValueTask<CustodianDisposalItem> GetByIdAsync(Guid? id);

        ValueTask<CustodianDisposalItem> SaveAsync(CustodianDisposalItem model, string user, DateTime date);
        ValueTask<CustodianDisposalItem> SaveBldgAsync(CustodianDisposalItem model, string user, DateTime date);
        ValueTask<CustodianDisposalItem> SaveLandAsync(CustodianDisposalItem model, string user, DateTime date);

        ValueTask ProcessForIirupAsync(Guid? custodianDisposalId, string user, DateTime date);
        ValueTask<CustodianDisposalItem> UpdateAsync(CustodianDisposalItem model, string user, DateTime date);
        ValueTask<CustodianDisposalItem> DeleteAsync(CustodianDisposalItem model, string user, DateTime date);
    }

    public class CustodianDisposalItemService : BaseValidator, ICustodianDisposalItemService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<CustodianDisposalItem> _exceptionService = new ExceptionService<CustodianDisposalItem>();        
        private readonly IDollarRateService _dollarRateService;
        private readonly GetDisplayNameDelegate _getDisplayName;

        public CustodianDisposalItemService(AppManEntities db)
        {
            _db = db;
            _dollarRateService = new DollarRateService(_db);
            _getDisplayName = propertyName => Utility.GetDisplayName<CustodianDisposalItem>(propertyName);
        }

        public ValueTask<CustodianDisposalItem> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.CustodianDisposalItems.Include(i => i.CustodianDisposal).Where(w => w.Id == id).SingleOrDefaultAsync();
            return data;
        });

        public IQueryable<CustodianDisposalItem> GetAllByCustodianDisposalId(Guid? custodianDisposalId) =>
        _exceptionService.TryCatch(() =>
        {
            var data = _db.CustodianDisposalItems.AsNoTracking()
                .Include(i => i.CustodianReportItem)
                .Include(i => i.CustodianDisposal.CustodianIirupItems)
                .Where(w => w.CustodianDisposalId == custodianDisposalId).AsQueryable();
            return data;
        });
        
        public ValueTask<CustodianDisposalItem> SaveAsync(CustodianDisposalItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            return await SaveAsync(model, user, date, 1);
        });

        public ValueTask<CustodianDisposalItem> SaveLandAsync(CustodianDisposalItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            return await SaveAsync(model, user, date, 2);
        });

        public ValueTask<CustodianDisposalItem> SaveBldgAsync(CustodianDisposalItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            return await SaveAsync(model, user, date, 3);
        });

        private async ValueTask<CustodianDisposalItem> SaveAsync(CustodianDisposalItem model, string user, DateTime date, int sw) 
        {
            var custodianDisposal = await _db.CustodianDisposals.FindAsync(model.CustodianDisposalId);
            ValidateIfPosted(custodianDisposal);

            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var gridItems = model.GridItems.Split(',');
            if (gridItems.Count() == 0)
            {
                throw new RecordNotFoundException(string.Format("No selected items, cannot generate."));
            }

            foreach(var gridItem in gridItems)
            {
                model.Id = Guid.NewGuid();
                if (sw == 1)
                {
                    model.CustodianReportItemId = Guid.Parse(gridItem);
                }
                else if (sw == 2)
                {
                    model.CustodianReportLandItemId = Guid.Parse(gridItem);
                }
                else
                {
                    model.CustodianReportBldgItemId = Guid.Parse(gridItem);
                }

                var entity = new CustodianDisposalItem();
                MapModelToEntityFields(entity, model, Mode.ADD);

                _db.CustodianDisposalItems.Add(entity);
                await _db.SaveChangesAsync();
            }                       

            return model;
        }

        public async ValueTask ProcessForIirupAsync(Guid? custodianDisposalId, string user, DateTime date)
        {            
            var custodianDisposalItems = await GetAllByCustodianDisposalId(custodianDisposalId).ToListAsync();
            foreach (var model in custodianDisposalItems)
            {
                model.ArticleDisplay = "Test";
                model.ArticleDisplay = model.CustodianReportItem.Article;
                model.EstimatedKgMetals = 0;
                model.EstimatedKgOthers = 0;
                model.ReplCost = 0;
                model.EstLife = 1;
                
                await UpdateAsync(model, user, date);
            }
        }
        
        public async ValueTask<CustodianDisposalItem> ComputeDisposalValueAsync(CustodianDisposalItem model)
        {
            var custodianIirup = await _db.CustodianIIRUPs.Where(w => w.CustodianIirupItems.Any(a => a.CustodianDisposalId == model.CustodianDisposalId)).FirstOrDefaultAsync();

            model.EstimatedCost = model.CustodianReportItem.UnitCost;
            model.SalvageValue = model.EstimatedCost * 0.1m;
            model.AcqYear = model.CustodianReportItem.PoDate.Value.Year;

            model.TotalUnits = model.CustodianReportItem.Qty ?? 1;
            model.FinalReplCost = model.CustodianReportItem.UnitCost == 0 ? model.ReplCost : model.CustodianReportItem.UnitCost;
            model.AppraisalRate = await _dollarRateService.GetRateAsync(custodianIirup.AsOf);
            model.AcquisitionRate = await _dollarRateService.GetRateAsync(model.CustodianReportItem.PoDate);
            model.CFF = Decimal.Round((Decimal)(model.AppraisalRate / model.AcquisitionRate), 3, MidpointRounding.AwayFromZero);
            model.CF = 0.1m;
            model.UF = 0.1m;
            model.AS_ = (int)2022 - model.AcqYear;
            model.L_AS = model.EstLife - model.AS_;
            model.R = model.L_AS < 0 ? 0 : model.AS_;
            model.R_L = model.R / model.EstLife;
            model.RUV = (model.EstimatedCost - model.SalvageValue) * model.R_L + model.SalvageValue;
            model.D = model.L_AS / model.EstLife;
            model.AF = GetAF(model.D);
            model.V1 = model.RUV * model.CF * model.CFF * model.TotalUnits;
            model.V2 = model.ReplCost * model.CF * model.UF * model.TotalUnits;
            model.V3 = model.ReplCost * model.AF * model.CFF * model.TotalUnits;
            model.V4 = ((model.EstimatedKgMetals * 10) + (model.EstimatedKgOthers * 5)) * model.TotalUnits;

            SetFinalDisposalValue(model);

            return model;
        }

        private void SetFinalDisposalValue(CustodianDisposalItem model)
        {
            var final = model.V1;
            var answer = 1;            
            if (model.V2 > final)
            {                
                final = model.V2;
                answer = 2;
            }
            if (model.V3 > final)
            {
                final = model.V3;
                answer = 3;
            }
            if (model.V4 > final)
            {
                final = model.V4;
                answer = 4;
            }
            model.FinalDisposalValue = final;
            model.Answer = answer;
        }

        private Decimal? GetAF(decimal? d)
        {
            decimal? result;
            if (d <= -1)
            {
                result = 0.1m;
            }
            else if (d <= -0.9m)
            {
                result = 0.118m;
            }
            else if (d <= -0.8m)
            {
                result = 0.136m;
            }
            else if (d <= -0.7m)
            {
                result = 0.155m;
            }
            else if (d <= -0.6m)
            {
                result = 0.173m;
            }
            else if (d <= -0.5m)
            {
                result = 0.191m;
            }
            else if (d <= -0.4m)
            {
                result = 0.209m;
            }
            else if (d <= -0.3m)
            {
                result = 0.227m;
            }
            else if (d <= -0.2m)
            {
                result = 0.245m;
            }
            else if (d <= -0.1m)
            {
                result = 0.264m;
            }
            else if (d < 0)
            {
                result = 0.282m;
            }
            else if (d == 0)
            {
                result = 0.3m;
            }
            else if (d < 0.5m)
            {
                result = 0.4m + d;
            }
            else if (d >= 0.5m)
            {
                result = 0.9m;
            }
            else
            {
                result = 0; // Default or fallback value
            }
            return result;
        }

        public ValueTask<CustodianDisposalItem> UpdateAsync(CustodianDisposalItem model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           ValidateIfNull(model);

           var entity = await _db.CustodianDisposalItems.FindAsync(model.Id);
           ValidateRecord(entity);
           ValidateFields(model);
                      
           model.UpdatedBy = user;
           model.UpdatedDt = date;

           await ComputeDisposalValueAsync(model);

           MapModelToEntityFields(entity, model, Mode.EDIT);

           _db.CustodianDisposalItems.Attach(entity);
           _db.Entry(entity).State = EntityState.Modified;
           await _db.SaveChangesAsync();
           return model;
       });

        public ValueTask<CustodianDisposalItem> DeleteAsync(CustodianDisposalItem model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            ValidateIfNull(model);

            var entity = await GetByIdAsync(model.Id);
            ValidateRecord(entity);
            ValidateIfPosted(entity);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.CustodianDisposalItems.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.CustodianDisposalItems.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(CustodianDisposalItem entity, CustodianDisposalItem model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }
            entity.CustodianDisposalId = model.CustodianDisposalId;
            entity.CustodianReportItemId = model.CustodianReportItemId;
            entity.CustodianReportLandItemId = model.CustodianReportLandItemId;
            entity.CustodianReportBldgItemId = model.CustodianReportBldgItemId;
            entity.ArticleFields = model.ArticleFields;
            entity.ArticleDisplay = model.ArticleDisplay;
            entity.EstimatedKgMetals = model.EstimatedKgMetals;
            entity.EstimatedKgOthers = model.EstimatedKgOthers;
            entity.ReplCost = model.ReplCost;
            entity.ShowAcqMonth = model.ShowAcqMonth;            
            entity.ShowAcqDay = model.ShowAcqDay;
            entity.EstimatedCost = model.EstimatedCost;
            entity.ServiceYear = model.ServiceYear;
            entity.AccDep = model.AccDep;
            entity.DisposalValue = model.DisposalValue;
            entity.ValueKg = model.ValueKg;
            entity.FinalDisposalValue = model.FinalDisposalValue;
            entity.SalvageValue = model.SalvageValue;
            entity.AcqYear = model.AcqYear;
            entity.EstLife = model.EstLife;
            entity.TotalUnits = model.TotalUnits;
            entity.FinalReplCost = model.FinalReplCost;
            entity.AppraisalRate = model.AppraisalRate;
            entity.AcquisitionRate = model.AcquisitionRate;
            entity.CFF = model.CFF;
            entity.CF = model.CF;
            entity.UF = model.UF;
            entity.AS_ = model.AS_;
            entity.L_AS = model.L_AS;
            entity.R = model.R;
            entity.R_L = model.R_L;
            entity.RUV = model.RUV;
            entity.D = model.D;
            entity.AF = model.AF;
            entity.V1 = model.V1;
            entity.V2 = model.V2;
            entity.V3 = model.V3;
            entity.V4 = model.V4;
            entity.Answer = model.Answer;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }

        private void ValidateFields(CustodianDisposalItem model)
        {            
            _imex.ThrowIfContainsErrors();
        }

        private void ValidateIfNull(CustodianDisposalItem model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateRecord(CustodianDisposalItem entity)
        {
            if (entity == null)
            {
                throw new NotFoundException(entity.Id);
            }
        }

        private void ValidateIfPosted(CustodianDisposalItem entity)
        {
            if (entity.CustodianDisposal.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.CustodianDisposal.PostedBy} on {entity.CustodianDisposal.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfPosted(CustodianDisposal entity)
        {
            if (entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private void ValidateIfNotPosted(CustodianDisposalItem entity)
        {
            if (entity.CustodianDisposal.PostedDt == null)
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }
        }
    }
}