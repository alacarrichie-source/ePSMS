using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Codes;
using iLgs.Services.PurchaseOrder;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.RPC
{
    public interface IRpciService
    {
        IQueryable<RPCI_VM> GetAll();
        IQueryable<RPCI_VM> GetAll(bool? isPosted, string type);
        IQueryable<RPCIItem> GetRpciXls(DateTime? asOf, Guid? id);
        ValueTask<RPCI> GetByIdAsync(Guid? id);
        ValueTask<RPCI_VM> GetByAsOfAsync(DateTime? AsOf);
        ValueTask<RPCI_VM> GenerateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI> PostAsync(Guid? id, string user, DateTime date);
        ValueTask<RPCI> UnPostAsync(Guid? id, string user, DateTime date);
        ValueTask<RPCI_VM> CreateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI_VM> UpdateAsync(RPCI_VM model, string user, DateTime date);
        ValueTask<RPCI_VM> DeleteAsync(RPCI_VM model, string user, DateTime date);
    }

    public class RpciService : IRpciService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<RPCI_VM> _vmExceptionService;
        private readonly IExceptionService<RPCI> _exceptionService;
        private readonly IOrderService _orderService;
        private readonly IPriceCapService _priceCapService;
        private readonly ISemiExpendableService _semiExpendableService;

        private decimal? _priceCap;
        private decimal? _SPHV;

        public RpciService(AppManEntities db,
            ICreateAndLogExceptions exceptions,
            IExceptionService<RPCI_VM> vmExceptionService,
            IExceptionService<RPCI> exceptionService,
            IOrderService orderService,
            IPriceCapService priceCapService,
            ISemiExpendableService semiExpendableService)
        {
            _db = db;
            _db.Database.CommandTimeout = 3000;
            _exceptions = exceptions;
            _vmExceptionService = vmExceptionService;
            _exceptionService = exceptionService;
            _orderService = orderService;
            _priceCapService = priceCapService;
            _semiExpendableService = semiExpendableService;
        }

        private decimal GetPriceCap()
        {
            return _priceCap ?? (_priceCap = _priceCapService.GetPriceCap()).Value;
        }

        private decimal GetSPHV()
        {
            return _SPHV ?? (_SPHV = _semiExpendableService.GetSPHV()).Value;
        }

        public ValueTask<RPCI> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.RPCIs.FindAsync(id);
            return data;
        });

        public IQueryable<RPCIItem> GetRpciXls(DateTime? asOf, Guid? id)
        {
            var data = _db.RPCIItems.Include(i => i.RPCI).Where(w => w.RPCI.AsOf == asOf && (id == null || w.RPCI.Id == id))
                .OrderBy(o => o.RPCI.Fund).ThenBy(o => o.PoNo);

            return data;
        }

        public ValueTask<RPCI_VM> GetByAsOfAsync(DateTime? AsOf) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var data = await _db.RPCIs.Where(w => w.AsOf == AsOf)
                .Select(s => new RPCI_VM
                {
                    Id = s.Id,
                    AsOf = s.AsOf,
                    Fund = s.Fund,
                    FromDonation = s.FromDonation,
                    InvDist = s.InvDist,
                    ItemTypeId = s.ItemTypeId,
                    Account = s.Account,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    DeptId = s.DeptId,
                    Department = s.Department,
                    ApprovedBy = s.ApprovedBy,
                    VerifiedBy = s.VerifiedBy,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    InvDistDesc = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    AcqMode = s.FromDonation == true ? "From Donation" : "Purchase"
                }).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<RPCI_VM> GetAll() =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = _db.RPCIs
                .Select(s => new RPCI_VM
                {
                    Id = s.Id,
                    Type = s.Type,
                    AsOf = s.AsOf,
                    Fund = s.Fund,
                    FromDonation = s.FromDonation,
                    InvDist = s.InvDist,
                    ItemTypeId = s.ItemTypeId,
                    Account = s.Account,
                    DeptId = s.DeptId,
                    Department = s.Department,
                    CertifiedCorrectBy = s.CertifiedCorrectBy,
                    ApprovedBy = s.ApprovedBy,
                    VerifiedBy = s.VerifiedBy,
                    PostedBy = s.PostedBy,
                    PostedDt = s.PostedDt,
                    InsertedDt = s.InsertedDt,
                    InvDistDesc = s.InvDist == "I" ? "Inventory" : s.InvDist == "D" ? "For Distribution" : "",
                    AcqMode = s.FromDonation == true ? "From Donation" : "Purchase",
                    QtyCons = s.RPCIItems.Where(w => w.ItemType == "C").Sum(x => x.TotalBalance),
                    AmountCons = s.RPCIItems.Where(w => w.ItemType == "C").Sum(x => x.AcqCost),
                    QtySPHV = s.RPCIItems.Where(w => w.ItemType == "SPHV").Sum(x => x.TotalBalance),
                    AmountSPHV = s.RPCIItems.Where(w => w.ItemType == "SPHV").Sum(x => x.AcqCost),
                    QtySPLV = s.RPCIItems.Where(w => w.ItemType == "SPLV").Sum(x => x.TotalBalance),
                    AmountSPLV = s.RPCIItems.Where(w => w.ItemType == "SPLV").Sum(x => x.AcqCost),
                    QtySE = s.RPCIItems.Where(w => w.ItemType.Substring(0, 1) == "S").Sum(x => x.TotalBalance),
                    AmountSE = s.RPCIItems.Where(w => w.ItemType.Substring(0, 1) == "S").Sum(x => x.AcqCost),
                    QtyBalance = s.RPCIItems.Sum(x => x.TotalBalance),
                    AcqCost = s.RPCIItems.Sum(x => x.AcqCost),
                    IsPosted = s.IsPosted
                });
            return data;
        });

        public IQueryable<RPCI_VM> GetAll(bool? isPosted, string type) =>
        _vmExceptionService.TryCatch(() =>
        {
            var data = GetAll();
            //return data.Where(w => w.IsPosted == isPosted && w.Type == type);
            return data.Where(w => w.IsPosted == isPosted);
        });

        public ValueTask<RPCI_VM> GenerateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            if (model.ItemTypeId == Guid.Empty)
            {
                model.ItemTypeId = null;
            }

            //if (model.DeptId != null) {                 
            //&& a.Account == model.Account 
            if (await _db.RPCIs.AnyAsync(a => a.AsOf == model.AsOf && a.Fund == model.Fund && a.FromDonation == model.FromDonation
                 && a.InvDist == model.InvDist && a.ItemTypeId == model.ItemTypeId                 
                 && a.DeptId == model.DeptId && a.IsPosted == model.IsPosted))
            {
                throw new RecordAlreadyExistsException();
            }
            //}

            if (model.ItemTypeId == null)
            {
                model.Account = "ALL";
            }

            var priceCap = _priceCapService.GetPriceCap(model.AsOf);
            var sphv = _semiExpendableService.GetSPHV(model.AsOf);
            await _db.Database.ExecuteSqlCommandAsync("Exec RPCI_Generate {0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}",
                model.Type, model.AsOf, model.Fund, model.FromDonation, model.InvDist, model.ItemTypeId, model.Account, model.DeptId, user, model.IsPosted, priceCap, sphv);
            model = await GetByAsOfAsync(model.AsOf);
            return model;
        });

        public ValueTask<RPCI> PostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}");
            }

            if (string.IsNullOrWhiteSpace(entity.CertifiedCorrectBy))
            {
                throw new InvalidValueException("Certified Correct by is Required!");
            }

            if (string.IsNullOrWhiteSpace(entity.ApprovedBy))
            {
                throw new InvalidValueException("Aporoved by is Required!");
            }

            if (string.IsNullOrWhiteSpace(entity.VerifiedBy))
            {
                throw new InvalidValueException("Verified by is Required!");
            }


            entity.PostedBy = user;
            entity.PostedDt = date;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RPCI> UnPostAsync(Guid? id, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(id);
            if (entity == null)
            {
                throw new RecordNotFoundException(id);
            }

            if (string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordNotYetPostedException($"Record is not yet posted!");
            }

            entity.PostedBy = "";
            entity.PostedDt = null;
            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return entity;
        });

        public ValueTask<RPCI_VM> CreateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var notPosted = await _orderService.GetNotPostedAsync((DateTime)model.AsOf);
            if (notPosted > 0)
            {
                throw new RecordRelationshipException(string.Format("The system found {0} records that are not yet posted as of date specified! Please post before proceeding..."));
            }

            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new RPCI()
            {
                Id = model.Id,
                Type = model.Type,
                AsOf = model.AsOf,
                Fund = model.Fund,
                FromDonation = model.FromDonation,
                InvDist = model.InvDist,
                ItemTypeId = model.ItemTypeId,
                Account = model.Account,
                DeptId = model.DeptId,
                Department = model.Department,
                CertifiedCorrectBy = model.CertifiedCorrectBy,
                ApprovedBy = model.ApprovedBy,
                VerifiedBy = model.VerifiedBy,
                PostedBy = model.PostedBy,
                PostedDt = model.PostedDt,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.RPCIs.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCI_VM> DeleteAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot delete!");
            }


            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.RPCIs.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<RPCI_VM> UpdateAsync(RPCI_VM model, string user, DateTime date) =>
        _vmExceptionService.TryCatch(async () =>
        {
            var entity = await _db.RPCIs.FindAsync(model.Id);
            if (entity == null)
            {
                throw new RecordNotFoundException(model.Id);
            }

            if (!string.IsNullOrWhiteSpace(entity.PostedBy))
            {
                throw new RecordAlreadyPostedException($"Record Already Posted by {entity.PostedBy}, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.AsOf = model.AsOf;
            entity.Fund = model.Fund;
            entity.FromDonation = model.FromDonation;
            entity.InvDist = model.InvDist;
            entity.ItemTypeId = model.ItemTypeId;
            entity.Account = model.Account;
            entity.DeptId = model.DeptId;
            entity.Department = model.Department;
            entity.CertifiedCorrectBy = model.CertifiedCorrectBy;
            entity.ApprovedBy = model.ApprovedBy;
            entity.VerifiedBy = model.VerifiedBy;
            entity.PostedBy = model.PostedBy;
            entity.PostedDt = model.PostedDt;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.RPCIs.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();
            return model;
        });
    }
}