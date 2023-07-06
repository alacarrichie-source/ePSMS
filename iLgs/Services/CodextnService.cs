using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iLgs.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace iLgs.Services
{
    public class CodextnService : ICodextnService
    {
        private readonly AppManEntities db = new AppManEntities();
        public CodextnService(AppManEntities db)
        {
            this.db = db;
        }

        public async Task<CodextnVM> CreateAsync(CodextnVM model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;

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

            db.Codextns.Add(entity);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<CodextnVM> DeleteAsync(CodextnVM model, string user, DateTime date)
        {            
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            Codextn entity = await db.Codextns.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            db.Codextns.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.Codextns.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }

        public IQueryable<CodextnVM> GetByMastCode(string mastCode)
        {            
            var data = db.Codextns.Where(w => w.CodeMast.Code == mastCode)
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
                    Desc5Hdg = s.CodeMast.Desc5Hdg
                });
            return data;
        }

        public IQueryable<CodextnVM> GetByMastId(Guid mastId)
        {            
            var data = db.Codextns.Where(w => w.MastId == mastId)
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
                    Desc5Hdg = s.CodeMast.Desc5Hdg
                });
            return data;
        }

        public async Task<CodextnVM> UpdateAsync(CodextnVM model, string user, DateTime date)
        {
            var entity = db.Codextns.Find(model.Id);

            if (entity != null)
            {
                model.UpdatedBy = user;
                model.UpdatedDt = date;

                entity.Code = model.Code;
                entity.Description = model.Description;
                entity.Desc2 = model.Desc2;
                entity.Desc3 = model.Desc3;
                entity.Desc4 = model.Desc4;
                entity.Desc5 = model.Desc5;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                db.Codextns.Attach(entity);
                db.Entry(entity).State = EntityState.Modified;
                await db.SaveChangesAsync();
            }
            return model;
        }
    }
}