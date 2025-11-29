using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseOrder
{
    public interface IOrderUploadService : IUploadService
    {
        IOrderUploadService Create(string type);
    }

    public class OrderUploadService : UploadService, IOrderUploadService
    {
        public OrderUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
            : base(db, appManEntitiesFactory, "ORDERS")
        {
        }

        // Constructor for other uploads like "REQUEST"
        public OrderUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory, string type)
            : base(db, appManEntitiesFactory, type)
        {
        }

        public IOrderUploadService Create(string type)
        {
            return new OrderUploadService(_db, _contextFactory, type);
        }

        private async Task<bool> IsPostedAsync(Guid? imageId)
        {
            var result = await _db.Orders.Where(w => w.Id == imageId && w.PostedDt != null).AnyAsync();
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