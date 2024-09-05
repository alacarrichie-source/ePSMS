using iLgs.Exceptions;
using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Services
{
    public interface IParIcsUploadService
    {
        IQueryable<Upload> GetAll();
        IQueryable<Upload> GetAllByImageId(Guid? imageId);
        Task<Upload> GetByIdAsync(Guid? id);
        Task<ActionResult> GetUploadedFileAsync(Guid? id);

        Task<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date);
        Task<Upload> UpdateAsync(Upload model, string user, DateTime date);
        Task<Upload> DeleteAsync(Upload model, string user, DateTime date);

        Task<bool> IsFileNameExistAsync(string fileName);        
    }

    public class ParIcsUploadService : IParIcsUploadService
    {
        private string _directory = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["UPLOAD_URL"].ToString()).DataSource;
        private string[] _supportedTypes = new[] { "pdf", "jpg", "jpeg", "png" };
        private readonly AppManEntities _db = new AppManEntities();

        public ParIcsUploadService(AppManEntities db)
        {
            _db = db;
        }
        public IQueryable<Upload> GetAll()
        {
            var data = _db.Uploads.AsQueryable();
            return data;
        }

        public IQueryable<Upload> GetAllByImageId(Guid? imageId)
        {
            var data = _db.Uploads.Where(w => w.ImageId == imageId);                
            return data;
        }

        public async Task<Upload> GetByIdAsync(Guid? id)
        {
            return await _db.Uploads.FindAsync(id);
        }

        public async Task<ActionResult> GetUploadedFileAsync(Guid? id)
        {            
            var file = await GetByIdAsync(id);
            var fileName = file.FileName;
            
            var physicalPath = Path.Combine(_directory, fileName);
            var fileExt = Path.GetExtension(fileName).Substring(1).ToLower();

            if (fileExt == "pdf") 
            {
                return new FilePathResult(physicalPath, "application/pdf");
            }
            else
            {                
                return new FilePathResult(physicalPath, "image/jpg");
            }
        }

        private async Task<bool> IsPostedAsync(Guid? imageId)
        {
            var result = await _db.PsCardItems.Where(w => w.GroupId == imageId).AnyAsync(a => a.ParPostedBy != "" && a.ParPostedBy != null);
            return result;
        }

        public async Task<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {
            if (await IsPostedAsync(model.ImageId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            if (files == null || !files.Any())
            {
                throw new RecordNotFoundException("No files to upload!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            foreach(var file in files)
            {
                var fileName = Path.GetFileName(file.FileName);
                if (await IsFileNameExistAsync(fileName))
                {
                    throw new RecordAlreadyExistsException($"File name {fileName} already exists!");
                }
            }

            foreach (var file in files)
            {
                
                if (file.ContentLength > (10240) * 100 * model.FileSize)
                {
                    throw new InvalidValueException($"The size of the file should not exceeed {model.FileSize} MB");                    
                }
                var fileExt = System.IO.Path.GetExtension(file.FileName).Substring(1).ToLower();
                if (!_supportedTypes.Contains(fileExt))
                {
                    throw new InvalidValueException("Invalid file type.");                    
                }

                //var physicalPath = Path.Combine(HttpContext.Current.Server.MapPath(_directory), file.FileName);
                var physicalPath = Path.Combine(_directory, file.FileName);
                //var appPathDirectory = System.IO.Path.GetDirectoryName(physicalPath);
                var hostaddress = HttpContext.Current.Request.UserHostAddress;
                file.SaveAs(physicalPath);

                model.Id = Guid.NewGuid();
                model.InsertedBy = user;
                model.UpdatedBy = user;
                model.InsertedDt = date;
                model.UpdatedDt = date;
                model.FileName = Path.GetFileName(file.FileName);
                model.VirtualDirectory = _directory;

                var entity = new Upload()
                {
                    Id = model.Id,
                    ImageId = model.ImageId,
                    FileName = model.FileName,
                    Description = model.Description,
                    ServerIpAddress = model.ServerIpAddress,
                    VirtualDirectory = model.VirtualDirectory,
                    InsertedBy = model.InsertedBy,
                    InsertedDt = model.InsertedDt,
                    UpdatedBy = model.UpdatedBy,
                    UpdatedDt = model.UpdatedDt
                };

                _db.Uploads.Add(entity);
                await _db.SaveChangesAsync();
                
            }
            return model;
        }

        public async Task<Upload> UpdateAsync(Upload model, string user, DateTime date)
        {
            if (await IsPostedAsync(model.ImageId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Uploads.FindAsync(model.Id);

            entity.ImageId = model.ImageId;
            entity.FileName = model.FileName;
            entity.Description = model.Description;
            entity.ServerIpAddress = model.ServerIpAddress;
            entity.VirtualDirectory = model.VirtualDirectory;
            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.Uploads.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            return model;
        }

        public async Task<Upload> DeleteAsync(Upload model, string user, DateTime date)
        {
            if (await IsPostedAsync(model.ImageId))
            {
                throw new RecordAlreadyPostedException("Record is already posted, cannot update!");
            }

            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Uploads.FindAsync(model.Id);

            entity.UpdatedBy = model.UpdatedBy;
            entity.UpdatedDt = model.UpdatedDt;

            _db.Uploads.Attach(entity);
            _db.Entry(entity).State = EntityState.Modified;
            await _db.SaveChangesAsync();

            _db.Uploads.Remove(entity);
            _db.Entry(entity).State = EntityState.Deleted;
            await _db.SaveChangesAsync();

            var directory = model.VirtualDirectory;
            var fileName = model.FileName;

            //var path = HttpContext.Current.Server.MapPath(directory);            
            var physicalPath = Path.Combine(_directory, fileName);
            var appPathDirectory = Path.GetDirectoryName(physicalPath);

            if (File.Exists(physicalPath))
            {
                // The files are not actually removed in this demo
                File.Delete(physicalPath);
            }

            return model;
        }

        public async Task<bool> IsFileNameExistAsync(string fileName)
        {
            return await _db.Uploads.AnyAsync(a => a.FileName.ToUpper() == fileName.ToUpper());
        }
    }
}