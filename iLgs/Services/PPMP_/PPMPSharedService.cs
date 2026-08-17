using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.PPMP_
{
    public interface IPPMPSharedService
    {
        Task<bool> IsPostedAsync(PPMP ppmp);
        Task<bool> IsPostedAsync(PPMPItem ppmpItem);
        Task<bool> IsPostedAsync(PPMPItemUsage ppmpItemUsage);
        Task<bool> IsPostedAsync(Guid ppmpId);
        //Task<bool> GetAnyRequestItemAsync(Guid id);
        //Task<bool> GetAnyPpmpItemAsync(Guid id);
        //Task<bool> GetAnyPsCardItemAsync(Guid id);
        Task ValidateStatusAsync(Guid ppmpId);
    }

    public class PPMPSharedService : IPPMPSharedService
    {
        private readonly AppManEntities _db;

        public PPMPSharedService(AppManEntities db)
        {
            _db = db;
        }

        public async Task<bool> IsPostedAsync(Guid ppmpId)
        {
            var entity = await _db.PPMPs.FindAsync(ppmpId);
            return !string.IsNullOrWhiteSpace(entity.PostedBy);
        }

        public Task<bool> IsPostedAsync(PPMP ppmp)
        {
            return IsPostedAsync(ppmp.Id);
        }

        public async Task<bool> IsPostedAsync(PPMPItem ppmpItem)
        {
            var ppmpId = (Guid)ppmpItem.PpmpId;
            return await IsPostedAsync(ppmpId);
        }

        public async Task<bool> IsPostedAsync(PPMPItemUsage ppmpItemUsage)
        {            
            var ppmpId = await _db.PPMPItemUsages.Where(w => w.Id == ppmpItemUsage.Id).Select(s => s.PPMPItem.PpmpId).FirstOrDefaultAsync();
            return await IsPostedAsync((Guid)ppmpId);
        }

        public async Task ValidateStatusAsync(Guid ppmpId)
        {
            if (await IsPostedAsync(ppmpId))
            {
                throw new RecordAlreadyPostedException("PPMP Number is already Posted. Cannot update.");
            }

            //if (await Getan(ppmpId))
            //{
            //    throw new RecordRelationshipException("PO Number already has an AIR. Cannot update.");
            //}

            //if (await GetAnyParsAsync(ppmpId))
            //{
            //    throw new RecordRelationshipException("PO Number already has PAR. Cannot update.");
            //}
        }

        //public async Task<bool> GetAnyRequestItemAsync(Guid id)
        //{
        //    return await _db.RequestItems.AnyAsync(a => a.PpmpId == id);
        //}

        //public async Task<bool> GetAnyParsAsync(Guid id)
        //{
        //    return await _db.PARs.AnyAsync(a => a.PpmpId == id);
        //}
    }
}