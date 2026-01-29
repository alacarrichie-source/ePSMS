using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Services.CustodianUploads
{
    public interface ICustodianIirupUploadService : IUploadService
    {
    }

    public class CustodianIirupUploadService : UploadService, ICustodianIirupUploadService
    {
        public CustodianIirupUploadService(AppManEntities db)
            : base(db, "CUSTODIAN")
        {
        }

        //public CustodianIirupUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
        //    : base(db, appManEntitiesFactory, "CUSTODIAN")
        //{
        //}

        private async Task<bool> IsPostedAsync(Guid? imageId)
        {
            var result = await _db.CustodianIirupItems.Where(w => w.Id == imageId).AnyAsync(a => a.CustodianIIRUP.PostedBy != "" && a.CustodianIIRUP.PostedBy != null);
            return result;
        }

        public override async ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {
            if (await IsPostedAsync(model.ImageId))
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