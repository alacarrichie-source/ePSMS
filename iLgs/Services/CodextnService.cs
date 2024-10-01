using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iLgs.Models;
using System.Threading.Tasks;
using System.Data.Entity;
using iLgs.Controllers;

namespace iLgs.Services
{
    public interface ICodextnService
    {
        IQueryable<CodextnVM> GetByMastCode(string mastCode);
        IQueryable<CodextnVM> GetByMastId(Guid mastId);
        bool IsValidMastCodeId(string mastCode, Guid? id);
        bool IsValidMastCodeCode(string mastCode, string code);
        bool IsValidCodeDesc(string mainCode, string description);
        ValueTask<IQueryable<Codextn>> GetUserDepartmentsAsync(string userId);
        ValueTask<bool> IsValidMastCodeIdAsync(string mastCode, Guid? id);
        ValueTask<bool> IsValidCodeDescAsync(string mainCode, string description);
        Task<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date);
        Task<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date);
        Task<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date);
    }

    public class CodextnService : ICodextnService
    {
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IUserService _userService;

        public CodextnService(AppManEntities db)
        {
            _db = db;
            _userService = new UserService(db);
        }

        public async ValueTask<IQueryable<Codextn>> GetUserDepartmentsAsync(string userId)
        {
            var IsAdmin = await _userService.IsAdmin(userId);
            var data = _db.Codextns.Where(w => w.CodeMast.Code == "DEPARTMENTS"
                && (IsAdmin || w.DepartmentUsers.Any(a => a.UserId == userId)));
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

        public IQueryable<CodextnVM> GetByMastId(Guid mastId)
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

        public async Task<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date)
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
        }
        public async Task<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date)
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
        }

        public async Task<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date)
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
        }

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