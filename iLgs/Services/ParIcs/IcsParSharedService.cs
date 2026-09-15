using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

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

        Task<string> NextParNoAsync(DateTime parDate);
        Task<string> NextIcsNoAsync(DateTime icsDate, decimal acqCost);
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
        private readonly ISemiExpendableService _semiExpendableService;

        public IcsParSharedService(AppManEntities db)
        {
            _db = db;
            _semiExpendableService = new SemiExpendableService(_db);
        }

        //public IcsParSharedService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //}

        public async ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date)
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var entity = await _db.IcsPars
                        .Where(w => w.RefNo == refNo && w.RefType == refType)
                        .SingleOrDefaultAsync();
                    if (entity == null)
                    {
                        throw new NotFoundException(refNo);
                    }

                    ValidateIfPosted(entity);
                    await ValidateUploadAsync(entity.Id, entity.RefNo, refType);

                    var icsParItems = await _db.IcsParItems
                        .Include(i => i.PsCardItemExtn)
                        .Include(i => i.IcsParItemComponents.Select(c => c.PsCardSubItem))
                        .Include(i => i.IcsParItemComponents.Select(c => c.PsCardItemExtn))
                        .Where(w => w.IcsParId == entity.Id)
                        .ToListAsync();

                    if (!icsParItems.Any())
                    {
                        throw new InvalidValueException("Cannot post an accountability document without accountable properties.");
                    }

                    if (string.Equals(entity.UpdateCode, "T", StringComparison.OrdinalIgnoreCase))
                    {
                        await ValidateTransferForPostingAsync(entity, icsParItems);
                    }

                    foreach (var item in icsParItems)
                    {
                        if (item.PsCardItemExtn != null && item.PsCardItemExtn.PsCardItemId.HasValue)
                        {
                            var cardItemId = item.PsCardItemExtn.PsCardItemId.Value;
                            var requiredSubItems = await _db.PsCardSubItems
                                .Where(s => s.PsCardItemId == cardItemId && s.IsRequiredForBundle == true)
                                .ToListAsync();

                            foreach (var reqSub in requiredSubItems)
                            {
                                var reqQty = reqSub.QtyPerParent ?? 1;
                                var allocatedQty = item.IcsParItemComponents
                                    .Where(c => c.PsCardSubItemId == reqSub.Id)
                                    .Sum(c => c.Qty);

                                if (allocatedQty < reqQty)
                                {
                                    throw new InvalidValueException(string.Format("Cannot post: Bundle for item '{0}' is incomplete. Component '{1}' requires {2:G29} but only has {3:G29} allocated.", GetSerialNo(item.PsCardItemExtn) ?? item.PsCardItemExtn.PropNo ?? "Unit", reqSub.Description, reqQty, allocatedQty));
                                }
                            }
                        }
                    }

                    entity.PostedBy = user;
                    entity.PostedDt = date;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    await _db.SaveChangesAsync();
                    transaction.Commit();

                    return entity;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async ValueTask<IcsPar> UnPostAsync(string refNo, string refType, string user, DateTime date)
        {
            using (var transaction = _db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var entity = await _db.IcsPars
                        .Include(i => i.IcsParItems)
                        .Where(w => w.RefNo == refNo && w.RefType == refType)
                        .SingleOrDefaultAsync();
                    if (entity == null)
                    {
                        throw new NotFoundException(string.Format("Ref No. {0} does not exist.", refNo));
                    }

                    ValidateIfNotPosted(entity);

                    var itemIds = entity.IcsParItems.Select(i => i.Id).ToList();
                    if (itemIds.Any() && await _db.IcsParItems.AnyAsync(i =>
                        i.PrevItemId.HasValue && itemIds.Contains(i.PrevItemId.Value) &&
                        i.IcsPar.PostedDt.HasValue))
                    {
                        throw new RecordRelationshipException("This transfer cannot be unposted because a later accountability transfer already exists.");
                    }

                    entity.PostedBy = null;
                    entity.PostedDt = null;
                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    await _db.SaveChangesAsync();
                    transaction.Commit();
                    return entity;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private async Task ValidateTransferForPostingAsync(IcsPar transfer, IList<IcsParItem> transferItems)
        {
            if (transfer.PostedDt.HasValue)
            {
                throw new InvalidValueException("This transfer is already posted.");
            }

            if (transferItems.Any(i => !i.PrevItemId.HasValue))
            {
                throw new InvalidValueException("Transfer source is no longer current.");
            }

            var sourceIds = transferItems.Select(i => i.PrevItemId.Value).Distinct().ToList();
            if (sourceIds.Count != transferItems.Count)
            {
                throw new InvalidValueException("One or more selected accountable properties are invalid.");
            }

            var sourceItems = await _db.IcsParItems
                .Include(i => i.IcsPar)
                .Include(i => i.PsCardItemExtn)
                .Include(i => i.IcsParItemComponents.Select(c => c.PsCardSubItem))
                .Include(i => i.IcsParItemComponents.Select(c => c.PsCardItemExtn))
                .Where(i => sourceIds.Contains(i.Id))
                .ToListAsync();

            if (sourceItems.Count != sourceIds.Count)
            {
                throw new InvalidValueException("Transfer source is no longer current.");
            }

            var sourceRefNos = sourceItems.Select(i => i.IcsPar == null ? null : i.IcsPar.RefNo).Distinct().ToList();
            if (sourceRefNos.Count != 1 || string.IsNullOrWhiteSpace(sourceRefNos[0]))
            {
                throw new InvalidValueException("Transfer source is no longer current.");
            }
            var sourceRefNo = sourceRefNos[0];
            if (!await _db.IcsParUpdates.AnyAsync(u => u.IcsParId == transfer.Id &&
                u.RefType == transfer.RefType && u.PrevRefNo == sourceRefNo))
            {
                throw new InvalidValueException("Transfer source is no longer current.");
            }

            var transferItemIds = transferItems.Select(i => i.Id).ToList();
            var successors = await _db.IcsParItems
                .Include(i => i.IcsPar)
                .Where(i => i.PrevItemId.HasValue && sourceIds.Contains(i.PrevItemId.Value) &&
                    !transferItemIds.Contains(i.Id))
                .ToListAsync();

            if (successors.Any(i => i.IcsPar != null && i.IcsPar.PostedDt.HasValue))
            {
                throw new InvalidValueException("This accountable property has already been transferred.");
            }
            if (successors.Any(i => i.IcsPar == null || !i.IcsPar.PostedDt.HasValue))
            {
                throw new InvalidValueException("A draft transfer already exists for this accountable property.");
            }

            foreach (var transferItem in transferItems)
            {
                var sourceItem = sourceItems.Single(i => i.Id == transferItem.PrevItemId.Value);
                if (sourceItem.IcsPar == null || !sourceItem.IcsPar.PostedDt.HasValue ||
                    sourceItem.IcsPar.RefType != transfer.RefType)
                {
                    throw new InvalidValueException("Transfer source is no longer current.");
                }
                if (!sourceItem.PsCardItemExtnId.HasValue || sourceItem.PsCardItemExtn == null ||
                    sourceItem.PsCardItemExtn.PsCardSubItemId.HasValue ||
                    transferItem.PsCardItemExtn == null || transferItem.PsCardItemExtn.PsCardSubItemId.HasValue ||
                    transferItem.PsCardItemExtnId != sourceItem.PsCardItemExtnId)
                {
                    throw new InvalidValueException("One or more selected accountable properties are invalid.");
                }
                if (!ComponentsMatch(sourceItem.IcsParItemComponents, transferItem.IcsParItemComponents))
                {
                    throw new InvalidValueException("One or more bundle components could not be transferred.");
                }

                foreach (var component in transferItem.IcsParItemComponents)
                {
                    if (component.PsCardSubItem == null ||
                        component.PsCardSubItem.PsCardItemId != transferItem.PsCardItemExtn.PsCardItemId ||
                        (component.PsCardItemExtnId.HasValue &&
                         (component.PsCardItemExtn == null || component.PsCardItemExtn.PsCardSubItemId != component.PsCardSubItemId)))
                    {
                        throw new InvalidValueException("One or more bundle components could not be transferred.");
                    }
                }
            }
        }

        private static bool ComponentsMatch(ICollection<IcsParItemComponent> source, ICollection<IcsParItemComponent> destination)
        {
            if (source == null || destination == null || source.Count != destination.Count)
            {
                return false;
            }

            foreach (var component in source)
            {
                var sourceCount = source.Count(c =>
                    c.PsCardSubItemId == component.PsCardSubItemId &&
                    c.PsCardItemExtnId == component.PsCardItemExtnId &&
                    c.Qty == component.Qty);
                var destinationCount = destination.Count(c =>
                    c.PsCardSubItemId == component.PsCardSubItemId &&
                    c.PsCardItemExtnId == component.PsCardItemExtnId &&
                    c.Qty == component.Qty);
                if (sourceCount != destinationCount)
                {
                    return false;
                }
            }
            return true;
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
            if (entity.PostedDt.HasValue || !string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("Record was already posted by {0} on {1}, cannot proceed.", entity.PostedBy, entity.PostedDt));
            }
        }

        public void ValidateIfNotPosted(IcsPar entity)
        {
            if (!entity.PostedDt.HasValue)
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

        public async Task<string> NextParNoAsync(DateTime parDate)
        {
            string yyyy = parDate.Year.ToString().Trim();
            string mm = parDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = await _db.IcsPars.Where(w => w.RefType == "P" && w.RefDate.Value.Year == parDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            if (data == null)
            {
                return keyName + "-" + "00001";
            }
            else
            {
                var sequence = (int.Parse(data.RefNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(5, '0');
            }
        }

        public async Task<string> NextIcsNoAsync(DateTime icsDate, decimal acqCost)
        {
            string icsType = "";
            string yyyy = icsDate.Year.ToString().Trim();
            string mm = icsDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            var SPHV = _semiExpendableService.GetSPHV(icsDate);

            if (acqCost < SPHV)
            {
                icsType = "SPLV";
            }
            else
            {
                icsType = "SPHV";
            }

            string keyName = icsType + "-" + yyyy + "-" + mm;

            var data = await _db.IcsPars.Where(w => w.RefType == "I" && w.RefNo.StartsWith(icsType) && w.RefDate.Value.Year == icsDate.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
            if (data == null)
            {
                return keyName + "-" + "00001";
            }
            else
            {
                var sequence = (int.Parse(data.RefNo.Split('-')[3]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(5, '0');
            }
        }
    }
}
