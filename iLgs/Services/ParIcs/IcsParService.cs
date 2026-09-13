using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using Mode = iLgs.Models.Enums.Mode;
using IcsValue = iLgs.Models.Enums.IcsValue;
using static iLgs.Models.Enums;

namespace iLgs.Services.ParIcs
{
    public interface IIcsParService : IIcsParSharedService
    {
        IQueryable<IcsParVM> GetAll();
        IQueryable<IcsParVM> GetAll(string refNo, string refType);
        IQueryable<IcsParVM> GetByPsCardItemExtnId(Guid? psCardItemExtnId);
        IQueryable<IcsPar> GetAllPars(Guid? psCardItemGroupId);
        IQueryable<IcsPar> GetAllIcs(Guid? psCardItemGroupId);
        IQueryable<IcsParVM> GetAllByPropNo(string propNo, string refType);
        IQueryable<IcsParVM> GetAllIcsForUpdate(string propNo);
        IQueryable<IcsParVM> GetAllParsForUpdate(string propNo);

        IQueryable<IcsParTransferItemVM> GetAllItemsForTransfer(string refNo, string refType);
        IQueryable<IcsParItemVM> GetAllItems(string refNo, string refType);

        ValueTask<IcsPar> CreateAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsPar> UpdateAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsPar> DeleteAsync(IcsPar model, string user, DateTime date);
        ValueTask<IcsParVM> DeleteUpdatesAsync(IcsParVM model, string user, DateTime date);

        ValueTask<IcsParVM> TransferIcsPar(IcsParVM model, string user, DateTime date);

        IIcsService IcsService { get; }
        IParService ParService { get; }
        IIcsParItemService IcsParItem { get; }
    }

    public class IcsParService : BaseValidator, IIcsParService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<IcsPar> _exceptionService;
        private readonly IExceptionService<IcsParVM> _vmExceptionService;
        private readonly GetDisplayNameDelegate _getDisplayName;
        private readonly IIcsService _icsService;
        private readonly IParService _parService;
        private readonly IIcsParItemService _icsParItemService;
        private readonly IIcsParSharedService _icsParSharedService;

