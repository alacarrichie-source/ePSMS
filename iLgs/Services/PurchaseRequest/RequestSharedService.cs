using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestSharedService
    {
        bool IsPosted(Guid prId);
        bool IsPosted(Request request);
        bool IsPosted(RequestItem requestItem);
        //bool IsPosted(RequestItemUnitGroup requestItemUnitGroup);
        //bool IsPosted(RequestItemUnitGroupDescription requestItemUnitGroupDescription);
        //bool IsPosted(RequestItemUnitGroupDescriptionItem requestItemUnitGroupDescriptionItem);
        Task<bool> IsPostedAsync(Guid prId);
        Task<bool> GetAnyOrderAsync(Guid id);
        //Task<bool> GetAnyParsAsync(Guid id);
        Task<bool> IsSubmittedAsync(Guid prId);
        Task ValidateStatusAsync(Guid prId);
    }

    public class RequestSharedService : IRequestSharedService
    {
        private readonly AppManEntities _db;

        public RequestSharedService(AppManEntities db)
        {
            _db = db;
        }

        public bool IsPosted(Guid prId)
        {
            var entity = _db.Requests.Find(prId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(Request request)
        {
            return IsPosted(request.Id);
        }

        public bool IsPosted(RequestItem requestItem)
        {
            var prId = (Guid)requestItem.PrId;
            return IsPosted(prId);
        }

        //public bool IsPosted(RequestItemUnitGroup requestItemUnitGroup)
        //{
        //    var prId = (Guid)requestItemUnitGroup.PrId;
        //    return IsPosted(prId);
        //}

        //public bool IsPosted(RequestItemUnitGroupDescription requestItemUnitGroupDescription)
        //{
        //    var prId = (Guid)_db.RequestItemUnitGroupDescriptions
        //        .Include(i => i.RequestItemUnitGroup)
        //        .Where(w => w.RequestItemUnitGroupId == requestItemUnitGroupDescription.RequestItemUnitGroupId)
        //        .AsNoTracking()
        //        .FirstOrDefault()?.RequestItemUnitGroup.PrId;
        //    return IsPosted(prId);
        //}

        //public bool IsPosted(RequestItemUnitGroupDescriptionItem requestItemUnitGroupDescriptionItem)
        //{
        //    var prId = (Guid)_db.RequestItemUnitGroupDescriptionItems
        //        .Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup)
        //        .Where(w => w.RequestItemUnitGroupDescriptionId == requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId)
        //        .AsNoTracking()
        //        .FirstOrDefault()?.RequestItemUnitGroupDescription.RequestItemUnitGroup.PrId;
        //    return IsPosted(prId);
        //}
        
        public async Task<bool> IsPostedAsync(Guid prId)
        {
            var result = await _db.Requests
                .Where(r => r.Id == prId)
                .Select(r => r.PostedBy)
                .FirstOrDefaultAsync();

            return !string.IsNullOrWhiteSpace(result);
        }


        public async Task<bool> IsSubmittedAsync(Guid prId)
        {
            var result = await _db.Requests
                .Where(r => r.Id == prId)
                .Select(r => r.SubmittedBy)
                .FirstOrDefaultAsync();

            return !string.IsNullOrWhiteSpace(result);
        }


        public async Task ValidateStatusAsync(Guid prId)
        {
            if (await IsPostedAsync(prId))
            {
                throw new RecordAlreadyPostedException("PR Number is already Posted. Cannot update.");
            }

            if (await GetAnyOrderAsync(prId))
            {
                throw new RecordRelationshipException("PR Number already has a Purchase Order. Cannot update.");
            }

            //if (await GetAnyParsAsync(prId))
            //{
            //    throw new RecordRelationshipException("PR Number already has PAR. Cannot update.");
            //}
        }

        public async Task<bool> GetAnyOrderAsync(Guid id)
        {
            //return await _db.Orders.AnyAsync(a => a.PrId == id);

            return await _db.OrderItemRequests.AnyAsync(a => a.RequestItem.PrId == id);
        }        
    }
}