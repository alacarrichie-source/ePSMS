using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace iLgs.Services.PropertyCard
{
    public interface IPsCardItemExtnOtherService
    {
        IQueryable<PsCardItemExtnOther> GetByPsCardItemId(Guid? psCardItemId);
        ValueTask<PsCardItemExtnOther> GetByIdAsync(Guid? id);

        ValueTask<PsCardItemExtnOther> CreateAsync(PsCardItemExtnOther model, string user, DateTime date);
        ValueTask<PsCardItemExtnOther> UpdateAsync(PsCardItemExtnOther model, string user, DateTime date);
        ValueTask<PsCardItemExtnOther> DeleteAsync(PsCardItemExtnOther model, string user, DateTime date);
    }

    public class PsCardItemExtnOtherService : IPsCardItemExtnOtherService
    {
        private readonly AppManEntities _db;
        private readonly IExceptionService<PsCardItemExtnOther> _exceptionService = new ExceptionService<PsCardItemExtnOther>();
        private readonly IPsCardItemTransactionService _psCardItemTransactionService;
        private readonly IPsCardItemExtnValidator _psCardItemExtnValidator;

        public PsCardItemExtnOtherService(AppManEntities db)
        {
            _db = db;
            _psCardItemTransactionService = new PsCardItemTransactionService(_db);
            _psCardItemExtnValidator = new PsCardItemExtnValidator(_db);
        }

        public IQueryable<PsCardItemExtnOther> GetByPsCardItemId(Guid? psCardItemId)
        {
            var data = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == psCardItemId);
            return data;
        }

        public ValueTask<PsCardItemExtnOther> GetByIdAsync(Guid? id) => _exceptionService.TryCatch(async () =>
        {
            var data = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.Id == id).FirstOrDefaultAsync();
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

        public ValueTask<PsCardItemExtnOther> CreateAsync(PsCardItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            //if (await IsPostedAsync(model.PsCardItemId))
            //{
            //    throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            //}
            _psCardItemExtnValidator.ValidateOnCreate(model);

            var psCardItem = await _db.PsCardItems.FirstOrDefaultAsync(f => f.Id == model.PsCardItemId);
            var itemQty = (int)(psCardItem.Qty ?? 0) + (int)(psCardItem.TransferIn ?? 0);
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

            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            // Generate the series
            List<string> seriesList = GenerateSeries(model.BegSerial, model.EndSerial);

            // Output the series
            foreach (var series in seriesList)
            {
                model.Id = Guid.NewGuid();
                model.SerialNo = series;

                var entity = new PsCardItemExtnOther()
                {
                    Id = model.Id,
                    PsCardItemId = model.PsCardItemId,
                    ContentNo = model.ContentNo,
                    CustItemNo = model.CustItemNo,
                    SerialNo = model.SerialNo,
                    Condition = model.Condition,
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                _db.PsCardItemExtns.Add(entity);
                await _db.SaveChangesAsync();

                await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);

                if (++itemExtnCount >= itemQty)
                {
                    break;
                }
            }

            return model;
        });

        public ValueTask<PsCardItemExtnOther> DeleteAsync(PsCardItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnValidator.ValidateOnDelete(model);

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefault(f => f.Id == model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCardItemExtns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<PsCardItemExtnOther> UpdateAsync(PsCardItemExtnOther model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            _psCardItemExtnValidator.ValidateOnUpdate(model);
            //if (await IsPostedAsync(model.PsCardItemId))
            //{
            //    throw new RecordAlreadyPostedException("Record already posted, cannot update!");
            //}
            var itemExtn = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().Where(w => w.PsCardItemId == model.PsCardItemId && w.Id != model.Id).FirstOrDefaultAsync();

            if (itemExtn != null)
            {
                throw new RecordAlreadyExistsException($"Serial No. {model.SerialNo} already exists!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCardItemExtns.OfType<PsCardItemExtnOther>().FirstOrDefaultAsync(f => f.Id == model.Id);

            entity.ContentNo = model.ContentNo;
            entity.CustItemNo = model.CustItemNo;
            entity.SerialNo = model.SerialNo;
            entity.Condition = model.Condition;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCardItemExtns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            await _psCardItemTransactionService.LogUpdates(model.Id, model.PsCardItemId, "CARD", user, date);

            return model;
        });

        private async ValueTask<bool> IsPostedAsync(Guid? PsCardItemId)
        {
            var entity = await _db.AIRs.Where(w => w.AIRItems.Any(a => a.Id == PsCardItemId)).FirstOrDefaultAsync();
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }        
    }
}