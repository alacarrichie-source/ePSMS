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
        private readonly AppManEntities _db = new AppManEntities();
        private readonly IRisItemService _risItemService;
        //private IDirectoryService directoryService;
        //private string imageDirectory;
        public PsCodeService(AppManEntities db)
        {
            _db = db;
            _risItemService = new RisItemService(db);
            //this.directoryService = new DirectoryService();
            //this.imageDirectory = directoryService.GetItemImageDirectory();
        }

        public IQueryable<PsCode> GetAll()
        {
            var data = _db.PsCodes.AsQueryable();
            return data;
        }
        public IQueryable<PsCodeVM> GetMaintenanceView()
        {
            var data = _db.PsCodes
                .Select(s => new PsCodeVM
                {
                    Id = s.Id,
                    ItemCodeId = s.ItemCodeId,
                    ItemCode = s.ItemCode.Code,
                    PsType = s.PsType,
                    PsNo = s.PsNo,
                    ItemName = s.ItemName,
                    UnitMeas = s.UnitMeas,
                    FileName = _db.Uploads.Any(a => a.ImageId == s.Id) ? _db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : ""
                    //ImageUrl = this.imageDirectory + (db.Uploads.Any(a => a.ImageId == s.Id) ?
                    //    db.Uploads.FirstOrDefault(f => f.ImageId == s.Id).FileName : "")
                });
            return data;
        }

        public async Task<PsCode> GetByIdAsync(Guid psId)
        {
            return await _db.PsCodes.FindAsync(psId);
        }

        public async Task<bool> GetAnyPsNoAsync(Guid psId, string psNo)
        {
            return await _db.PsCodes.AnyAsync(a => a.Id != psId && a.PsNo == psNo);
        }

        public async Task<PsCode> GetByPsNoAsync(string psNo)
        {
            return await _db.PsCodes.Where(w => w.PsNo == psNo).FirstOrDefaultAsync();
        }        

        public async Task<PsCode> CreateAsync(PsCode model, string user, DateTime date)
        {
            model.Id = Guid.NewGuid();
            model.InsertedBy = user;
            model.InsertedDt = date;
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            model.PsNo = PsNo(model);
            model.ItemDescription = model.ItemDescription ?? "";
            model.ReorderPoint = model.ReorderPoint ?? 0;
            model.DaysToConsume = model.DaysToConsume ?? 0;

            _db.PsCodes.Add(model);
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<PsCode> UpdateAsync(PsCode model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;
            model.UnitMeas = model.UnitMeas;

            model.PsNo = PsNo(model);
            model.PsType = model.PsType.ToUpper();
            model.ItemDescription = model.ItemDescription ?? "";
            model.ReorderPoint = model.ReorderPoint ?? 0;
            model.DaysToConsume = model.DaysToConsume ?? 0;

            _db.PsCodes.Attach(model);
            _db.Entry(model).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<PsCode> DeleteAsync(PsCode model, string user, DateTime date)
        {

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.PsCodes.FindAsync(model.Id);

            entity.UpdatedBy = user;
            entity.UpdatedDt = date;

            _db.PsCodes.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.PsCodes.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            return model;
        }        

        private string PsNo(PsCode model)
        {
            var itemCode = model.ItemCode.Code;
            var itemName = model.ItemName;
            return _risItemService.PsNoDisplay(itemCode, itemName);
        }

        //public string GetImageUrl(Guid imageId)
        //{
        //    string imageUrl = this.imageDirectory + 
        //        (db.Uploads.Any(a => a.ImageId == imageId) ? 
        //            db.Uploads.FirstOrDefault(f => f.ImageId == imageId).FileName : "");
        //    return imageUrl;
        //}
    }
}