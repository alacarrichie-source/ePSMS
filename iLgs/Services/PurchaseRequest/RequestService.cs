using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using iLgs.Models;
using System.Data.Entity;
using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using System.Linq.Expressions;

namespace iLgs.Services.PurchaseRequest
{
    public interface IRequestService
    {
        IQueryable<RequestVM> GetAll();
        ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId);
        Task<Request> GetByIdAsync(Guid? prId);
        Task<Request> GetByPrNoAsync(string prNo);
        Task<bool> IsAnyPrNoAsync(Guid id, string prNo);
        Task<bool> IsAnyRisNoAsync(Guid id, string risNo);
        bool IsPosted(Guid requestId);
        bool IsPosted(Request request);
        bool IsPosted(RequestItem requestItem);
        bool IsPosted(RequestItemUnitGroup requestItemUnitGroup);
        bool IsPosted(RequestItemUnitGroupDescription requestItemunitGroupDescription);
        bool IsPosted(RequestItemUnitGroupDescriptionItem requestItemunitGroupDescriptionItem);
        Task<bool> IsPostedAsync(Guid? requestId);
        Task<bool> IsWithPOAsync(Guid? requestId);
        Task<bool> IsPoPostedAsync(Guid? requestId);
        Task<bool> IsWithInvalidUnitCostAsync(Guid? requestId);

