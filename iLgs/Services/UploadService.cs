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
    public interface IUploadService
    {
        IQueryable<Upload> GetAll();
        IQueryable<Upload> GetAllByImageId(Guid? imageId);
        ValueTask<Upload> GetByIdAsync(Guid? id);
        ValueTask<ActionResult> GetUploadedFileAsync(Guid? id);

        ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date);
        ValueTask<Upload> UpdateAsync(Upload model, string user, DateTime date);
        ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date);

        Task<bool> IsFileNameExistAsync(string fileName);
    }

    public class UploadService : IUploadService
    {
        protected string _subDir = "";
        protected string _directory = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["UPLOAD_URL"].ToString()).DataSource;
        protected string[] _supportedTypes = new[] { "xlsx", "xls", "docx", "doc", "pdf", "jpg", "jpeg", "png" };
        protected AppManEntities _db;

        public UploadService(AppManEntities db)
        {
            _db = db;
            _directory += _subDir + (string.IsNullOrWhiteSpace(_subDir) ? "" : "/");
        }

        public UploadService(AppManEntities db, string subDir)
        {
            _db = db;
            _subDir = subDir;
            _directory += subDir + "/";
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

        public async ValueTask<Upload> GetByIdAsync(Guid? id)
        {
            return await _db.Uploads.FindAsync(id);
        }

        public async ValueTask<ActionResult> GetUploadedFileAsync(Guid? id)
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

        
        public virtual async ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {            
            if (files == null || !files.Any())
            {
                throw new RecordNotFoundException("No files to upload!");
            }

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            foreach (var file in files)
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

                model.Id = Guid.NewGuid();
                var fileName = model.Id + "-" + file.FileName;
                var physicalPath = Path.Combine(_directory, fileName);
                var hostaddress = HttpContext.Current.Request.UserHostAddress;
                file.SaveAs(physicalPath);

                model.InsertedBy = user;
                model.UpdatedBy = user;
                model.InsertedDt = date;
                model.UpdatedDt = date;
                model.FileName = Path.GetFileName(fileName);
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

        public virtual async ValueTask<Upload> UpdateAsync(Upload model, string user, DateTime date)
        {            
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

        public virtual async ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date)
        {
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