using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnOtherService
    {
        IQueryable<PsCardItemExtnOtherVM> GetByPsCardItemId(Guid? psCardItemId);
        IQueryable<PsCardItemExtnOtherVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? trasferId);
        ValueTask<PsCardItemExtnOtherVM> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnOtherVM> CreateAsync(PsCardItemExtnOtherVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnOtherVM> UpdateAsync(PsCardItemExtnOtherVM model, string user, DateTime date);
        ValueTask<PsCardItemExtnOtherVM> DeleteAsync(PsCardItemExtnOtherVM model, string user, DateTime date);
    }

    public class PsCardItemExtnOtherService : IPsCardItemExtnOtherService
    {
        private readonly AppManEntities _db;
        private readonly IAppManEntitiesFactory _contextFactory;
        private readonly IExceptionService<PsCardItemExtnOtherVM> _exceptionService;
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnOtherValidator _psCardItemExtnOtherValidator;
        private readonly IPsCardItemExtnSharedService _psCardItemExtnSharedService;

        public PsCardItemExtnOtherService(AppManEntities db,
            IAppManEntitiesFactory appManEntitiesFactory,
            IExceptionService<PsCardItemExtnOtherVM> exceptionService,
            IPsCardItemTransactionService psCardItemTransactionService,
            IPsCardItemExtnOtherValidator psCardItemExtnOtherValidator,
            IPsCardItemExtnSharedService psCardItemExtnSharedService)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _exceptionService = exceptionService;
            _psCardItemTransactionService = psCardItemTransactionService;
            _psCardItemExtnOtherValidator = psCardItemExtnOtherValidator;
            _psCardItemExtnSharedService = psCardItemExtnSharedService;
        }

        private Expression<Func<PsCardItemExtnOther, PsCardItemExtnOtherVM>> GetProjection()
        {
            return s => new PsCardItemExtnOtherVM
            {
                Location = s.Codextn.Description,
                Id = s.Id,
                //PsCardItemExtnId = s.Id,
                PsCardItemId = s.PsCardItemId,
                AIRItemExtnId = s.AIRItemExtnId,
                SetLotNo = s.SetLotNo,
                SetLotQtyNo = s.SetLotQtyNo,
                ContentNo = s.ContentNo,
                CustItemNo = s.CustItemNo,
                IsAutoGen = s.IsAutoGen,
                LocationId = s.LocationId,
                PropNo = s.PropNo,
                PropYear = s.PropYear,
                PropSeq = s.PropSeq,
                SeriesNo = s.SeriesNo,
                Remarks = s.Remarks,
                Annex = s.Annex,
                OldAmount = s.OldAmount,
                OldPropNo = s.OldPropNo,
                UpcomingOfficer = s.UpcomingOfficer,
                SubLocation = s.SubLocation,
                Condition = s.Condition,
                AddCost = s.AddCost,
                AcqCost = s.AcqCost,
                AcqDate = s.AcqDate,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                // Extn
                SerialNo = s.SerialNo
            };
        }

        public IQueryable<PsCardItemExtnOtherVM> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId)
                .Select(GetProjection());
            return data;
        }

        public IQueryable<PsCardItemExtnOtherVM> GetByPsCardItemIdWithTransferId(Guid? psCardItemId, Guid? transferId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().AsNoTracking()
                .Where(w => w.PsCardItemId == psCardItemId && w.PsCardItemTransferItems.Any(a => a.PsCardItemTransferId == transferId))
                .Select(GetProjection());
            return data;
        }

        public ValueTask<PsCardItemExtnOtherVM> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().AsNoTracking()
                .Where(w => w.Id == id)
                .Select(GetProjection())
                .FirstOrDefaultAsync();
            return data;
        });

        static List<string> GenerateSeries(string startSeries, string endSeries)
        {
            List<string> result = new List<string>();

            // Regular expression to capture the numeric part at the end of the series
            string pattern = @"(.*?)(\d+)$";
            Match startMatch = Regex.Match(startSeries, pattern);
            Match endMatch = Regex.Match(endSeries, pattern);

            if (startMatch.Success && endMatch.Success)
            {
                // Extract the non-numeric part (the prefix) and numeric parts (the numbers)
                string prefix = startMatch.Groups[1].Value;  // e.g., 'AB-01-X-'
                int startNumber = int.Parse(startMatch.Groups[2].Value);  // Starting number
                int endNumber = int.Parse(endMatch.Groups[2].Value);  // Ending number

                // Determine the padding length based on the starting series
                int paddingLength = startMatch.Groups[2].Value.Length;

                // Loop through the range from start to end and generate the series
                for (int i = startNumber; i <= endNumber; i++)
                {
                    // Reassemble the series and maintain the original padding
                    result.Add($"{prefix}{i.ToString($"D{paddingLength}")}");
                }
            }

            return result;
        }

        public ValueTask<PsCardItemExtnOtherVM> CreateAsync(PsCardItemExtnOtherVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnOtherValidator.ValidateOnCreate(model);

            var psCardItem = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == model.PsCardItemId);
            var itemQty = (int)(psCardItem.Qty ?? 0); // + (int)(psCardItem.TransferIn ?? 0);
            var itemExtns = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId);
            var itemExtnCount = itemExtns.Count();

            if (itemExtnCount >= itemQty)
            {
                throw new InvalidValueException($"Cannot create more than {itemQty} record(s).");
            }

            if (itemExtns.Any(a => a.SerialNo == model.SerialNo))
            {
                throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = new PsCardItemExtnOther();
                MapModelToEntityFields(entity, model, Mode.ADD);

                ctx.PsCardItemExtns.Add(entity);
                await ctx.SaveChangesAsync();

                // Add Item to PsCardItemTransferItems
                var psCardItemTransferItem = new PsCardItemTransferItem()
                {
                    Id = Guid.NewGuid(),
                    PsCardItemTransferId = model.TransferId,
                    PsCardItemExtnId = model.Id,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };
                ctx.PsCardItemTransferItems.Add(psCardItemTransferItem);
                await ctx.SaveChangesAsync();
                await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);
            }

            return model;
        });

        public ValueTask<PsCardItemExtnOtherVM> UpdateAsync(PsCardItemExtnOtherVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnOtherValidator.ValidateOnUpdate(model);

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var itemExtn = await ctx.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.Id && w.SerialNo == model.SerialNo).FirstOrDefaultAsync();

                if (itemExtn != null)
                {
                    throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
                }

                model.UpdatedBy = user;
                model.UpdatedDt = date;

                var entity = await ctx.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);
                MapModelToEntityFields(entity, model, Mode.EDIT);

                //_db.PsCardItemExtns.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);
            }

            return model;
        });

        public void MapModelToEntityFields(PsCardItemExtnOther entity, PsCardItemExtnOtherVM model, Mode mode)
        {
            _psCardItemExtnSharedService.MapModelToEntityFields(entity, model, mode);
            
            // extn
            entity.SerialNo = model.SerialNo;            
        }

        public ValueTask<PsCardItemExtnOtherVM> DeleteAsync(PsCardItemExtnOtherVM model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnOtherValidator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var psCardItemTransferItem = await ctx.PsCardItemTransferItems.FirstOrDefaultAsync(f => f.PsCardItemExtnId == model.Id);
                if (psCardItemTransferItem != null)
                {
                    psCardItemTransferItem.UpdatedBy = model.UpdatedBy;
                    psCardItemTransferItem.UpdatedDt = model.UpdatedDt;

                    //_db.PsCardItemTransferItems.Attach(psCardItemTransferItem);
                    //_db.Entry(psCardItemTransferItem).State = EntityState.Modified;
                    await ctx.SaveChangesAsync();

                    ctx.PsCardItemTransferItems.Remove(psCardItemTransferItem);
                    //_db.Entry(psCardItemTransferItem).State = EntityState.Deleted;
                    await ctx.SaveChangesAsync();
                }

                var entity = await ctx.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //_db.PsCardItemExtns.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.PsCardItemExtns.Remove(entity);
                //_db.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();
            }

            return model;
        });


        private async ValueTask<bool> IsPostedAsync(Guid? PsCardItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == PsCardItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}