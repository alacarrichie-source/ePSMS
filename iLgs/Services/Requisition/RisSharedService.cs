using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Requisition
{
    public interface IRisSharedService
    {
        bool IsPosted(Guid risId);
        bool IsPosted(RISs ris);
        bool IsPosted(RisItem risItem);
        bool IsPosted(RisItemUnitGroup risItemUnitGroup);
        bool IsPosted(RisItemUnitGroupDescription risItemunitGroupDescription);
        bool IsPosted(RisItemUnitGroupDescriptionItem risItemunitGroupDescriptionItem);

        Task<bool> IsPostedAsync(Guid risId);        
    }

    public class RisSharedService : IRisSharedService
    {
        private readonly AppManEntities _db;
        
        public RisSharedService(AppManEntities db)
        {
            _db = db;            
        }

        public async Task<bool> IsPostedAsync(Guid risId)
        {
            var entity = await _db.RISses.FindAsync(risId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(Guid risId)
        {
            var entity = _db.RISses.Find(risId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public bool IsPosted(RISs ris)
        {
            return IsPosted(ris.Id);
        }

        public bool IsPosted(RisItem risItem)
        {
            var risId = (Guid)risItem.RisId;
            return IsPosted(risId);
        }

        public bool IsPosted(RisItemUnitGroup risItemUnitGroup)
        {
            var risId = (Guid)risItemUnitGroup.RisId;
            return IsPosted(risId);
        }

        public bool IsPosted(RisItemUnitGroupDescription risItemUnitGroupDescription)
        {
            var risId = (Guid)_db.RisItemUnitGroups.Where(w => w.Id == risItemUnitGroupDescription.UnitGroupId).AsNoTracking().FirstOrDefault()?.RisId;
            return IsPosted(risId);
        }

        public bool IsPosted(RisItemUnitGroupDescriptionItem risItemUnitGroupDescriptionItem)
        {
            var risId = (Guid)_db.RisItemUnitGroups.Where(w => w.RisItemUnitGroupDescriptions.Any(a => a.Id == risItemUnitGroupDescriptionItem.UnitGroupDescriptionId)).AsNoTracking().FirstOrDefault()?.RisId;
            return IsPosted(risId);
        }        
    }
}