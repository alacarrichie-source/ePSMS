using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
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

        private readonly AppManEntities _db;

        public IcsParSharedService(AppManEntities db)
        {
            _db = db;
        }

        public async ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date)
        {
            var entity = _db.IcsPars.Where(w => w.RefNo == refNo && w.RefType == refType).SingleOrDefault();
            if (entity == null)
            {
                throw new NotFoundException(refNo);
            }

            ValidateIfPosted(entity); ;
            await ValidateUploadAsync(entity.Id, entity.RefNo, refType);

            entity.PostedBy = user;
            entity.PostedDt = date;

            _db.IcsPars.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return entity;
        }

        public async ValueTask<IcsPar> UnPostAsync(string refNo, string refType, string user, DateTime date)
        {
            var entity = _db.IcsPars.Where(w => w.RefNo == refNo && w.RefType == refType).SingleOrDefault();
            if (entity == null)
            {
                throw new NotFoundException($"Ref No. {refNo} does not exists.");
            }

            ValidateIfNotPosted(entity);
            ValidateUpdates(refNo, refType);

            entity.PostedBy = null;
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.IcsPars.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
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
                    throw new RecordRelationshipException($"ICS No. {refNo} was already cancelled by ICS No. {cancelledBy}, cannot proceed.");
                }
                else
                {
                    throw new RecordRelationshipException($"PAR No. {refNo} was already cancelled by PAR No. {cancelledBy}, cannot proceed.");
                }
            }
        }

        public void ValidateIfPosted(IcsPar entity)
        {
            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record was already posted by {entity.PostedBy} on {entity.PostedDt}, cannot proceed.");
            }
        }

        public void ValidateIfNotPosted(IcsPar entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record is not yet posted, please verify.");
            }
        }

        public async Task ValidateUploadAsync(Guid? icsParId, string parNo, string refType)
        {
            if (!await IsWwithUploadAsync(icsParId))
            {
                throw new InvalidValueException($"No uploaded files found, cannot post!");                
            }
        }

        public async Task<bool> IsWwithUploadAsync(Guid? icsParId)
        {
            var result = await _db.Uploads.AnyAsync(a => a.ImageId == icsParId);
            return result;
        }
    }
}