        Task<RequestVM> CreateAsync(RequestVM model, string user, DateTime date);
        Task<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date);
        Task<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date);
        Task PostAsync(Guid requestId, string user, DateTime date);
        Task UnpostAsync(Guid requestId, string user, DateTime date);
    }

    public class RequestService : IRequestService
    {
        private readonly AppManEntities _db;
        private readonly IUserService _userService;
        private readonly decimal _priceCap = 50000;

        public RequestService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(_db);
        }

        private static Expression<Func<Request, RequestVM>> Projection
        = s => new RequestVM
        {
            Id = s.Id,
            PrNo = s.PrNo,
            PrDate = s.PrDate,
            Availability = s.Availability,
            AvaialbilityDesig = s.AvaialbilityDesig,
            ApprovedBy = s.ApprovedBy,
            ApprovedDesig = s.ApprovedDesig,
            SubmittedBy = s.SubmittedBy,
            SubmittedDt = s.SubmittedDt,
            IsWithPO = s.Orders.Any(),
            InsertedBy = s.InsertedBy,
            InsertedDt = s.InsertedDt,
            // Transients from RIS
            RisNo = s.RISs.RisNo,
            Fund = s.RISs.Fund,
            Department = s.RISs.Office,
            Section = s.RISs.Division,
            FPP = s.RISs.FPP,
            Purpose = s.RISs.Purpose,
            RequestedBy = s.RISs.RequestedBy,
            RequestedDesig = s.RISs.ReceivedByDesignation,
            RisDate = s.RISs.RisDate,
            RisId = s.RisId
        };

        public IQueryable<RequestVM> GetAll()
        {
            return _db.Requests.AsNoTracking().Select(Projection).AsQueryable();
        }

        public async ValueTask<IQueryable<RequestVM>> GetAllAsync(string userId)
        {
            IQueryable<RequestVM> data = null;
            if (await _userService.IsAdminAsync(userId))
            {
                data = _db.Requests.AsNoTracking()
                    .Select(Projection).OrderByDescending(o => o.RisNo);
            }
            else
            {
                data = _db.Requests.AsNoTracking()
                    .Where(w => w.RISs.Codextn.DepartmentUsers.Any(a => a.UserId == userId))
                    .Select(Projection).OrderByDescending(o => o.RisNo);
            }
            return data;
        }

        public async Task<Request> GetByIdAsync(Guid? prId)
        {
            return await _db.Requests.FindAsync(prId);
        }

        public async Task<bool> IsAnyPrNoAsync(Guid id, string prNo)
        {
            return await _db.Requests.AnyAsync(a => a.Id != id && a.PrNo == prNo);
        }

        public async Task<bool> IsAnyRisNoAsync(Guid id, string risNo)
        {
            return await _db.Requests.AnyAsync(a => a.Id != id && a.RISs.RisNo == risNo);
        }

        public async Task<Request> GetByPrNoAsync(string prNo)
        {
            return await _db.Requests.Where(w => w.PrNo == prNo).FirstOrDefaultAsync();
        }

        public bool IsPosted(Guid requestId)
        {
            var entity = _db.Requests.Find(requestId);
            return !string.IsNullOrWhiteSpace(entity.SubmittedBy);
        }

        public bool IsPosted(Request request)
        {
            return IsPosted(request.Id);
        }

        public bool IsPosted(RequestItem requestItem)
        {
            var requestId = (Guid)requestItem.PrId;
            return IsPosted(requestId);
        }

        public bool IsPosted(RequestItemUnitGroup requestItemUnitGroup)
        {
            var requestId = (Guid)requestItemUnitGroup.PrId;
            return IsPosted(requestId);
        }

        public bool IsPosted(RequestItemUnitGroupDescription requestItemUnitGroupDescription)
        {
            var requestId = (Guid)_db.RequestItemUnitGroupDescriptions
                .Include(i => i.RequestItemUnitGroup)
                .Where(w => w.RequestItemUnitGroupId == requestItemUnitGroupDescription.RequestItemUnitGroupId)
                .AsNoTracking()
                .FirstOrDefault()?.RequestItemUnitGroup.PrId;
            return IsPosted(requestId);
        }

        public bool IsPosted(RequestItemUnitGroupDescriptionItem requestItemUnitGroupDescriptionItem)
        {
            var requestId = (Guid)_db.RequestItemUnitGroupDescriptionItems
                .Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup)
                .Where(w => w.RequestItemUnitGroupDescriptionId == requestItemUnitGroupDescriptionItem.RequestItemUnitGroupDescriptionId)
                .AsNoTracking()
                .FirstOrDefault()?.RequestItemUnitGroupDescription.RequestItemUnitGroup.PrId;
            return IsPosted(requestId);
        }

        public async Task<bool> IsPostedAsync(Guid? requestId)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity == null)
            {
                return false;
            }
            else
            {
                return !string.IsNullOrWhiteSpace(entity.SubmittedBy);
            }
        }

        public async Task<bool> IsWithPOAsync(Guid? requestId)
        {
            return await _db.Orders.AnyAsync(a => a.PrId == requestId);
        }

        public async Task<bool> IsPoPostedAsync(Guid? requestId)
        {
            return await _db.Orders.AnyAsync(a => a.PrId == requestId && !(a.PostedBy == "" || a.PostedBy == null));
        }

        public async Task<bool> IsWithInvalidUnitCostAsync(Guid? requestId)
        {
            return await _db.RequestItems.AnyAsync(a => a.PrId == requestId && (a.UnitCost == null || a.UnitCost == 0));
        }

        public async Task<RequestVM> CreateAsync(RequestVM model, string user, DateTime date)
        {
            ValidateIfNull(model);
            model.Id = Guid.NewGuid();
            if (string.IsNullOrWhiteSpace(model.PrNo))
            {
                model.PrNo = NextPrNo((DateTime)model.PrDate);
            }
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            ValidateOnCreate(model);

            var entity = new Request()
            {
                Id = model.Id,
                RisId = model.RisId,
                PrNo = model.PrNo,
                PrDate = model.PrDate,
                Availability = model.Availability ?? "",
                AvaialbilityDesig = model.AvaialbilityDesig ?? "",
                ApprovedBy = model.ApprovedBy ?? "",
                ApprovedDesig = model.ApprovedDesig ?? "",
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt

                // Transients From RIS Removed
                //Fund = model.Fund,
                //Department = model.Department,
                //Section = model.Section,
                //PrNo = model.PrNo,
                //PrDate = model.PrDate,
                //FPP = model.FPP,
                //Purpose = model.Purpose,
                //RequestedBy = model.RequestedBy,
                //RequestedDesig = model.RequestedDesig,
            };

            // include items during add
            var risItems = _db.RisItems.Where(w => w.RisId == model.RisId).OrderBy(o => o.InsertedDt).ToList();
            foreach (var risItem in risItems)
            {
                var insertedDt = DateTime.Now;
                RequestItem requestItem = new RequestItem()
                {
                    Id = Guid.NewGuid(),
                    RisItemId = risItem.Id,
                    PrId = entity.Id,
                    Qty = risItem.QtyRequest,
                    InsertedBy = user,
                    InsertedDt = insertedDt,
                    UpdatedBy = user,
                    UpdatedDt = insertedDt
                };

                //foreach (var risItemExtn in risItem.RisItemExtns)
                //{
                //    RequestItemExtn requestItemExtn = new RequestItemExtn()
                //    {
                //        Id = Guid.NewGuid(),
                //        RequestItemId = requestItem.Id,
                //        ItemKey = risItemExtn.ItemKey,
                //        ItemValue = risItemExtn.ItemValue,
                //        Sequence = risItemExtn.Sequence,
                //        InsertedBy = user,
                //        InsertedDt = date,
                //        UpdatedBy = user,
                //        UpdatedDt = date
                //    };
                //    requestItem.RequestItemExtns.Add(requestItemExtn);
                //}

                entity.RequestItems.Add(requestItem);
            }

            // Unit Groups
            var unitGroups = await _db.RisItemUnitGroups.Include(i => i.RisItemUnitGroupDescriptions).Where(w => w.RisId == model.RisId).OrderBy(o => o.InsertedDt).ToListAsync();
            foreach(var unitGroup in unitGroups)
            {
                var unitGroupDt = DateTime.Now;
                var requestItemUnitGroup = new RequestItemUnitGroup()
                {
                    Id = Guid.NewGuid(),
                    PrId = model.Id,
                    RisItemUnitGroupId = unitGroup.Id,
                    InsertedBy = user,
                    InsertedDt = unitGroupDt,
                    UpdatedBy = user,
                    UpdatedDt = unitGroupDt
                };

                foreach(var unitGroupDescription in unitGroup.RisItemUnitGroupDescriptions.OrderBy(o => o.InsertedDt).ToList())
                {
                    var groupDescriptionDt = DateTime.Now;
                    var requestItemUnitGroupDescription = new RequestItemUnitGroupDescription()
                    {
                        Id = Guid.NewGuid(),
                        RequestItemUnitGroupId = requestItemUnitGroup.Id,
                        RisItemUnitGroupDescriptionId = unitGroupDescription.Id,
                        InsertedBy = user,
                        InsertedDt = groupDescriptionDt,
                        UpdatedBy = user,
                        UpdatedDt = groupDescriptionDt
                    };

                    var risItemUnitGroupDescriptionItems = await _db.RisItemUnitGroupDescriptionItems.Where(w => w.UnitGroupDescriptionId == unitGroupDescription.Id).OrderBy(o => o.InsertedDt).ToListAsync();
                    foreach(var unitGroupDescriptionItem in risItemUnitGroupDescriptionItems)
                    {
                        var groupDescriptionItemDt = DateTime.Now;
                        var requsetItemUnitGroupDescriptionItem = new RequestItemUnitGroupDescriptionItem()
                        {
                            Id = Guid.NewGuid(),                            
                            RisItemUnitGroupDescriptionItemId = unitGroupDescriptionItem.Id,
                            RequestItemUnitGroupDescriptionId = requestItemUnitGroupDescription.Id,
                            RequestItemId = entity.RequestItems.FirstOrDefault(f => f.RisItemId == unitGroupDescriptionItem.RisItemId).Id,
                            InsertedBy = user,
                            InsertedDt = groupDescriptionItemDt,
                            UpdatedBy = user,
                            UpdatedDt = groupDescriptionItemDt
                        };
                        requestItemUnitGroupDescription.RequestItemUnitGroupDescriptionItems.Add(requsetItemUnitGroupDescriptionItem);
                    }
                    requestItemUnitGroup.RequestItemUnitGroupDescriptions.Add(requestItemUnitGroupDescription);
                }
                entity.RequestItemUnitGroups.Add(requestItemUnitGroup);
            }

            _db.Requests.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestVM> UpdateAsync(RequestVM model, string user, DateTime date)
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Requests.Where(w => w.Id == model.Id).FirstOrDefaultAsync();
            ValidateRecord(entity, model.Id);
            ValidateIfPosted(entity);
            ValidateOnUpdate(entity, model);

            // if there's a change of requisition item
            if (entity.RisId != model.RisId)
            {
                var requestItems = _db.RequestItems.Where(w => w.PrId == model.Id);
                await requestItems.ForEachAsync(f => {
                    f.UpdatedBy = model.UpdatedBy;
                    f.UpdatedDt = model.UpdatedDt;
                });
                await _db.SaveChangesAsync();

                _db.RequestItems.RemoveRange(requestItems);
                await _db.SaveChangesAsync();
                
                // include items during add
                var risItems = _db.RisItems.Where(w => w.RisId == model.RisId).ToList();
                foreach (var risItem in risItems)
                {
                    RequestItem requestItem = new RequestItem()
                    {
                        Id = Guid.NewGuid(),
                        RisItemId = risItem.Id,
                        PrId = entity.Id,
                        Qty = risItem.QtyRequest,
                        InsertedBy = user,
                        InsertedDt = date,
                        UpdatedBy = user,
                        UpdatedDt = date
                    };

                    //foreach (var risItemExtn in risItem.RisItemExtns)
                    //{
                    //    RequestItemExtn requestItemExtn = new RequestItemExtn()
                    //    {
                    //        Id = Guid.NewGuid(),
                    //        RequestItemId = requestItem.Id,
                    //        ItemKey = risItemExtn.ItemKey,
                    //        ItemValue = risItemExtn.ItemValue,
                    //        Sequence = risItemExtn.Sequence,
                    //        InsertedBy = user,
                    //        InsertedDt = date,
                    //        UpdatedBy = user,
                    //        UpdatedDt = date
                    //    };
                    //    requestItem.RequestItemExtns.Add(requestItemExtn);
                    //}

                    entity.RequestItems.Add(requestItem);
                }
            }

            entity.RisId = model.RisId;
            entity.PrNo = model.PrNo;
            entity.PrDate = model.PrDate;
            entity.Availability = model.Availability ?? "";
            entity.AvaialbilityDesig = model.AvaialbilityDesig ?? "";
            entity.ApprovedBy = model.ApprovedBy ?? "";
            entity.ApprovedDesig = model.ApprovedDesig ?? "";
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
            
            _db.Requests.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<RequestVM> DeleteAsync(RequestVM model, string user, DateTime date)
        {
            ValidateIfNull(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Requests.FindAsync(model.Id);
            ValidateRecord(entity, model.Id);
            ValidateIfPosted(entity);
            ValidateRelationship(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.Requests.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.Requests.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        }


        public async Task PostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                if (string.IsNullOrEmpty(entity.Availability))
                {
                    throw new InvalidValueException("Cash availability is Required.");
                }

                if (string.IsNullOrEmpty(entity.ApprovedBy))
                {
                    throw new InvalidValueException("Approved by is Required.");
                }


                var unitGroupItems = _db.RequestItemUnitGroupDescriptionItems
                    .AsNoTracking()
                    .Include(i => i.RequestItem)
                    .Include(i => i.RequestItemUnitGroupDescription.RequestItemUnitGroup)
                    .Where(w => w.RequestItemUnitGroupDescription.RequestItemUnitGroup.PrId == entity.Id).ToList();
                if (unitGroupItems.Any())
                {
                    var rate = unitGroupItems.Sum(s => s.RequestItem.PriceRate) ?? 0;
                    if (rate != 100)
                    {
                        throw new InvalidValueException("Price rate must be 100%");
                    }
                }

                if (_db.RequestItems.Any(a => a.PrId == requestId && (!a.UnitCost.HasValue || a.UnitCost == 0)))
                {
                    throw new InvalidValueException("All items must have a unit cost.");
                }

                // validate item price
                var requestItems = _db.RequestItems.Include(i => i.RisItem.ItemCode.ItemType).AsNoTracking().Where(w => w.PrId == requestId).ToList();
                foreach(var requestItem in requestItems)
                {                    
                    var category = requestItem.RisItem.ItemCode.ItemType.Category;
                    decimal? unitCost = 0;
                    var unitGroup = _db.RequestItemUnitGroups.Where(w => w.RequestItemUnitGroupDescriptions.Any(a => a.RequestItemUnitGroupDescriptionItems.Any(b => b.RequestItemId == requestItem.Id))).FirstOrDefault();
                    if (unitGroup != null)
                    {
                        unitCost = unitGroup.UnitCost;
                        if (category != "S")
                        {
                            if (unitCost < _priceCap)
                            {
                                throw new InvalidValueException($"Please use supplies code for items with a group unit cost below {_priceCap:n0}.");
                            }
                        }
                        else
                        {
                            if (unitCost >= _priceCap)
                            {
                                throw new InvalidValueException($"Please use property code for items with a group unit cost of {_priceCap:n0} and above.");
                            }
                        }
                    }
                    else
                    {
                        unitCost = requestItem.UnitCost;
                        if (category != "S")
                        {
                            if (unitCost < _priceCap)
                            {
                                throw new InvalidValueException($"Please use supplies code for items with a unit cost below {_priceCap:n0}.");
                            }
                        }
                        else
                        {
                            if (unitCost >= _priceCap)
                            {
                                throw new InvalidValueException($"Please use property code for items with a unit cost of {_priceCap:n0} and above.");
                            }
                        }
                    }
                }

                entity.SubmittedBy = user;
                entity.SubmittedDt = date;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                _db.Requests.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
                await _db.SaveChangesAsync();
            }
        }

        public async Task UnpostAsync(Guid requestId, string user, DateTime date)
        {
            var entity = await _db.Requests.FindAsync(requestId);
            if (entity != null)
            {
                entity.SubmittedBy = null;
                entity.SubmittedDt = null;
                entity.UpdatedBy = user;
                entity.UpdatedDt = date;

                _db.Requests.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
                await _db.SaveChangesAsync();
            }
        }

        private string NextPrNo(DateTime prDate)
        {
            string yyyy = prDate.Year.ToString().Trim();
            string mm = prDate.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.Requests.Where(w => w.PrDate.Value.Year == prDate.Year).OrderByDescending(o => o.PrNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.PrNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }

        private void ValidateOnCreate(RequestVM model)
        {
            if (!string.IsNullOrWhiteSpace(model.PrNo) && _db.Requests.Any(a => a.PrNo == model.PrNo))
            {
                throw new RecordAlreadyExistsException(string.Format("PR Number {0} already exists", model.PrNo));
            }
            else
            {
                var ris = _db.RISses.Find(model.RisId);
                if (ris == null)
                {
                    throw new NotFoundException((Guid)model.RisId);
                }
                else
                {
                    if (ris.RisDate > model.PrDate)
                    {
                        throw new InvalidValueException("PR Date must be greater than or equal to RIS date!");
                    }
                }
            }
        }

        private void ValidateOnUpdate(Request entity, RequestVM model)
        {            
            if (entity.SubmittedDt != null)
            {
                throw new RecordAlreadyPostedException(string.Format("PR Number {0} already posted, cannot update!", model.PrNo));
            }

            if (_db.Requests.Any(a => a.PrNo == model.PrNo && a.Id != model.Id))
            {
                throw new RecordAlreadyExistsException(string.Format("PR Number {0} already exists!", model.PrNo));
            }

            var ris = _db.RISses.Find(model.RisId);
            if (ris == null)
            {
                throw new RecordRelationshipException(string.Format("RIS Number {0} does exists!", model.RisNo));
            }

            else if (ris.RisDate > model.PrDate)
            {
                throw new InvalidValueException("PR date must be greather than or equal to RIS date!");
            }
        }

        //private async ValueTask ValidateOnDestroy(RequestVM model)
        //{
        //    var order = await _db.Orders.FindAsync(model.Id);
        //    if (order == null)
        //    {
        //        throw new RecordNotFoundException(model.Id);
        //    }

        //    if (await IsPostedAsync(model.Id))
        //    {
        //        throw new RecordAlreadyPostedException(string.Format("PO Number {0} already Posted, cannot delete!", model.PoNo));
        //    }

        //    if (await GetAnyAirsAsync(model.Id))
        //    {
        //        throw new RecordRelationshipException("PO Number already with AIR, cannot delete!");
        //    }

        //    if (await GetAnyParsAsync(model.Id))
        //    {
        //        throw new RecordRelationshipException("PO Number already with PAR, cannot delete!");
        //    }
        //}

        private void ValidateRelationship(Guid prId)
        {
            var order = _db.Orders.Where(a => a.PrId == prId).AsNoTracking().FirstOrDefault();
            if (order != null)
            {
                throw new RecordRelationshipException($"This PR Number is in use by PO Number {order.PoNo}, cannot delete!");
            }
        }

        private void ValidateRecord(Request entity, Guid id)
        {
            if (entity is null)
            {
                throw new NotFoundException(id);
            }
        }

        private void ValidateIfNull(RequestVM model)
        {
            if (model is null)
            {
                throw new NullException();
            }
        }

        private void ValidateIfPosted(Request entity)
        {
            if (entity != null && !string.IsNullOrWhiteSpace(entity.SubmittedBy))
            {
                throw new RecordAlreadyPostedException(string.Format("PR Number {0} already posted, cannot update!", entity.PrNo));
            }
        }
    }
}