using iLgs.Models;
using iLgs.Services.Validators;
using iLgs.Utilities;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services.Codes
{
    public interface ICodextnService
    {
        IQueryable<CodextnVM> GetByMastCode(string mastCode);
        IQueryable<CodextnVM> GetByMastId(Guid mastId);
        ValueTask<CodextnVM> GetByIdAsync(Guid? id);
        bool IsValidMastCodeId(string mastCode, Guid? id);
        bool IsValidMastCodeCode(string mastCode, string code);
        bool IsValidCodeDesc(string mainCode, string description);
        ValueTask<IQueryable<Codextn>> GetUserDepartmentsAsync(string userId);
        IQueryable<Codextn> GetRequiredFields(string part);
        IQueryable<Codextn> GetUploadList();
        IQueryable<Codextn> GetItemCodeRequestUploadList();
        IQueryable<Codextn> GetIssuanceYears();
        ValueTask<bool> IsValidMastCodeIdAsync(string mastCode, Guid? id);
        ValueTask<bool> IsValidCodeDescAsync(string mainCode, string description);

        ValueTask<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date);
        ValueTask<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date);
        ValueTask<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date);

        ValueTask<Codextn> CreateAsync(Codextn model, string user, DateTime date);
        ValueTask<Codextn> UpdateAsync(Codextn model, string user, DateTime date);
        ValueTask<Codextn> DeleteAsync(Codextn model, string user, DateTime date);        
    }

    public class CodextnService : BaseValidator, ICodextnService
    {
        protected readonly AppManEntities _db;        
        protected readonly GetDisplayNameDelegate _getDisplayName;
        protected readonly IExceptionService<Codextn> _exceptionService;
        protected readonly IExceptionService<CodextnVM> _vmExceptionService;
        private readonly IUserService _userService;
        
        public CodextnService(AppManEntities db, IExceptionService<Codextn> exceptionService, 
            IExceptionService<CodextnVM> vmExceptionService,
            IUserService userService)
        {
            _db = db;
            _exceptionService = exceptionService;
            _vmExceptionService = vmExceptionService;
            _getDisplayName = Utility.GetDisplayName<Codextn>;
            _userService = userService;            
        }

        public async ValueTask<IQueryable<Codextn>> GetUserDepartmentsAsync(string userId)
        {
            var IsAdmin = await _userService.IsAdminAsync(userId);
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "LOCATIONS"
                //&& w.Desc3 != "N"
                && w.Code.Substring(w.Code.Length - 2) == "00"
                && (IsAdmin 
                        || w.DepartmentUsers.Any(a => a.UserId == userId)
                        || _db.Codextns.Any(a => a.CodeMast.Code == "LOCATIONS" 
                                && a.Code.Substring(0, 2) == w.Code.Substring(0, 2) 
                                && a.DepartmentUsers.Any(b => b.UserId == userId))
                    )
                ).AsNoTracking().OrderBy(o => o.Description);
            return data;
        }

        public IQueryable<Codextn> GetRequiredFields(string part)
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "REQUIRED-FIELDS" && w.Code.StartsWith(part)).AsNoTracking().OrderBy(o => o.Code);
            return data;
        }

        public IQueryable<Codextn> GetUploadList()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "UPLOAD-LIST").AsNoTracking().OrderBy(o => o.Description);
            return data;
        }
        public IQueryable<Codextn> GetItemCodeRequestUploadList()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "ITEM-UPLOAD-LIST").AsNoTracking().OrderBy(o => o.Description);
            return data;
        }

        public IQueryable<Codextn> GetIssuanceYears()
        {
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUANCE-YEAR").AsNoTracking().OrderBy(o => o.Description);
            return data;
        }

        public IQueryable<CodextnVM> GetByMastCode(string mastCode)
        {            
            var data = _db.Codextns.Where(w => w.CodeMast.Code == mastCode).AsNoTracking()
                .Select(s => new CodextnVM
                {
                    Id = s.Id,
                    Code = s.Code,
                    MastId = s.MastId,
                    Description = s.Description,
                    Desc2 = s.Desc2,
                    Desc3 = s.Desc3,
                    Desc4 = s.Desc4,
                    Desc5 = s.Desc5,
                    CodeHdg = s.CodeMast.CodeHdg,
                    Desc1Hdg = s.CodeMast.Desc1Hdg,
                    Desc2Hdg = s.CodeMast.Desc2Hdg,
                    Desc3Hdg = s.CodeMast.Desc3Hdg,
                    Desc4Hdg = s.CodeMast.Desc4Hdg,
                    Desc5Hdg = s.CodeMast.Desc5Hdg,
                    Pad = s.Code.Trim().Substring(s.Code.Trim().Length - 2),
                    Padding = s.Code.Trim().Substring(s.Code.Trim().Length - 2)  == "00" ? 0 : 30                    
                });
            return data;
        }

        public virtual IQueryable<CodextnVM> GetByMastId(Guid mastId)
        {            
            var data = _db.Codextns.Where(w => w.MastId == mastId)
                .Select(s => new CodextnVM
                {
                    Id = s.Id,
                    Code = s.Code,
                    MastId = s.MastId,
                    Description = s.Description,
                    Desc2 = s.Desc2,
                    Desc3 = s.Desc3,
                    Desc4 = s.Desc4,
                    Desc5 = s.Desc5,
                    CodeHdg = s.CodeMast.CodeHdg,
                    Desc1Hdg = s.CodeMast.Desc1Hdg,
                    Desc2Hdg = s.CodeMast.Desc2Hdg,
                    Desc3Hdg = s.CodeMast.Desc3Hdg,
                    Desc4Hdg = s.CodeMast.Desc4Hdg,
                    Desc5Hdg = s.CodeMast.Desc5Hdg,
                    Pad = s.Code.Trim().Substring(s.Code.Trim().Length - 2),
                    Padding = s.Code.Trim().Substring(s.Code.Trim().Length - 2) == "00" ? 0 : 30
                });
            return data;
        }

        public async ValueTask<CodextnVM> GetByIdAsync(Guid? id)
        {
            var data = await _db.Codextns.Where(w => w.Id == id)
                .Select(s => new CodextnVM
                {
                    Id = s.Id,
                    Code = s.Code,
                    MastId = s.MastId,
                    Description = s.Description,
                    Desc2 = s.Desc2,
                    Desc3 = s.Desc3,
                    Desc4 = s.Desc4,
                    Desc5 = s.Desc5,
                    CodeHdg = s.CodeMast.CodeHdg,
                    Desc1Hdg = s.CodeMast.Desc1Hdg,
                    Desc2Hdg = s.CodeMast.Desc2Hdg,
                    Desc3Hdg = s.CodeMast.Desc3Hdg,
                    Desc4Hdg = s.CodeMast.Desc4Hdg,
                    Desc5Hdg = s.CodeMast.Desc5Hdg,
                    Pad = s.Code.Trim().Substring(s.Code.Trim().Length - 2),
                    Padding = s.Code.Trim().Substring(s.Code.Trim().Length - 2) == "00" ? 0 : 30
                }).FirstOrDefaultAsync();
            return data;
        }

        public bool IsValidCodeDesc(string mainCode, string description)
        {
            return _db.Codextns.Any(a => a.CodeMast.Code == mainCode && a.Description == description);
        }

        public async ValueTask<bool> IsValidCodeDescAsync(string mainCode, string description)
        {
            return await _db.Codextns.AnyAsync(a => a.CodeMast.Code == mainCode && a.Description == description);
        }

        public bool IsValidMastCodeCode(string mastCode, string code)
        {
            return _db.Codextns.Any(a => a.CodeMast.Code == mastCode && a.Code == code);
        }

        public bool IsValidMastCodeId(string mastCode, Guid? id)
        {
            return _db.Codextns.Any(a => a.CodeMast.Code == mastCode && a.Id == id);
        }

        public async ValueTask<bool> IsValidMastCodeIdAsync(string mastCode, Guid? id)
        {
            return await _db.Codextns.AnyAsync(a => a.CodeMast.Code == mastCode && a.Id == id);
        }

        public ValueTask<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await CreateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await UpdateAsync((Codextn)model, user, date);
            return model;
        });

        public ValueTask<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date) => _vmExceptionService.TryCatch(async () =>
        {
            await DeleteAsync((Codextn)model, user, date);
            return model;
        });

        public virtual ValueTask<Codextn> CreateAsync(Codextn model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            model.Code = string.IsNullOrWhiteSpace(model.Code) ? NextCode(model.MastId) : model.Code;

            var entity = new Codextn()
            {
                Id = model.Id,
                MastId = model.MastId,
                Code = model.Code,
                Description = model.Description,
                Desc2 = model.Desc2,
                Desc3 = model.Desc3,
                Desc4 = model.Desc4,
                Desc5 = model.Desc5,
                InsertedBy = model.InsertedBy,
                InsertedDt = model.InsertedDt,
                UpdatedBy = model.UpdatedBy,
                UpdatedDt = model.UpdatedDt
            };

            _db.Codextns.Add(entity);
            await _db.SaveChangesAsync();

            return model;
        });

        public virtual ValueTask<Codextn> UpdateAsync(Codextn model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            var entity = _db.Codextns.Find(model.Id);

            if (entity != null)
            {
                model.UpdatedBy = user;
                model.UpdatedDt = date;

                model.Code = string.IsNullOrWhiteSpace(model.Code) ? NextCode(model.MastId) : model.Code;

                entity.Code = model.Code;
                entity.Description = model.Description;
                entity.Desc2 = model.Desc2;
                entity.Desc3 = model.Desc3;
                entity.Desc4 = model.Desc4;
                entity.Desc5 = model.Desc5;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                _db.Codextns.Attach(entity);
                _db.Entry(entity).State = EntityState.Modified;
                await _db.SaveChangesAsync();
            }
            return model;
        });
        
        public virtual ValueTask<Codextn> DeleteAsync(Codextn model, string user, DateTime date) => _exceptionService.TryCatch(async () =>
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            Codextn entity = await _db.Codextns.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.Codextns.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.Codextns.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        });

        private string NextCode(Guid mastId)
        {
            //var data = db.Codextns.Where(w => w.MastId == mastId && IsNumeric(w.Code)).OrderByDescending(o => o.Code).FirstOrDefault();
            var data = _db.Database.SqlQuery<Codextn>("Select Top 1 * From Codextn Where MastId = {0} Order by Code Desc", mastId).FirstOrDefault();
            if (data == null)
            {
                return "0001";
            }
            else
            {
                var sequence = (int.Parse(data.Code) + 1).ToString();
                return sequence.PadLeft(4, '0');
            }
        }

        static bool IsNumeric(string value)
        {
            return int.TryParse(value, out _);
        }
    }
}