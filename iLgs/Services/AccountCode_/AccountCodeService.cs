using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using static iLgs.Models.Enums;

namespace iLgs.Services.AccountCode_
{
    public interface IAccountCodeService
    {
        IQueryable<AccountCodeVM> GetAll();
        IQueryable<AccountCodeVM> GetAllByType(string type);
        ValueTask<AccountCodeVM> GetByIdAsync(Guid? id);

        ValueTask<AccountCodeVM> CreateAsync(AccountCodeVM model, string user, DateTime date);
        ValueTask<AccountCodeVM> UpdateAsync(AccountCodeVM model, string user, DateTime date);
        ValueTask<AccountCodeVM> DeleteAsync(AccountCodeVM model, string user, DateTime date);        

        IAccountCodeItemService AccountCodeItem { get; }

        //MemoryStream ProcessExcelFileSummary(int? forYear, Guid? deptId, Guid? locationId, DateTime? asOf, DateTime? insertedAsOf, string templateFilePath);
    }

    public class AccountCodeService : IAccountCodeService
    {
        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private readonly IExceptionService<AccountCodeVM> _exceptionService;
        private readonly IUserService _userService;
        
        private readonly IAccountCodeItemService _AccountCodeItemService;

        public AccountCodeService(AppManEntities db)
        {
            _db = db;
            _exceptions = new CreateAndLogExceptions();
            _exceptionService = new ExceptionService<AccountCodeVM>();
            _userService = new UserService(_db);
            _AccountCodeItemService = new AccountCodeItemService(_db);
        }

        public IAccountCodeItemService AccountCodeItem => _AccountCodeItemService;

        private Expression<Func<AccountCode, AccountCodeVM>> Projection()
        {
            return s => new AccountCodeVM
            {
                Id = s.Id,
                Effectivity = s.Effectivity,
                Type = s.Type,
                Description = s.Description,
                InsertedBy = s.InsertedBy,
                InsertedDt = s.InsertedDt,
                UpdatedBy = s.UpdatedBy,
                UpdatedDt = s.UpdatedDt,                
            };
        }

        public ValueTask<AccountCodeVM> GetByIdAsync(Guid? id) =>
        _exceptionService.TryCatch(async () =>
        {
            var data = await _db.AccountCodes.Where(w => w.Id == id).Select(Projection()).FirstOrDefaultAsync();
            return data;
        });

        public IQueryable<AccountCodeVM> GetAll()
        {
            var data = _db.AccountCodes.AsNoTracking().Select(Projection());
            return data;
        }

        public IQueryable<AccountCodeVM> GetAllByType(string type)
        {
            var data = GetAll().Where(w => w.Type == type);
            return data;
        }

        public ValueTask<AccountCodeVM> CreateAsync(AccountCodeVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.UpdatedBy = user;
            model.InsertedDt = date;
            model.UpdatedDt = date;

            var entity = new AccountCode();
            MapModelToEntityFields(entity, model, Mode.ADD);

            _db.AccountCodes.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public ValueTask<AccountCodeVM> UpdateAsync(AccountCodeVM model, string user, DateTime date) =>
       _exceptionService.TryCatch(async () =>
       {
           
           var entity = await _db.AccountCodes.FindAsync(model.Id);

           model.UpdatedBy = user;
           model.UpdatedDt = date;

           MapModelToEntityFields(entity, model, Mode.EDIT);

           await _db.SaveChangesAsync();

           return model;
       });

        public ValueTask<AccountCodeVM> DeleteAsync(AccountCodeVM model, string user, DateTime date) =>
        _exceptionService.TryCatch(async () =>
        {            
            var entity = await _db.AccountCodes.Where(w => w.Id == model.Id).FirstOrDefaultAsync();

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            await _db.SaveChangesAsync();

            _db.AccountCodes.Remove(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public void MapModelToEntityFields(AccountCode entity, AccountCodeVM model, Mode mode)
        {
            if (mode == Mode.ADD)
            {
                entity.Id = model.Id;
                entity.InsertedBy = model.InsertedBy;
                entity.InsertedDt = model.InsertedDt;
            }

            entity.Effectivity = model.Effectivity;
            entity.Type = model.Type.ToUpper();
            entity.Description = model.Description;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;
        }        
    }
}