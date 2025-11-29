using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Services
{
    public interface ICustodianDeptUploadService
    {
        IQueryable<Upload> GetAll();
        IQueryable<Upload> GetAllByImageId(Guid? imageId);
        IQueryable<Upload> GetAllDeptUploads(string deptCode);
        ValueTask<Upload> GetByIdAsync(Guid? id);
        ValueTask<ActionResult> GetUploadedFileAsync(Guid? id);
        ValueTask<ActionResult> GetImageIdFirstUploadAsync(Guid? id);
        string GetDirectoryPath();

        ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string deptCode, string user, DateTime date);
        ValueTask<Upload> UpdateAsync(Upload model, string user, DateTime date);
        ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date);

        byte[] DownloadFile(string fileName);
        ValueTask<ActionResult> GetThumbnailPhotoByIdAsync(Guid? imageId);
        ValueTask<ActionResult> GetLargePhotoByIdAsync(Guid? imageId);

        Task<bool> IsFileNameExistAsync(string fileName);
        ValueTask CopyAsync(Guid? imageId, string directoryPath, string user, DateTime date);

        ICustodianDeptUploadService Create(string subDir);
    }

    public class CustodianDeptUploadService : ICustodianDeptUploadService
    {
        protected string _subDir = "";
        protected string _directory = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["UPLOAD_URL"].ToString()).DataSource;
        protected string[] _supportedTypes = new[] { "xlsx", "xls", "docx", "doc", "pdf", "jpg", "jpeg", "png" };
        protected AppManEntities _db;
        protected IAppManEntitiesFactory _contextFactory;

        public CustodianDeptUploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _directory += _subDir + (string.IsNullOrWhiteSpace(_subDir) ? "" : "/");
        }

        public CustodianDeptUploadService(AppManEntities db, string subDir)
        {
            _db = db;
            _subDir = subDir;
            _directory += subDir + "/";
        }

        public ICustodianDeptUploadService Create(string subDir)
        {
            return new CustodianDeptUploadService(_db, subDir);
        }

        public string GetDirectoryPath()
        {
            return _directory;
        }

        public IQueryable<Upload> GetAll()
        {
            var subDir = "/" + _subDir + "/";
            var data = _db.Uploads.AsNoTracking().Where(w => w.VirtualDirectory.EndsWith(subDir)).AsQueryable();
            return data;
        }

        public IQueryable<Upload> GetAllByImageId(Guid? imageId)
        {
            var subDir = "/" + _subDir + "/";
            var data = _db.Uploads.AsNoTracking().Where(w => w.ImageId == imageId && w.VirtualDirectory.EndsWith(subDir));
            return data;
        }

        public IQueryable<Upload> GetAllDeptUploads(string deptCode)
        {
            var subDir = "/" + _subDir + "/";
            var data = _db.Uploads.AsNoTracking().Where(w => w.VirtualDirectory.EndsWith(subDir) && w.FileName.StartsWith(deptCode));
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

        public async ValueTask<ActionResult> GetImageIdFirstUploadAsync(Guid? imageId)
        {
            var file = await _db.Uploads.Where(w => w.ImageId == imageId).OrderBy(o => o.InsertedDt).FirstOrDefaultAsync();
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

        public async ValueTask<ActionResult> GetThumbnailPhotoByIdAsync(Guid? imageId)
        {
            var upload = await _db.Uploads.FirstOrDefaultAsync(p => p.ImageId == imageId && p.Description == "Thumbnail");
            if (upload == null)
            {
                return null;
            }

            var fileName = upload.FileName;
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

        public async ValueTask<ActionResult> GetLargePhotoByIdAsync(Guid? imageId)
        {
            var upload = await _db.Uploads.FirstOrDefaultAsync(p => p.ImageId == imageId && p.Description == "Large");
            if (upload == null)
            {
                return null;
            }

            var fileName = upload.FileName;
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

        public virtual async ValueTask<Upload> UploadAsyncOld(IEnumerable<HttpPostedFileBase> files, Upload model, string deptCode, string user, DateTime date)
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
                if (file.ContentLength > 1024 * 1024 * model.FileSize)
                {
                    throw new InvalidValueException($"The size of the file should not exceeed {model.FileSize} MB");
                }
                var fileExt = System.IO.Path.GetExtension(file.FileName).Substring(1).ToLower();
                if (!_supportedTypes.Contains(fileExt))
                {
                    throw new InvalidValueException("Invalid file type.");
                }

                model.Id = Guid.NewGuid();
                var fileName = $"{deptCode}-{file.FileName}";
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

        public virtual async ValueTask<Upload> UploadAsync(
            IEnumerable<HttpPostedFileBase> files,
            Upload model,
            string deptCode,
            string user,
            DateTime date)
        {
            if (files == null || !files.Any())
                throw new RecordNotFoundException("No files to upload!");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new InvalidValueException("Description is required.");

            // Validate all first
            foreach (var file in files)
            {
                string originalName = Path.GetFileName(file.FileName);

                if (file.ContentLength > (1024 * 1024 * model.FileSize))
                    throw new InvalidValueException($"File '{originalName}' exceeds the allowed size of {model.FileSize} MB.");

                string ext = Path.GetExtension(originalName)?.TrimStart('.').ToLower();
                if (string.IsNullOrEmpty(ext) || !_supportedTypes.Contains(ext))
                    throw new InvalidValueException($"Invalid file type: {originalName}");
            }

            using (var ctx = new AppManEntities())
            {
                foreach (var file in files)
                {
                    model.Id = Guid.NewGuid();
                    var fileName = $"{deptCode}-{file.FileName}";

                    if (await ctx.Uploads.AnyAsync(a => a.FileName.ToUpper() == fileName.ToUpper()))
                    {
                        throw new RecordAlreadyExistsException($"File name '{fileName}' already exists.");
                    }

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

                    ctx.Uploads.Add(entity);
                    await ctx.SaveChangesAsync();
                }                
            }

            return model; 
        }

        public byte[] DownloadFile(string fileName)
        {
            try
            {
                // Define the file path
                string filePath = Path.Combine(_directory, fileName);

                // Check if the file exists
                if (!System.IO.File.Exists(filePath))
                {
                    throw new FileNotFoundException("File not found.", fileName);
                }

                // Read the file as a byte array
                return System.IO.File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                // Log the exception and rethrow it or return null
                throw new InvalidOperationException("Error downloading file: " + ex.Message);
            }
        }

        public virtual async ValueTask<Upload> UpdateAsync(Upload model, string user, DateTime date)
        {
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            var entity = await _db.Uploads.FindAsync(model.Id);

            //entity.ImageId = model.ImageId;
            //entity.FileName = model.FileName;
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

        public async ValueTask CopyAsync(Guid? imageId, string directoryPath, string user, DateTime date)
        {
            var uploads = await _db.Uploads.Where(w => w.ImageId == imageId).ToListAsync();
            if (uploads.Any())
            {
                foreach (var upload in uploads)
                {
                    // Pattern: match a GUID
                    var fileName = Regex.Replace(upload.FileName, @"^[0-9a-fA-F\-]{36}", "");
                    var resultPath = upload.VirtualDirectory.Substring(upload.VirtualDirectory.IndexOf("UPLOADS", StringComparison.OrdinalIgnoreCase));
                    var destinationPath = directoryPath + upload.Id.ToString() + fileName;
                    if (!File.Exists(directoryPath))
                    {
                        var uploadId = Guid.NewGuid();
                        var entity = new Upload()
                        {
                            Id = uploadId,
                            ImageId = imageId,
                            FileName = uploadId + fileName,
                            Description = upload.Description,
                            VirtualDirectory = directoryPath,
                            InsertedBy = user,
                            InsertedDt = date,
                            UpdatedBy = user,
                            UpdatedDt = date
                        };

                        _db.Uploads.Add(entity);
                        await _db.SaveChangesAsync();

                        int index = upload.VirtualDirectory.IndexOf("UPLOADS", StringComparison.OrdinalIgnoreCase);
                        var sourcePath = directoryPath + upload.VirtualDirectory.Substring(index);

                        File.Copy(sourcePath, destinationPath);
                    }
                }
            }
        }
    }
}