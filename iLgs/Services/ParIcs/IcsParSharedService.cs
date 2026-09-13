using iLgs.Exceptions;
using iLgs.Exceptions.Service;
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
    public interface IIcsParSharedService
    {
        ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date);
        ValueTask<IcsPar> UnPostAsync(string refNo, string refType, string user, DateTime date);

        void ValidateUpdates(string refNo, string refType);
        void ValidateIfPosted(IcsPar entity);
        void ValidateIfNotPosted(IcsPar entity);
        Task ValidateUploadAsync(Guid? icsParId, string parNo, string refType);
        Task<bool> IsWwithUploadAsync(Guid? icsParId);
    }

    public class IcsParSharedService : IIcsParSharedService
    {

        private static string GetSerialNo(PsCardItemExtn extn)
        {
            if (extn == null) return null;
            var other = extn as PsCardItemExtnOther;
            if (other != null && !string.IsNullOrWhiteSpace(other.SerialNo)) return other.SerialNo;
            var vehicle = extn as PsCardItemExtnVehicle;
            if (vehicle != null) return !string.IsNullOrWhiteSpace(vehicle.PlateNo) ? vehicle.PlateNo : vehicle.ConductionNo;
            return extn.SeriesNo;
        }

        private readonly AppManEntities _db;

        public IcsParSharedService(AppManEntities db)
        {
            _db = db;
        }

        //public IcsParSharedService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //}

        public async ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date)
        {
            var entity = await _db.IcsPars.Where(w => w.RefNo == refNo && w.RefType == refType).SingleOrDefaultAsync();
            if (entity == null)
            {
                throw new NotFoundException(refNo);
            }

            ValidateIfPosted(entity); ;
            await ValidateUploadAsync(entity.Id, entity.RefNo, refType);

            // Re-validate per-bundle completeness before posting
            var icsParItems = await _db.IcsParItems
                .Include(i => i.PsCardItemExtn)
                .Where(w => w.IcsParId == entity.Id)
                .ToListAsync();

            foreach (var item in icsParItems)
            {
                if (item.PsCardItemExtn != null && item.PsCardItemExtn.PsCardItemId.HasValue)
                {
                    var cardItemId = item.PsCardItemExtn.PsCardItemId.Value;
                    var requiredSubItems = await _db.PsCardSubItems
                        .Where(s => s.PsCardItemId == cardItemId && s.IsRequiredForBundle == true)
                        .ToListAsync();

                    if (requiredSubItems.Any())
                    {
                        var itemComponents = await _db.IcsParItemComponents
                            .Where(c => c.IcsParItemId == item.Id)
                            .ToListAsync();

                        foreach (var reqSub in requiredSubItems)
                        {
                            var reqQty = reqSub.QtyPerParent ?? 1;
                            var allocatedQty = itemComponents
                                .Where(c => c.PsCardSubItemId == reqSub.Id)
                                .Sum(c => c.Qty);

                            if (allocatedQty < reqQty)
                            {
                                throw new InvalidValueException(string.Format("Cannot post: Bundle for item '{0}' is incomplete. Component '{1}' requires {2:G29} but only has {3:G29} allocated.", GetSerialNo(item.PsCardItemExtn) ?? item.PsCardItemExtn.PropNo ?? "Unit", reqSub.Description, reqQty, allocatedQty));
                            }
                        }
                    }
                }
            }

            entity.PostedBy = user;
            entity.PostedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        }

        public async ValueTask<IcsPar> UnPostAsync(string refNo, string refType, string user, DateTime date)
        {
            var entity = await _db.IcsPars.Where(w => w.RefNo == refNo && w.RefType == refType).SingleOrDefaultAsync();
            if (entity == null)
            {
                throw new NotFoundException(string.Format("Ref No. {0} does not exists.", refNo));
            }

            ValidateIfNotPosted(entity);
            ValidateUpdates(refNo, refType);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            await _db.SaveChangesAsync();

            return entity;
        }

        public void ValidateUpdates(string refNo, string refType)
        {
            var icsParUpdates = _db.IcsParUpdates.Include(i => i.IcsPar).Where(a => a.PrevRefNo == refNo && a.RefType == refType).ToList();
            if (icsParUpdates.Any())
            {
                var cancelledBy = string.Join("/", icsParUpdates.Select(s => s.IcsPar.RefNo));
                if (refType == "I")
                {
                    throw new RecordRelationshipException(string.Format("ICS No. {0} was already cancelled by ICS No. {1}, cannot proceed.", refNo, cancelledBy));
                }
                else
                {
                    throw new RecordRelationshipException(string.Format("PAR No. {0} was already cancelled by PAR No. {1}, cannot proceed.", refNo, cancelledBy));
                }
            }
        }

        public void ValidateIfPosted(IcsPar entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("Record was already posted by {0} on {1}, cannot proceed.", entity.PostedBy, entity.PostedDt));
            }
        }

        public void ValidateIfNotPosted(IcsPar entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException("Record is not yet posted, please verify.");
            }
        }

        public async Task ValidateUploadAsync(Guid? icsParId, string parNo, string refType)
        {
            if (!await IsWwithUploadAsync(icsParId))
            {
                throw new InvalidValueException("No uploaded files found, cannot post!");
            }
        }

        public async Task<bool> IsWwithUploadAsync(Guid? icsParId)
        {
            var result = await _db.Uploads.AnyAsync(a => a.ImageId == icsParId);
            return result;
        }
    }
}
