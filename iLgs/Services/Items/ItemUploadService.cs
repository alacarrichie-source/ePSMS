using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Items
{
    public interface IItemUploadService : IUploadService
    {
    }

    public class ItemUploadService : UploadService, IItemUploadService
    {
        public ItemUploadService(AppManEntities db)
            : base(db, "ITEM")
        {

        }

        //public ItemUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
        //    : base(db, appManEntitiesFactory,  "ITEM")
        //{

        //}

        public override async ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {        
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
            return await base.UpdateAsync(model, user, date);
        }

        public override async ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date)
        {            
            return await base.DeleteAsync(model, user, date);

        }
    }
}