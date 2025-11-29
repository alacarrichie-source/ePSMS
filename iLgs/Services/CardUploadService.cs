using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services
{
    public interface ICardUploadService : IUploadService
    {
    }

    public class CardUploadService : UploadService, ICardUploadService
    {
        public CardUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
            : base(db, appManEntitiesFactory, "CARD")
        {
        }

        private void ValidateIfPosted(Guid? psCardItemId)
        {
            var entity = _db.PsCards.Where(w => w.PsCardItems.Any(a => a.Id == psCardItemId)).FirstOrDefault();
            if (entity != null && entity.PostedDt != null)
            {
                var msg = $"Record already posted by {entity.PostedBy} on {entity.PostedDt}, cannot update!";
                throw new RecordAlreadyPostedException(msg);
            }
        }

        private async ValueTask<bool> IsParPostedAsync(Guid? psCardItemId)
        {
            var result = await _db.PsCardItems.Where(w => _db.PsCardItems.Where(x => x.Id == psCardItemId && x.GroupId == w.GroupId)
                .Any(a => a.ParPostedBy != "" && a.ParPostedBy != null)).AnyAsync();
            return result;
        }

        public override async ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {
            ValidateIfPosted(model.PsCardItemId);
            if (await IsParPostedAsync(model.PsCardItemId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            if (files == null || !files.Any())
            {
                throw new RecordNotFoundException("No files to upload!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            return await base.UploadAsync(files, model, user, date);
        }

        public override async ValueTask<Upload> UpdateAsync(Upload model, string user, DateTime date)
        {
            ValidateIfPosted(model.PsCardItemId);
            if (await IsParPostedAsync(model.PsCardItemId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            return await base.UpdateAsync(model, user, date);
        }

        public override async ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date)
        {
            ValidateIfPosted(model.PsCardItemId);
            if (await IsParPostedAsync(model.PsCardItemId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            return await base.DeleteAsync(model, user, date);

        }
    }
}