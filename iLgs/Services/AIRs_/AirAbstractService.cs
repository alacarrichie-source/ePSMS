using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.AIRs_
{
    public interface IAirAbstractService
    {
        bool IsPosted(Guid airId);
        bool IsPosted(AIR air);
        bool IsPosted(AIRItem airItem);
        bool IsPosted(AIRItemExtn airItemExtn);
        ValueTask<bool> IsPostedAsync(Guid airId);
    }

    public class AirAbstractService : IAirAbstractService
    {
        private readonly AppManEntities _db;

        public AirAbstractService(AppManEntities db)
        {
            _db = db;
        }

        public bool IsPosted(Guid airId)
        {
            var entity = _db.AIRs.Find(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(AIR air)
        {
            return IsPosted(air.Id);
        }

        public bool IsPosted(AIRItem airItem)
        {
            var airId = (Guid)airItem.AirId;
            return IsPosted(airId);
        }

        public bool IsPosted(AIRItemExtn airItemExtn)
        {
            var airId = (Guid)_db.AIRItemExtns
                .Include(i => i.AIRItem)
                .Where(w => w.AIRItemId == airItemExtn.AIRItemId)
                .AsNoTracking()
                .FirstOrDefault()?.AIRItem.AirId;
            return IsPosted(airId);
        }

        public async ValueTask<bool> IsPostedAsync(Guid airId)
        {
            var entity = await _db.AIRs.FindAsync(airId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }
    }
}