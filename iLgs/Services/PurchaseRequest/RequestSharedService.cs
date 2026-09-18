using iLgs.Ai.Services;
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
        Task ValidateStatusAsync(Guid prId, bool allowAdminEdit = false);
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


        public async Task ValidateStatusAsync(Guid prId, bool allowAdminEdit = false)
        {
            var historyStatus = await _db.DocumentStatusHistories
                .AsNoTracking()
                .Where(x => x.DocumentType == DocumentTypes.PurchaseRequest && x.DocumentId == prId)
                .OrderByDescending(x => x.ChangedDt)
                .ThenByDescending(x => x.Id)
                .Select(x => x.ToStatus)
                .FirstOrDefaultAsync();

            var isPosted = await IsPostedAsync(prId) ||
                string.Equals(historyStatus, PrStatuses.Posted, StringComparison.OrdinalIgnoreCase);

            if (isPosted)
            {
                throw new RecordAlreadyPostedException("This Purchase Request is already Posted. Cannot update.");
            }

            if (allowAdminEdit)
            {
                if (!string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(historyStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase) &&
                    !string.Equals(historyStatus, PrStatuses.Draft, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Admin edit is only permitted for Draft, Submitted, or Unposted Purchase Requests.");
                }
            }
            else
            {
                if (string.Equals(historyStatus, PrStatuses.Submitted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This Purchase Request is currently Submitted for review. Cannot update directly.");
                }

                if (string.Equals(historyStatus, PrStatuses.Unposted, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This Purchase Request is unposted. Return it for revision or edit via posting administration.");
                }

                if (string.Equals(historyStatus, PrStatuses.Returned, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This Purchase Request has been Returned for revision. Please use the Revise PR cart workflow.");
                }

                if (string.Equals(historyStatus, PrStatuses.Revising, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("This Purchase Request is currently being revised. Please use the Procurement cart workflow.");
                }
            }

            if (await GetAnyOrderAsync(prId))
            {
                throw new RecordRelationshipException("PR Number already has a Purchase Order. Cannot update.");
            }
        }

        public async Task<bool> GetAnyOrderAsync(Guid id)
        {
            //return await _db.Orders.AnyAsync(a => a.PrId == id);

            return await _db.OrderItemRequests.AnyAsync(a => a.RequestItem.PrId == id);
        }        
    }
}