        public IcsParService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<IcsPar>();
            _vmExceptionService = new ExceptionService<IcsParVM>();
            _getDisplayName = propertyName => Utility.GetDisplayName<IcsParVM>(propertyName);
            _icsService = new IcsService(_db);
            _parService = new ParService(_db);
            _icsParItemService = new IcsParItemService(_db);
            _icsParSharedService = new IcsParSharedService(_db);
        }

        //public IcsParService(AppManEntities db,
        //    IAppManEntitiesFactory appManEntitiesFactory,
        //    ICreateAndLogExceptions exceptions,
        //    IExceptionService<IcsPar> exceptionService,
        //    IExceptionService<IcsParVM> vmExceptionService,
        //    IIcsService icsService,
        //    IParService parService,
        //    IIcsParItemService icsParItemService,
        //    IIcsParSharedService icsParSharedService)
        //{
        //    _db = db;
        //    _contextFactory = appManEntitiesFactory;
        //    _exceptions = exceptions;
        //    _exceptionService = exceptionService;
        //    _vmExceptionService = vmExceptionService;
        //    _getDisplayName = propertyName => Utility.GetDisplayName<IcsParVM>(propertyName);
        //    _icsService = icsService;
        //    _parService = parService;
        //    _icsParItemService = icsParItemService;
        //    _icsParSharedService = icsParSharedService;
        //}

        public IIcsService IcsService { get { return _icsService; } }
        public IParService ParService { get { return _parService; } }
        public IIcsParItemService IcsParItem { get { return _icsParItemService; } }

        public IQueryable<IcsParVM> GetAll() 
        {
            return
        _vmExceptionService.TryCatch(() =>
        {
            //var data = _db.IcsPars.AsNoTracking().Select(GetProjection()).AsQueryable();
            var data = _db.Database.SqlQuery<IcsParVM>("Exec IcsPars_GetAll").AsQueryable();
            return data;
        });
        }

        public IQueryable<IcsParVM> GetAll(string refNo, string refType) 
        {
            return
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<IcsParVM>("Exec IcsPars_GetAll {0}, {1}", refNo, refType).AsQueryable();
            return data;
        });
        }

        public IQueryable<IcsParVM> GetAllByPropNo(string propNo, string refType) 
        {
            return
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.Database.SqlQuery<IcsParVM>("Exec IcsPars_GetAllByPropNo {0}, {1}", propNo, refType).AsQueryable();
            return data;
        });
        }

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


        public IQueryable<IcsParVM> GetByPsCardItemExtnId(Guid? psCardItemExtnId)
        {
            var data = _db.IcsParItems.Where(w => w.PsCardItemExtnId == psCardItemExtnId).AsNoTracking()
                .Select(s => new IcsParVM
                {
                    Id = s.Id,
                    UpdateCode = s.IcsPar.UpdateCode,
                    RefNo = s.IcsPar.RefNo,
                    RefDate = s.IcsPar.RefDate,
                    RefType = s.IcsPar.RefType,
                    LocationCode = s.IcsPar.LocationCode,
                    Location = s.IcsPar.Location,
                    ReceivedBy = s.IcsPar.ReceivedBy,
                    IssuedBy = s.IcsPar.IssuedBy,
                    IssuedTo = s.IssuedTo
                }).AsQueryable();
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

        public IQueryable<IcsParTransferItemVM> GetAllItemsForTransfer(string refNo, string refType)
        {
            var data = _db.Database.SqlQuery<IcsParTransferItemVM>("Exec IcsPars_GetItemsForTransfer {0}, {1}", refNo, refType).AsQueryable();
            return data;
        }

        public IQueryable<IcsParItemVM> GetAllItems(string refNo, string refType)
        {
            var data = _db.Database.SqlQuery<IcsParItemVM>("Exec IcsPars_GetItems {0}, {1}", refNo, refType).AsQueryable();
            return data;
        }


        public ValueTask<IcsPar> CreateAsync(IcsPar model, string user, DateTime date) 
        {
            return
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
        }

        public ValueTask<IcsPar> UpdateAsync(IcsPar model, string user, DateTime date) 
        {
            return
        _exceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars.FindAsync(model.Id);
            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            ValidateIfPosted(entity);

            MapModelToEntityFields(entity, model, Mode.EDIT);

            await _db.SaveChangesAsync();

            return model;
        });
        }

        public ValueTask<IcsParVM> DeleteUpdatesAsync(IcsParVM model, string user, DateTime date)
        {
            return
        _vmExceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars
                .Include(i => i.IcsParItems)
                .Include(i => i.IcsParUpdates)
                //.Include(i => i.IcsParUnitGroups)
                .FirstOrDefaultAsync(f => f.Id == model.Id);

            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            if (entity.UpdateCode == "N")
            {
                throw new RecordRelationshipException("This record was made via ICS/PAR module, cannot delete.");
            }

            ValidateIfPosted(entity);
            ValidateUpdates(model.RefNo, model.RefType);

            //foreach (var unitGroup in entity.IcsParUnitGroups)
            //{
            //    var unitGroupDescriptions = _db.IcsPartUnitGroupDescriptions.Include(i => i.IcsParUnitGroupDescriptionItems).Where(w => w.UnitGroupId == unitGroup.Id);
            //    _db.IcsPartUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
            //}

            // Don't update PsCardItemExtn
            //foreach (var icsParItem in entity.IcsParItems)
            //{
            //    var psCardItemExtn = _db.PsCardItemExtns.Where(w => w.Id == icsParItem.PsCardItemExtnId).FirstOrDefault();
            //    psCardItemExtn.LocationId = null;
            //    psCardItemExtn.PropNo = null;
            //    psCardItemExtn.PropSeq = null;
            //    psCardItemExtn.PropYear = null;
            //    psCardItemExtn.UpdatedBy = user;
            //    psCardItemExtn.UpdatedDt = date;
            //    _db.PsCardItemExtns.Attach(psCardItemExtn);
            //    _db.Entry(psCardItemExtn).State = EntityState.Modified;
            //}

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.IcsPars.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });
        }

        public ValueTask<IcsPar> DeleteAsync(IcsPar model, string user, DateTime date) 
        {
            return
        _exceptionService.TryCatch(async () =>
        {
            IcsPar entity = await _db.IcsPars
                .Include(i => i.IcsParItems)
                .Include(i => i.IcsParUpdates)
                //.Include(i => i.IcsParUnitGroups)
                .FirstOrDefaultAsync(f => f.Id == model.Id);

            if (entity == null)
            {
                throw new NotFoundException(model.Id);
            }

            ValidateIfPosted(entity);
            ValidateUpdates(model.RefNo, model.RefType);

            //foreach (var unitGroup in entity.IcsParUnitGroups)
            //{
            //    var unitGroupDescriptions = _db.IcsPartUnitGroupDescriptions.Include(i => i.IcsParUnitGroupDescriptionItems).Where(w => w.UnitGroupId == unitGroup.Id);
            //    _db.IcsPartUnitGroupDescriptions.RemoveRange(unitGroupDescriptions);
            //}

            var itemIds = entity.IcsParItems.Select(i => i.Id).ToList();
            if (itemIds.Any())
            {
                var components = await _db.IcsParItemComponents.Where(c => itemIds.Contains(c.IcsParItemId)).ToListAsync();
                if (components.Any())
                {
                    _db.IcsParItemComponents.RemoveRange(components);
                    await _db.SaveChangesAsync();
                }
            }

            foreach (var icsParItem in entity.IcsParItems)
            {
                var psCardItemExtn = await _db.PsCardItemExtns.Where(w => w.Id == icsParItem.PsCardItemExtnId).FirstOrDefaultAsync();
                if (psCardItemExtn != null)
                {
                    psCardItemExtn.LocationId = null;
                    psCardItemExtn.PropNo = null;
                    psCardItemExtn.PropSeq = null;
                    psCardItemExtn.PropYear = null;
                    psCardItemExtn.UpdatedBy = user;
                    psCardItemExtn.UpdatedDt = date;
                    await _db.SaveChangesAsync();
                }
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.IcsPars.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });
        }

        public ValueTask<IcsParVM> TransferIcsPar(IcsParVM model, string user, DateTime date) 
        {
            return
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

            if (model.SelectedIds == null)
            {
                throw new InvalidValueException("No selected items, cannot proceed.");
            }

            ValidateTransferFields(model);
            //ValidateUpdates(model.RefNo, model.RefType);

            var prevIcsPar = await _db.IcsPars
                .Include(i => i.IcsParItems)
                //.Include(i => i.IcsParUnitGroups)
                .FirstOrDefaultAsync(f => f.RefNo == model.PrevRefNo && f.RefType == model.RefType);

            if (prevIcsPar == null)
            {
                throw new NotFoundException(string.Format("ICS/PAR No. {0} does not exists, please verify.", model.PrevRefNo));
            }

            if (model.RefType == "P")
            {
                model = await TransferParAsync(prevIcsPar, model, user, date);
            }
            else
            {
                model = await TransferIcsAsync(prevIcsPar, model, user, date);
            }

            return model;
        });
        }

        private async ValueTask<IcsParVM> TransferParAsync(IcsPar prevIcsPar, IcsParVM model, string user, DateTime date)
        {
            var refNo = await _parService.NextRefNoAsync((DateTime)model.RefDate, model.RefType);
            model.Id = Guid.NewGuid();
            model.RefNo = refNo;
            var icsPar = new IcsPar()
            {
                Id = model.Id,
                UpdateCode = "T",
                LocationId = model.LocationId,
                LocationCode = model.LocationCode,
                Location = model.Location,
                RefNo = refNo,
                RefDate = model.RefDate,
                RefType = model.RefType,
                ReceivedById = model.ReceivedById,
                ReceivedBy = model.ReceivedBy?.Trim(),
                ReceivedByTitle = model.ReceivedByTitle?.Trim(),
                ReceivedByTitle2 = model.ReceivedByTitle2?.Trim(),
                ReceivedByPosition = model.ReceivedByPosition?.Trim(),
                ReceivedDate = model.ReceivedDate,
                ReceivedDept = model.ReceivedDept?.Trim(),
                IssuedBy = model.IssuedBy?.Trim(),
                IssuedByPosition = model.IssuedByPosition?.Trim(),
                IssuedDate = model.IssuedDate,
                IssuedDept = model.IssuedDept?.Trim(),
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
            await _db.SaveChangesAsync();

            var prevIcsParItems = prevIcsPar.IcsParItems;
            var selectedIds = model.SelectedIds.Split(',');

            foreach (var selectedId in selectedIds)
            {
                var gSelectedId = Guid.Parse(selectedId);
                var prevIcsParItem = prevIcsParItems.SingleOrDefault(f => f.Id == gSelectedId);
                var psCardItemExtn = await _db.PsCardItemExtns.FirstOrDefaultAsync(f => f.Id == prevIcsParItem.PsCardItemExtnId);
                var icsParItem = new IcsParItem()
                {
                    Id = Guid.NewGuid(),
                    IcsParId = icsPar.Id,
                    PrevItemId = prevIcsParItem.Id,
                    PsCardItemExtnId = prevIcsParItem.PsCardItemExtnId,
                    Qty = prevIcsParItem.Qty,
                    AddCost = psCardItemExtn.AddCost,
                    //Amount = prevIcsParItem.Amount,
                    Amount = psCardItemExtn.AcqCost,
                    IssuedTo = model.IssuedTo,
                    Designation = model.Designation,
                    InsertedBy = user,
                    InsertedDt = date,
                    UpdatedBy = user,
                    UpdatedDt = date
                };

                icsPar.IcsParItems.Add(icsParItem);
            }

            //_db.IcsPars.Attach(icsPar);
            //_db.Entry(icsPar).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            //var prevUnitGroups = await _db.IcsParUnitGroups.Include(i => i.IcsPartUnitGroupDescriptions)
            //    .Where(w => w.IcsParId == prevIcsPar.Id).AsNoTracking().ToListAsync();
            //foreach (var prevUnitGroup in prevUnitGroups)
            //{
            //    var icsParUnitGroup = new IcsParUnitGroup()
            //    {
            //        Id = Guid.NewGuid(),
            //        IcsParId = icsPar.Id,
            //        SetLotNo = prevUnitGroup.SetLotNo,
            //        Qty = prevUnitGroup.Qty,
            //        Unit = prevUnitGroup.Unit,
            //        UnitCost = prevUnitGroup.UnitCost,
            //        TotalCost = prevUnitGroup.TotalCost,
            //        AddCost = prevUnitGroup.AddCost,
            //        TUnitCost = prevUnitGroup.TUnitCost,
            //        GTotalCost = prevUnitGroup.GTotalCost,
            //        InsertedBy = user,
            //        InsertedDt = date,
            //        Updatedby = user,
            //        UpdatedDt = date
            //    };

            //    decimal? unitCost = 0;
            //    decimal? addCost = 0;
            //    decimal? gTotalCost = 0;

            //    foreach (var prevUnitGroupDescription in prevUnitGroup.IcsPartUnitGroupDescriptions)
            //    {
            //        var icsParUnitGroupDesc = new IcsPartUnitGroupDescription()
            //        {
            //            Id = Guid.NewGuid(),
            //            UnitGroupId = icsParUnitGroup.Id,
            //            Description = prevUnitGroupDescription.Description,
            //            InsertedBy = user,
            //            InsertedDt = date,
            //            UpdatedBy = user,
            //            UpdatedDt = date
            //        };

            //        var prevUnitGroupDescriptionItems = await _db.IcsParUnitGroupDescriptionItems
            //            .Include(i => i.IcsParItem.PsCardItemExtn.PsCardItem)
            //            .Where(w => w.UnitGroupDescriptionId == prevUnitGroupDescription.Id).AsNoTracking().ToListAsync();
            //        foreach (var prevUnitGroupDescriptionItem in prevUnitGroupDescriptionItems)
            //        {
            //            unitCost = unitCost + prevUnitGroupDescriptionItem.IcsParItem.PsCardItemExtn.PsCardItem.UnitCost;
            //            foreach (var selectedId in selectedIds)
            //            {
            //                var gSelectedId = Guid.Parse(selectedId);
            //                if (gSelectedId == prevUnitGroupDescriptionItem.IcsParItemId)
            //                {
            //                    var newIcsParItem = await _db.IcsParItems.Include(i => i.PsCardItemExtn).AsNoTracking().SingleOrDefaultAsync(s => s.PrevItemId == gSelectedId);
            //                    var icsParUnitGroupDescItem = new IcsParUnitGroupDescriptionItem()
            //                    {
            //                        Id = Guid.NewGuid(),
            //                        UnitGroupDescriptionId = icsParUnitGroupDesc.Id,
            //                        IcsParItemId = newIcsParItem.Id,
            //                        InsertedBy = user,
            //                        InsertedDt = date,
            //                        UpdatedBy = user,
            //                        UpdatedDt = date
            //                    };
            //                    icsParUnitGroupDesc.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescItem);
            //                    addCost = addCost + newIcsParItem.PsCardItemExtn.AddCost ?? 0;
            //                    gTotalCost = gTotalCost + newIcsParItem.PsCardItemExtn.AcqCost ?? 0;
            //                    break;
            //                }
            //            }
            //        }
            //        icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDesc);
            //    }

            //    icsParUnitGroup.UnitCost = unitCost;
            //    icsParUnitGroup.TotalCost = unitCost;
            //    icsParUnitGroup.AddCost = addCost;
            //    icsParUnitGroup.TUnitCost = unitCost + addCost;
            //    icsParUnitGroup.GTotalCost = gTotalCost;

            //    icsPar.IcsParUnitGroups.Add(icsParUnitGroup);
            //}

            await _db.SaveChangesAsync();

            return model;
        }

        //private async ValueTask<bool> IsCompleteGroupItemAsync(string[] selectedItemIds, string poNo)
        //{
        //    var psCardUnitGroupDescriptionItems = await _db.PsCardItemUnitGroupDescriptionItems
        //        .AsNoTracking()
        //        .Where(w => w.PsCardItemUnitGroupDescription.PsCardItemUnitGroup.PoNo == poNo).ToListAsync();
        //    foreach (var psCardUnitGroupDescriptionItem in psCardUnitGroupDescriptionItems)
        //    {
        //        // check each psCardUnitGroupDescriptionItem.PsCardItemId if in seelectedItemId
        //        var icsParItems = await _db.IcsParItems
        //            .AsNoTracking()
        //            .Where(w => w.PsCardItemExtn.PsCardItemId == psCardUnitGroupDescriptionItem.PsCardItemId)
        //            .ToListAsync();
        //        if (!icsParItems.Any())
        //        {
        //            return false;
        //        }

        //        // check if all Group Items in IcsParItems were selected
        //        foreach (var icsParItem in icsParItems)
        //        {
        //            if (!selectedItemIds.Any(a => a.Equals(icsParItem.Id.ToString())))
        //            {
        //                return false;
        //            }
        //        }
        //    }
        //    return true;
        //}


        /*
         * Transfer the ICS based on the original group value.
         */
        private async ValueTask<IcsParVM> TransferIcsAsync(IcsPar prevIcsPar, IcsParVM model, string user, DateTime date)
        {
            var prevIcsParItems = prevIcsPar.IcsParItems;
            var selectedItemIds = model.SelectedIds.Split(','); // items to transfer                        

            for (int icsValue = 1; icsValue <= 2; icsValue++)
            {
                var transferItemList = new List<IcsParItem>();
                foreach (var selectedItemId in selectedItemIds)
                {
                    var gSelectedItemId = Guid.Parse(selectedItemId);
                    IcsParItem item = null;

                    /*
                     * TO DO: Include Additional Cost, if any
                     */

                    if (icsValue == (int)IcsValue.SPLV)
                    {
                        /*
                         LV:
                            1. Item where prev ICS is SPLV                            
                         */
                        item = await _db.IcsParItems
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Id == gSelectedItemId && f.IcsPar.RefNo.StartsWith("SPLV"));

                        if (item != null)
                        {
                            transferItemList.Add(item);
                        }
                    }
                    else
                    {
                        /*                      
                         HV:
                            1. Item where prev ICS is SPHV

                        */
                        item = await _db.IcsParItems
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Id == gSelectedItemId && f.IcsPar.RefNo.StartsWith("SPHV"));

                        if (item != null)
                        {
                            transferItemList.Add(item);
                        }
                    }
                }

                if (transferItemList.Any())
                {
                    var refNo = await _icsService.NextRefNoAsync((DateTime)model.RefDate, model.RefType, (IcsValue)icsValue);
                    model.Id = Guid.NewGuid();
                    model.RefNo = refNo;
                    var icsPar = new IcsPar()
                    {
                        Id = model.Id,
                        UpdateCode = "T",
                        LocationId = model.LocationId,
                        LocationCode = model.LocationCode,
                        Location = model.Location,
                        RefNo = refNo,
                        RefDate = model.RefDate,
                        RefType = model.RefType,
                        ReceivedById = model.ReceivedById,
                        ReceivedBy = model.ReceivedBy?.Trim(),
                        ReceivedByTitle = model.ReceivedByTitle?.Trim(),
                        ReceivedByTitle2 = model.ReceivedByTitle2?.Trim(),
                        ReceivedByPosition = model.ReceivedByPosition?.Trim(),
                        ReceivedDate = model.ReceivedDate,
                        ReceivedDept = model.ReceivedDept?.Trim(),
                        IssuedBy = model.IssuedBy?.Trim(),
                        IssuedByPosition = model.IssuedByPosition?.Trim(),
                        IssuedDate = model.IssuedDate,
                        IssuedDept = model.IssuedDept?.Trim(),
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

                    foreach (var prevIcsParItem in transferItemList)
                    {
                        var psCardItemExtn = await _db.PsCardItemExtns.AsNoTracking().FirstOrDefaultAsync(f => f.Id == prevIcsParItem.PsCardItemExtnId);
                        var icsParItem = new IcsParItem()
                        {
                            Id = Guid.NewGuid(),
                            IcsParId = icsPar.Id,
                            PrevItemId = prevIcsParItem.Id,
                            PsCardItemExtnId = prevIcsParItem.PsCardItemExtnId,
                            Qty = prevIcsParItem.Qty,
                            AddCost = psCardItemExtn.AddCost,
                            Amount = psCardItemExtn.AcqCost,
                            //Amount = prevIcsParItem.Amount, // include additional cost in the future
                            IssuedTo = model.IssuedTo,
                            Designation = model.Designation,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        icsPar.IcsParItems.Add(icsParItem);
                    }

                    _db.IcsPars.Add(icsPar);
                    await _db.SaveChangesAsync();

                    //// Get generated icsParItems
                    //var icsParItems = _db.IcsParItems.Include(i => i.PsCardItemExtn)
                    //    .AsNoTracking().Where(w => w.IcsParId == icsPar.Id);

                    //// unit groups with icsParItems, use IcsParUnitGroup structure for Transfer, not the PsCardUnitGroup. Bec. PsCardUnitGroup is by Qty (whole), while icParUnitGroup is by Item                    
                    //var unitGroups = await _db.IcsParUnitGroups
                    //    .Include(i => i.IcsPartUnitGroupDescriptions)
                    //    .AsNoTracking()
                    //    .Where(w => w.IcsPartUnitGroupDescriptions
                    //        .Any(a => a.IcsParUnitGroupDescriptionItems
                    //            .Any(b => icsParItems.Any(c => c.PsCardItemExtn.PsCardItemId == b.IcsParItem.PsCardItemExtn.PsCardItemId)))).ToListAsync();

                    //foreach (var unitGroup in unitGroups)
                    //{
                    //    var icsParUnitGroup = new IcsParUnitGroup()
                    //    {
                    //        Id = Guid.NewGuid(),
                    //        IcsParId = icsPar.Id,
                    //        SetLotNo = unitGroup.SetLotNo,
                    //        Qty = 1,
                    //        Unit = unitGroup.Unit,
                    //        //UnitCost = unitGroup.UnitCost,
                    //        //TotalCost = unitGroup.TotalCost,
                    //        //AddCost = unitGroup.AddCost,
                    //        //TUnitCost = unitGroup.TUnitCost,
                    //        //GTotalCost = unitGroup.GTotalCost,
                    //        InsertedBy = user,
                    //        InsertedDt = date,
                    //        Updatedby = user,
                    //        UpdatedDt = date
                    //    };

                    //    // value can change if part of the icsParItem was transfered
                    //    decimal? unitCost = 0; // will be based on the total unit cost of all transfered items (as a group unit cost)
                    //                           //decimal? totalCost = 0;
                    //    decimal? addCost = 0;
                    //    decimal? gTotalCost = 0;

                    //    foreach (var unitGroupDescription in unitGroup.IcsPartUnitGroupDescriptions)
                    //    {
                    //        var icsParUnitGroupDesc = new IcsPartUnitGroupDescription()
                    //        {
                    //            Id = Guid.NewGuid(),
                    //            UnitGroupId = icsParUnitGroup.Id,
                    //            Description = unitGroupDescription.Description,
                    //            InsertedBy = user,
                    //            InsertedDt = date,
                    //            UpdatedBy = user,
                    //            UpdatedDt = date
                    //        };

                    //        var unitGroupDescriptionItems = await _db.IcsParUnitGroupDescriptionItems
                    //            .Include(i => i.IcsParItem.PsCardItemExtn.PsCardItem)
                    //            .AsNoTracking()
                    //            .Where(w => w.IcsPartUnitGroupDescription.IcsParUnitGroup.IcsParId == prevIcsPar.Id
                    //                && w.UnitGroupDescriptionId == unitGroupDescription.Id
                    //                && icsParItems.Any(a => a.PsCardItemExtn.Id == w.IcsParItem.PsCardItemExtnId)).ToListAsync();

                    //        foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems) // per itemId
                    //        {
                    //            unitCost = unitCost + unitGroupDescriptionItem.IcsParItem.PsCardItemExtn.PsCardItem.UnitCost;
                    //            foreach (var selectedItemId in selectedItemIds)
                    //            {
                    //                var gSelectedId = Guid.Parse(selectedItemId);
                    //                if (gSelectedId == unitGroupDescriptionItem.IcsParItemId)
                    //                {
                    //                    var newIcsParItem = _db.IcsParItems.Include(i => i.PsCardItemExtn).AsNoTracking().SingleOrDefault(s => s.PrevItemId == gSelectedId);
                    //                    var icsParUnitGroupDescItem = new IcsParUnitGroupDescriptionItem()
                    //                    {
                    //                        Id = Guid.NewGuid(),
                    //                        UnitGroupDescriptionId = icsParUnitGroupDesc.Id,
                    //                        IcsParItemId = newIcsParItem.Id,
                    //                        InsertedBy = user,
                    //                        InsertedDt = date,
                    //                        UpdatedBy = user,
                    //                        UpdatedDt = date
                    //                    };
                    //                    icsParUnitGroupDesc.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescItem);
                    //                    addCost = addCost + newIcsParItem.PsCardItemExtn.AddCost ?? 0;
                    //                    gTotalCost = gTotalCost + newIcsParItem.PsCardItemExtn.AcqCost ?? 0;
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //        icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDesc);
                    //    }

                    //    icsParUnitGroup.UnitCost = unitCost;
                    //    icsParUnitGroup.TotalCost = unitCost;
                    //    icsParUnitGroup.AddCost = addCost;
                    //    icsParUnitGroup.TUnitCost = unitCost + addCost;
                    //    icsParUnitGroup.GTotalCost = gTotalCost;

                    //    icsPar.IcsParUnitGroups.Add(icsParUnitGroup);
                    //}

                    await _db.SaveChangesAsync();
                }
            }

            return model;
        }

        /*
         * This will revalue the Items based on its new group value.
         * Ex. Prev Set is HV, but will only transfer a specific item with LV. Thus, the new ICS will be created as LV ICS.
         */
        private async ValueTask<IcsParVM> TransferIcsStrictAsync(IcsPar prevIcsPar, IcsParVM model, string user, DateTime date)
        {
            var prevIcsParItems = prevIcsPar.IcsParItems;
            var selectedItemIds = model.SelectedIds.Split(','); // items to transfer                        

            for (int icsValue = 1; icsValue <= 2; icsValue++)
            {
                var transferItemList = new List<IcsParItem>();
                foreach (var selectedItemId in selectedItemIds)
                {
                    var gSelectedItemId = Guid.Parse(selectedItemId);
                    IcsParItem individualItem = null;
                    IcsParItem groupItem1 = null;
                    IcsParItem groupItem2 = null;
                    /*
                     * TO DO: Include Additional Cost, if any
                     */

                    if (icsValue == (int)IcsValue.SPLV)
                    {
                        /*
                         LV:
                            1. Individual Items < 5K and not member of Set
                            2. Group Items Unit Value < 5K, no need to check if set is complete bec. each item is below the Set Unit Cost
                            3. Group Items Unit Value >= 5K and Item UV < 5k, where set is not complete.                            
                         */
                        individualItem = await _db.IcsParItems
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Id == gSelectedItemId && f.PsCardItemExtn.AcqCost < 5000
                                //&& !_db.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == f.PsCardItemExtn.PsCardItemId)
                                );

                        if (individualItem != null)
                        {
                            transferItemList.Add(individualItem);
                        }
                        //else
                        //{
                        //    groupItem1 = await _db.IcsParItems
                        //        .AsNoTracking()
                        //        .FirstOrDefaultAsync(f => f.Id == gSelectedItemId
                        //            && _db.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == f.PsCardItemExtn.PsCardItemId
                        //                && a.PsCardItemUnitGroupDescription.PsCardItemUnitGroup.UnitCost < 5000));

                        //    if (groupItem1 != null)
                        //    {
                        //        transferItemList.Add(groupItem1);
                        //    }
                        //    else
                        //    {
                        //        // check if member of HV, but set is not complete, assign also to LV.
                        //        groupItem2 = await _db.IcsParItems
                        //            .Include(i => i.PsCardItemExtn.PsCardItem)
                        //            .AsNoTracking()
                        //            .FirstOrDefaultAsync(f => f.Id == gSelectedItemId && f.PsCardItemExtn.AcqCost < 5000
                        //                && _db.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == f.PsCardItemExtn.PsCardItemId
                        //                    && a.PsCardItemUnitGroupDescription.PsCardItemUnitGroup.UnitCost >= 5000));

                        //        if (groupItem2 != null)
                        //        {
                        //            // check if all group items are in selectedIds
                        //            var poNo = groupItem2.PsCardItemExtn.PsCardItem.PoNo;
                        //            if (!string.IsNullOrWhiteSpace(poNo))
                        //            {
                        //                var isComplete = await IsCompleteGroupItemAsync(selectedItemIds, poNo);
                        //                if (!isComplete)
                        //                {
                        //                    transferItemList.Add(groupItem2);
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                    }
                    else
                    {
                        /*                      
                         HV:
                            1. Individual Item >= 5k and not member of Set
                            2. Group Items Unit Value >= 5k
                                a. where set is complete.
                                b. Set is not complete, but Item UV >= 5k

                        */
                        individualItem = await _db.IcsParItems
                            .AsNoTracking()
                            .FirstOrDefaultAsync(f => f.Id == gSelectedItemId && f.PsCardItemExtn.AcqCost >= 5000
                                    //&& !_db.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == f.PsCardItemExtn.PsCardItemId)
                                    );

                        if (individualItem != null)
                        {
                            transferItemList.Add(individualItem);
                        }
                        //else
                        //{
                        //    groupItem1 = await _db.IcsParItems
                        //        .Include(i => i.PsCardItemExtn.PsCardItem)
                        //        .AsNoTracking()
                        //        .FirstOrDefaultAsync(f => f.Id == gSelectedItemId
                        //            && _db.PsCardItemUnitGroupDescriptionItems.Any(a => a.PsCardItemId == f.PsCardItemExtn.PsCardItemId
                        //                && a.PsCardItemUnitGroupDescription.PsCardItemUnitGroup.UnitCost >= 5000));

                        //    // check if group item is still complete
                        //    if (groupItem1 != null)
                        //    {
                        //        // check if all group items are in selectedIds
                        //        var poNo = groupItem1.PsCardItemExtn.PsCardItem.PoNo;
                        //        if (!string.IsNullOrWhiteSpace(poNo))
                        //        {
                        //            var isComplete = await IsCompleteGroupItemAsync(selectedItemIds, poNo);
                        //            if (isComplete)
                        //            {
                        //                transferItemList.Add(groupItem1);
                        //            }
                        //            else
                        //            {
                        //                if (groupItem1.PsCardItemExtn.AcqCost >= 5000)
                        //                {
                        //                    transferItemList.Add(groupItem1);
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                    }
                }

                if (transferItemList.Any())
                {
                    var refNo = await _icsService.NextRefNoAsync((DateTime)model.RefDate, model.RefType, (IcsValue)icsValue);
                    model.Id = Guid.NewGuid();
                    model.RefNo = refNo;
                    var icsPar = new IcsPar()
                    {
                        Id = model.Id,
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

                    foreach (var prevIcsParItem in transferItemList)
                    {
                        var icsParItem = new IcsParItem()
                        {
                            Id = Guid.NewGuid(),
                            IcsParId = icsPar.Id,
                            PrevItemId = prevIcsParItem.Id,
                            PsCardItemExtnId = prevIcsParItem.PsCardItemExtnId,
                            Qty = prevIcsParItem.Qty,
                            Amount = prevIcsParItem.Amount, // include additional cost in the future
                            IssuedTo = model.IssuedTo,
                            Designation = model.Designation,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        icsPar.IcsParItems.Add(icsParItem);
                    }

                    _db.IcsPars.Add(icsPar);
                    await _db.SaveChangesAsync();

                    //// Get generated icsParItems
                    //var icsParItems = _db.IcsParItems.Include(i => i.PsCardItemExtn)
                    //    .AsNoTracking().Where(w => w.IcsParId == icsPar.Id);

                    //// unit groups with icsParItems, use IcsParUnitGroup structure for Transfer, not the PsCardUnitGroup. Bec. PsCardUnitGroup is by Qty (whole), while icParUnitGroup is by Item                    
                    //var unitGroups = await _db.IcsParUnitGroups
                    //    .Include(i => i.IcsPartUnitGroupDescriptions)
                    //    .AsNoTracking()
                    //    .Where(w => w.IcsPartUnitGroupDescriptions
                    //        .Any(a => a.IcsParUnitGroupDescriptionItems
                    //            .Any(b => icsParItems.Any(c => c.PsCardItemExtn.PsCardItemId == b.IcsParItem.PsCardItemExtn.PsCardItemId)))).ToListAsync();

                    //foreach (var unitGroup in unitGroups)
                    //{
                    //    var icsParUnitGroup = new IcsParUnitGroup()
                    //    {
                    //        Id = Guid.NewGuid(),
                    //        IcsParId = icsPar.Id,
                    //        SetLotNo = unitGroup.SetLotNo,
                    //        Qty = 1,
                    //        Unit = unitGroup.Unit,
                    //        //UnitCost = unitGroup.UnitCost,
                    //        //TotalCost = unitGroup.TotalCost,
                    //        //AddCost = unitGroup.AddCost,
                    //        //TUnitCost = unitGroup.TUnitCost,
                    //        //GTotalCost = unitGroup.GTotalCost,
                    //        InsertedBy = user,
                    //        InsertedDt = date,
                    //        Updatedby = user,
                    //        UpdatedDt = date
                    //    };

                    //    // value can change if part of the icsParItem was transfered
                    //    decimal? unitCost = 0; // will be based on the total unit cost of all transfered items (as a group unit cost)
                    //    decimal? totalCost = 0;
                    //    decimal? addCost = 0;
                    //    decimal? tUnitCost = 0;
                    //    decimal? gTotalCost = 0;

                    //    foreach (var unitGroupDescription in unitGroup.IcsPartUnitGroupDescriptions)
                    //    {
                    //        var icsParUnitGroupDesc = new IcsPartUnitGroupDescription()
                    //        {
                    //            Id = Guid.NewGuid(),
                    //            UnitGroupId = icsParUnitGroup.Id,
                    //            Description = unitGroupDescription.Description,
                    //            InsertedBy = user,
                    //            InsertedDt = date,
                    //            UpdatedBy = user,
                    //            UpdatedDt = date
                    //        };

                    //        var unitGroupDescriptionItems = await _db.IcsParUnitGroupDescriptionItems
                    //            .Include(i => i.IcsParItem.PsCardItemExtn.PsCardItem)
                    //            .AsNoTracking()
                    //            .Where(w => w.IcsPartUnitGroupDescription.IcsParUnitGroup.IcsParId == prevIcsPar.Id
                    //                && w.UnitGroupDescriptionId == unitGroupDescription.Id
                    //                && icsParItems.Any(a => a.PsCardItemExtn.Id == w.IcsParItem.PsCardItemExtnId)).ToListAsync();

                    //        foreach (var unitGroupDescriptionItem in unitGroupDescriptionItems) // per itemId
                    //        {
                    //            unitCost = unitCost + unitGroupDescriptionItem.IcsParItem.PsCardItemExtn.PsCardItem.UnitCost;
                    //            foreach (var selectedItemId in selectedItemIds)
                    //            {
                    //                var gSelectedId = Guid.Parse(selectedItemId);
                    //                if (gSelectedId == unitGroupDescriptionItem.IcsParItemId)
                    //                {
                    //                    var newIcsParItem = _db.IcsParItems.Include(i => i.PsCardItemExtn).AsNoTracking().SingleOrDefault(s => s.PrevItemId == gSelectedId);
                    //                    var icsParUnitGroupDescItem = new IcsParUnitGroupDescriptionItem()
                    //                    {
                    //                        Id = Guid.NewGuid(),
                    //                        UnitGroupDescriptionId = icsParUnitGroupDesc.Id,
                    //                        IcsParItemId = newIcsParItem.Id,
                    //                        InsertedBy = user,
                    //                        InsertedDt = date,
                    //                        UpdatedBy = user,
                    //                        UpdatedDt = date
                    //                    };
                    //                    icsParUnitGroupDesc.IcsParUnitGroupDescriptionItems.Add(icsParUnitGroupDescItem);
                    //                    addCost = addCost + newIcsParItem.PsCardItemExtn.AddCost ?? 0;
                    //                    break;
                    //                }
                    //            }
                    //        }
                    //        icsParUnitGroup.IcsPartUnitGroupDescriptions.Add(icsParUnitGroupDesc);
                    //    }

                    //    icsParUnitGroup.UnitCost = unitCost;
                    //    icsParUnitGroup.TotalCost = unitCost;
                    //    icsParUnitGroup.AddCost = addCost;
                    //    icsParUnitGroup.TUnitCost = unitCost + addCost;
                    //    icsParUnitGroup.GTotalCost = unitCost + addCost;

                    //    icsPar.IcsParUnitGroups.Add(icsParUnitGroup);
                    //}

                    await _db.SaveChangesAsync();
                }
            }

            return model;
        }

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

        private async ValueTask<string> NextParRefNoAsync(DateTime? refDate, string refType)
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

        private async ValueTask<string> NextIcsRefNoAsync(DateTime? refDate, string refType, int? icvValue)
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
            _icsParSharedService.ValidateIfPosted(entity);
        }

        public void ValidateIfNotPosted(IcsPar entity)
        {
            _icsParSharedService.ValidateIfNotPosted(entity);
        }

        public void ValidateUpdates(string refNo, string refType)
        {
            _icsParSharedService.ValidateUpdates(refNo, refType);
        }

        public async ValueTask<IcsPar> PostAsync(string refNo, string refType, string user, DateTime date)
        {
            return await _icsParSharedService.PostAsync(refNo, refType, user, date);
        }

        public async ValueTask<IcsPar> UnPostAsync(string refNo, string refType, string user, DateTime date)
        {
            return await _icsParSharedService.UnPostAsync(refNo, refType, user, date);
        }

        public async Task<bool> IsWwithUploadAsync(Guid? icsParId)
        {
            return await _icsParSharedService.IsWwithUploadAsync(icsParId);
        }

        public async Task ValidateUploadAsync(Guid? icsParId, string parNo, string refType)
        {
            await _icsParSharedService.ValidateUploadAsync(icsParId, parNo, refType);
        }
    }
}
