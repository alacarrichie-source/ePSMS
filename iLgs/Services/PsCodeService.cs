using iLgs.Models;
using iLgs.Services.Interfaces;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public class PsCodeService : IPsCodeService
    {
        private readonly AppManEntities db = new AppManEntities();
        private IDirectoryService directoryService;
        private string imageDirectory;
        public PsCodeService(AppManEntities db)
        {
            this.db = db;
            this.directoryService = new DirectoryService();
            this.imageDirectory = directoryService.GetItemImageDirectory();
        }

        public IQueryable<PsCode> GetAll()
        {
            var data = db.PsCodes.AsQueryable();
            return data;
        }
        public IQueryable<PsCodeVM> GetMaintenanceView()
        {
            var data = db.PsCodes
                .Select(s => new PsCodeVM
                {
                    Id = s.Id,
                    PsType = s.PsType,
                    PsNo = s.PsNo,
                    ItemName = s.ItemName,
                    UnitMeas = s.UnitMeas,
                    ImageUrl = this.imageDirectory + (db.Uploads.Any(a => a.ImageId == s.Id) ?
                        db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : "")
                });
            return data;
        }

        public async Task<PsCode> GetByIdAsync(Guid psId)
        {
            return await db.PsCodes.FindAsync(psId);
        }

        public async Task<bool> GetAnyPsNoAsync(Guid psId, string psNo)
        {
            return await db.PsCodes.AnyAsync(a => a.Id != psId && a.PsNo == psNo);
        }

        public async Task<PsCode> GetByPsNoAsync(string psNo)
        {
            return await db.PsCodes.Where(w => w.PsNo == psNo).FirstOrDefaultAsync();
        }        

        public async Task<PsCode> CreateAsync(PsCode model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            model.ItemDescription = model.ItemDescription ?? "";
            model.ReorderPoint = model.ReorderPoint ?? 0;
            model.DaysToConsume = model.DaysToConsume ?? 0;

            db.PsCodes.Add(model);
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<PsCode> UpdateAsync(PsCode model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            model.UnitMeas = model.UnitMeas;

            model.PsType = model.PsType.ToUpper();
            model.ItemDescription = model.ItemDescription ?? "";
            model.ReorderPoint = model.ReorderPoint ?? 0;
            model.DaysToConsume = model.DaysToConsume ?? 0;

            db.PsCodes.Attach(model);
            db.Entry(model).State = EntityState.Modified;
            await db.SaveChangesAsync();

            return model;
        }

        public async Task<PsCode> DeleteAsync(PsCode model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await db.PsCodes.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            db.PsCodes.Attach(entity);
            db.Entry(entity).State = EntityState.Modified;
            await db.SaveChangesAsync();

            db.PsCodes.Remove(entity);
            db.Entry(entity).State = EntityState.Deleted;
            await db.SaveChangesAsync();

            return model;
        }        

        public string GetImageUrl(Guid imageId)
        {
            string imageUrl = this.imageDirectory + 
                (db.Uploads.Any(a => a.ImageId == imageId) ? 
                    db.Uploads.FirstOrDefault(f => f.ImageId == imageId).FileName : "");
            return imageUrl;
        }
    }
}