using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.ParIcs
{
    public interface IParIcsUploadService: IUploadService
    {        
    }

    public class ParIcsUploadService : UploadService, IParIcsUploadService
    {
        public ParIcsUploadService(AppManEntities db)
            : base(db, "PAR")
        {
        }

        //public ParIcsUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory) 
        //    : base(db, appManEntitiesFactory, "PAR")
        //{            
        //}

        private async Task<bool> IsPostedAsync(Guid? imageId)
        {
            var result = await _db.PsCardItems.Where(w => w.GroupId == imageId).AnyAsync(a => a.ParPostedBy != "" && a.ParPostedBy != null);
            return result;
        }

        public override async ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {
            //if (await IsPostedAsync(model.ImageId))
            //{
            //    throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            //}

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
            if (await IsPostedAsync(model.ImageId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            return await base.UpdateAsync(model, user, date);            
        }

        public override async ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date)
        {
            if (await IsPostedAsync(model.ImageId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            return await base.DeleteAsync(model, user, date);
            
        }        
    }
}