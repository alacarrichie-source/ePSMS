using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Interfaces;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcs
{
    public interface IIcsParService
    {
        IQueryable<IcsParVM> GetAll();
        IQueryable<IcsParVM> GetAll(string refNo, string refType);
        IQueryable<IcsPar> GetAllPars(Guid? psCardItemGroupId);
        IQueryable<IcsPar> GetAllIcs(Guid? psCardItemGroupId);
        IQueryable<IcsParVM> GetAllByPropNo(string propNo, string refType);
        IQueryable<IcsParVM> GetAllIcsForUpdate(string propNo);
        IQueryable<IcsParVM> GetAllParsForUpdate(string propNo);
        ValueTask<IcsPar> CreateAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsPar> UpdateAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsPar> DeleteAsync(IcsPar model, string user, DateTime date);

        ValueTask<IcsParVM> TransferIcsPar(IcsParVM model, string user, DateTime date);

        ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date);
        ValueTask<IcsPar> UnPostAsync(string refNo, string refType, string user, DateTime date);
    }

    public class IcsParService : BaseValidator, IIcsParService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private readonly IExceptionService<IcsPar> _exceptionService = new ExceptionService<IcsPar>();
        private readonly IExceptionService<IcsParVM> _vmExceptionService = new ExceptionService<IcsParVM>();
        private readonly GetDisplayNameDelegate _getDisplayName;

        public IcsParService(AppManEntities db)
        {
            _db = db;
            _getDisplayName = propertyName => Utility.GetDisplayName<IcsParVM>(propertyName);
        }

        public IQueryable<IcsParVM> GetAll() =>
        _vmExceptionService.TryCatch(() =>
        {
            //var data = _db.IcsPars.AsNoTracking().Select(GetProjection()).AsQueryable();
            var data = _db.Database.SqlQuery<IcsParVM>("Exec IcsPars_GetAll").AsQueryable();
            return data;
        });

        public IQueryable<IcsParVM> GetAll(string refNo, string refType) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<IcsParVM>("Exec IcsPars_GetAll {0}, {1}", refNo, refType).AsQueryable();
            return data;
        });

        public IQueryable<IcsParVM> GetAllByPropNo(string propNo, string refType) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<IcsParVM>("Exec IcsPars_GetAllByPropNo {0}, {1}", propNo, refType).AsQueryable();
            return data;
        });

        public IQueryable<IcsPar> GetAllIcs(Guid? psCardItemGroupId)
        {
            return GetAllIcsPars(psCardItemGroupId, "I");
        }

        public IQueryable<IcsPar> GetAllPars(Guid? psCardItemGroupId)
        {
            return GetAllIcsPars(psCardItemGroupId, "P");
        }

        private IQueryable<IcsPar> GetAllIcsPars(Guid? psCardItemGroupId, string refType)
        {
            var data = _db.IcsPars.Where(w => w.RefType == refType
                && w.IcsParItems.Any(a => a.PsCardItemExtn.PsCardItem.Id == psCardItemGroupId)).AsNoTracking().AsQueryable();
            return data;
        }

        public IQueryable<IcsParVM> GetAllIcsForUpdate(string propNo)
        {
            return GetAllByPropNo(propNo, "I");
        }

        public IQueryable<IcsParVM> GetAllParsForUpdate(string propNo)
        {
            return GetAllByPropNo(propNo, "P");
        }

        private IQueryable<IcsPar> GetAllIcsPars(string propNo, string reftype)
        {
            var data = _db.IcsPars.Where(w => w.RefType == reftype && w.IcsParItems.Any(a => a.PsCardItemExtn.PropNo == propNo)).AsNoTracking();
            return data;
        }


        public ValueTask<IcsPar> CreateAsync(IcsPar model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new IcsPar();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.IcsPars.Add(entity);
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<IcsPar> UpdateAsync(IcsPar model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars.FindAsync(model.Id);
            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            ValidateIfPosted(entity);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            _db.IcsPars.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<IcsPar> DeleteAsync(IcsPar model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars
                .Include(i => i.IcsParItems)
                .Include(i => i.IcsParUpdates)
                .Include(i => i.IcsParUnitGroups)
                .FirstOrDefaultAsync(f => f.Id == model.Id);

            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            ValidateIfPosted(entity);
            ValidateUpdates(model.RefNo, model.RefType);

            foreach (var icsParItem in entity.IcsParItems)
            {
                var psCardItemExtn = _db.PsCardItemExtns.Where(w => w.Id == icsParItem.PsCardItemExtnId).FirstOrDefault();
                psCardItemExtn.LocationId = null;
                psCardItemExtn.PropNo = null;
                psCardItemExtn.PropSeq = null;
                psCardItemExtn.PropYear = null;
                psCardItemExtn.UpdatedBy = user;
                psCardItemExtn.UpdatedDt = date;
                _db.PsCardItemExtns.Attach(psCardItemExtn);
                _db.Entry(psCardItemExtn).State = EntityState.Modified;
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.IcsPars.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.IcsPars.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();
            return model;
        });

        public ValueTask<IcsParVM> TransferIcsPar(IcsParVM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (model == null)
            {
                throw new InvalidValueException("Model is null!");
            }

            if (model.LocationId == null || model.LocationId == Guid.Empty)
            {
                throw new InvalidValueException("Field Location is required!");
            }

            ValidateTransferFields(model);
            ValidateUpdates(model.RefNo, model.RefType);

            var prevIcsPar = await _db.IcsPars
                .Include(i => i.IcsParItems)
                .Include(i => i.IcsParUnitGroups)
                .FirstOrDefaultAsync(f => f.RefNo == model.PrevRefNo && f.RefType == model.RefType);

            if (prevIcsPar == null)
            {
                throw new NotFoundException($"ICS/PAR No. {model.PrevRefNo} does not exists, please verify.");
            }

            var refNo = await NextRefNoAsync(model.RefDate, model.RefType);
            model.RefNo = refNo;
            var icsPar = new IcsPar()
            {
                Id = Guid.NewGuid(),
                UpdateCode = "T",
                LocationId = model.LocationId,
                LocationCode = model.LocationCode,
                Location = model.Location,
                RefNo = refNo,
                RefDate = model.RefDate,
                RefType = model.RefType,
                ReceivedById = model.ReceivedById,
                ReceivedBy = model.ReceivedBy.Trim(),
                ReceivedByTitle = model.ReceivedByTitle?.Trim(),
                ReceivedByTitle2 = model.ReceivedByTitle2?.Trim(),
                ReceivedByPosition = model.ReceivedByPosition?.Trim(),
                ReceivedDate = model.ReceivedDate,
                ReceivedDept = model.ReceivedDept.Trim(),
                IssuedBy = model.IssuedBy.Trim(),
                IssuedByPosition = model.IssuedByPosition.Trim(),
                IssuedDate = model.IssuedDate,
                IssuedDept = model.IssuedDept.Trim(),                
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            var icsParUpdate = new IcsParUpdate()
            {
                Id = Guid.NewGuid(),
                IcsParId = icsPar.Id,
                RefType = model.RefType,
                PrevRefNo = model.PrevRefNo,
                Remarks = model.Remarks,
                InsertedBy = user,
                InsertedDt = date,
                UpdatedBy = user,
                UpdatedDt = date
            };

            icsPar.IcsParUpdates.Add(icsParUpdate);

            _db.IcsPars.Add(icsPar);
            _db.Entry(icsPar).State = EntityState.Added;
            await _db.SaveChangesAsync();

            var prevIcsParItems = prevIcsPar.IcsParItems.ToList();
            foreach(var prevIcsParItem in prevIcsParItems)
            {
                var icsParItem = new IcsParItem()
                {
                    Id = Guid.NewGuid(),                    
                    IcsParId = icsPar.Id,
                    PrevItemId = prevIcsParItem.Id,
                    PsCardItemExtnId = prevIcsParItem.PsCardItemExtnId,
                    Qty = prevIcsParItem.Qty,
                    Amount = prevIcsParItem.Amount,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                icsPar.IcsParItems.Add(icsParItem);
            }

            _db.IcsPars.Attach(icsPar);
            _db.Entry(icsPar).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            var prevUnitGroups = prevIcsPar.IcsParUnitGroups.ToList();
            foreach(var prevUnitGroup in prevUnitGroups)
            {
                var icsParUnitGroup = new IcsParUnitGroup()
                {
                    Id = Guid.NewGuid(),
                    IcsParId = icsPar.Id,
                    SetLotNo = prevUnitGroup.SetLotNo,
                    Qty = prevUnitGroup.Qty,
                    Unit = prevUnitGroup.Unit,
                    UnitCost = prevUnitGroup.UnitCost,
                    TotalCost = prevUnitGroup.TotalCost,
                    AddCost = prevUnitGroup.AddCost,
                    TUnitCost = prevUnitGroup.TUnitCost,
                    GTotalCost = prevUnitGroup.GTotalCost,
                    InsertedBy = user,
                    InsertedDt = date,
                    Updatedby = user,
                    UpdatedDt = date
                };
                var prevUnitGroupDescs = _db.IcsPartUnitGroupDescriptions
                    .Include(i => i.IcsParUnitGroupDescriptionItems)
                    .Where(w => w.UnitGroupId == prevUnitGroup.Id).ToList();
                foreach(var prevUnitGroupDesc in prevUnitGroupDescs)
                {
                    var icsParUnitGroupDesc = new IcsPartUnitGroupDescription()
                    {
                        Id = Guid.NewGuid(),
                        UnitGroupId = icsParUnitGroup.Id,
                        Description = prevUnitGroupDesc.Description,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    var prevUnitGroupDescItems = prevUnitGroupDesc.IcsParUnitGroupDescriptionItems.ToList();
                    foreach(var prevUnitGroupDescItem in prevUnitGroupDescItems)
                    {
                        var icsParItemId = icsPar.IcsParItems.FirstOrDefault(w => w.PrevItemId == prevUnitGroupDescItem.IcsParItemId)?.Id;
                        var icsParUnitGroupDescItem = new IcsParUnitGroupDescriptionItem()
                        {
                            Id = Guid.NewGuid(),
                            UnitGroupDescriptionId = icsParUnitGroupDesc.Id,
                            IcsParItemId = icsParItemId,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };
                        icsParUnitGroupDesc.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescItem);
                    }
                    icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDesc);
                }
                icsPar.IcsParUnitGroups.Add(icsParUnitGroup); 
            }

            _db.IcsPars.Attach(icsPar);
            _db.Entry(icsPar).State = EntityState.Modified;
            _db.SaveChanges();

            return model;
        });

        private void ValidateTransferFields(IcsParVM model)
        {
            if (string.IsNullOrWhiteSpace(model.ReceivedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.ReceivedBy)), "Field is required.");
            }

            if (string.IsNullOrWhiteSpace(model.IssuedBy))
            {
                _imex.UpsertDataList(_getDisplayName(nameof(model.IssuedBy)), "Field is required.");
            }

            _imex.ThrowIfContainsErrors();
        }

        private async ValueTask<string> NextRefNoAsync(DateTime? refDate, string refType)
        {
            string yyyy = refDate.Value.Year.ToString().Trim();
            string mm = refDate.Value.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = await _db.IcsPars.Where(w => w.RefType == refType && w.RefDate.Value.Year == refDate.Value.Year).OrderByDescending(o => o.RefNo).FirstOrDefaultAsync();
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


        public void MapModelToEntityFields(IcsPar entity, IcsPar model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.LocationId = model.LocationId;
            entity.LocationCode = model.LocationCode;
            entity.Location = model.Location;
            entity.RefNo = model.RefNo;
            entity.RefDate = model.RefDate;
            entity.RefType = model.RefType;
            entity.ReceivedByTitle = model.ReceivedByTitle;
            entity.ReceivedByTitle2 = model.ReceivedByTitle2;
            entity.ReceivedBy = model.ReceivedBy;
            entity.ReceivedByPosition = model.ReceivedByPosition;
            entity.ReceivedDate = model.ReceivedDate;
            entity.ReceivedDept = model.ReceivedDept;
            entity.IssuedBy = model.IssuedBy;
            entity.IssuedByPosition = model.IssuedByPosition;
            entity.IssuedDate = model.IssuedDate;
            entity.IssuedDept = model.IssuedDept;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
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

        public void ValidateUpdates(string refNo, string refType)
        {
            var icsParUpdates = _db.IcsParUpdates.Where(a => a.PrevRefNo == refNo && a.RefType == refType).ToList();
            if (icsParUpdates.Any())
            {
                var cancelledBy = string.Join("/", icsParUpdates.Select(s => s.PrevRefNo));
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

        public async ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date)
        {
            var entity = _db.IcsPars.Where(w => w.RefNo == refNo && w.RefType == refType).SingleOrDefault();
            if (entity == null)
            {
                throw new NotFoundException(refNo);
            }

            ValidateIfPosted(entity); ;
            await ValidateUploadAsync(entity.Id, entity.RefNo);

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

        private async Task<bool> IsWwithUploadAsync(Guid? icsParId)
        {
            var result = await _db.Uploads.AnyAsync(a => a.ImageId == icsParId);
            return result;
        }

        private async Task ValidateUploadAsync(Guid? icsParId, string parNo)
        {
            if (!await IsWwithUploadAsync(icsParId))
            {
                throw new InvalidValueException($"No uploaded files found PAR No. {parNo}, cannot post!");
            }
        }

    }
}