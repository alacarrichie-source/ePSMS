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
    public interface IUploadService
    {
        IQueryable<Upload> GetAll();
        IQueryable<Upload> GetAllByImageId(Guid? imageId);
        IQueryable<Upload> GetAllCustodianUploads(int? forYear, Guid? deptId, Guid? locationId);
        ValueTask<Upload> GetByIdAsync(Guid? id);
        ValueTask<ActionResult> GetUploadedFileAsync(Guid? id);
        ValueTask<ActionResult> GetImageIdFirstUploadAsync(Guid? id);
        string GetDirectoryPath();

        ValueTask<Upload> UploadAsync(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date);
        ValueTask<Upload> UpdateAsync(Upload model, string user, DateTime date);
        ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date);

        byte[] DownloadFile(string fileName);
        ValueTask<ActionResult> GetThumbnailPhotoByIdAsync(Guid? imageId);
        ValueTask<ActionResult> GetLargePhotoByIdAsync(Guid? imageId);

        Task<bool> IsFileNameExistAsync(string fileName);
        ValueTask CopyAsync(Guid? imageId, string directoryPath, string user, DateTime date);
    }

    public class UploadService : IUploadService
    {
        protected string _subDir = "";
        protected string _directory = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["UPLOAD_URL"].ToString()).DataSource;
        protected string[] _supportedTypes = new[] { "xlsx", "xls", "docx", "doc", "pdf", "jpg", "jpeg", "png" };
        protected AppManEntities _db;
        protected IAppManEntitiesFactory _contextFactory;

        public UploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _directory += _subDir + (string.IsNullOrWhiteSpace(_subDir) ? "" : "/");
        }

        public UploadService(AppManEntities db, IAppManEntitiesFactory appManEntitiesFactory, string subDir)
        {
            _db = db;
            _contextFactory = appManEntitiesFactory;
            _subDir = subDir;
            _directory += subDir + "/";
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

        public IQueryable<Upload> GetAllCustodianUploads(int? forYear, Guid? deptId, Guid? locationId)
        {
            var data = _db.Uploads.AsNoTracking()
                .Where(w => _db.CustodianReports.Any(a => a.AsOf.Value.Year == forYear && a.DeptId == deptId 
                    && a.CustodianReportItems.Any(b => b.Id == w.ImageId && (locationId == null || b.LocationId == locationId))));           
            return data;
        }

        public async ValueTask<Upload> GetByIdAsync(Guid? id)
        {
            return await _db.Uploads.FindAsync(id);
        }

        public async ValueTask<ActionResult> GetUploadedFileAsyncOld(Guid? id)
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

            //// Read the file bytes
            //byte[] fileBytes = System.IO.File.ReadAllBytes(physicalPath);

            //// Get MIME type based on the file extension
            //string mimeType = MimeMapping.GetMimeMapping(fileName);

            //// Return the file content to be viewed in the browser
            //return File(fileBytes, mimeType);
        }

        public async ValueTask<ActionResult> GetUploadedFileAsync(Guid? id)
        {
            var file = await GetByIdAsync(id);
            var fileName = file.FileName;

            var physicalPath = Path.Combine(_directory, fileName);
            var ext = Path.GetExtension(fileName).ToLower();

            string mime;

            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    mime = "image/jpeg";
                    break;

                case ".png":
                    mime = "image/png";
                    break;

                case ".gif":
                    mime = "image/gif";
                    break;

                case ".bmp":
                    mime = "image/bmp";
                    break;

                case ".webp":
                    mime = "image/webp";
                    break;

                case ".pdf":
                    mime = "application/pdf";
                    break;

                default:
                    mime = "application/octet-stream";
                    break;
            }

            return new FilePathResult(physicalPath, mime);
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

            //// Read the file bytes
            //byte[] fileBytes = System.IO.File.ReadAllBytes(physicalPath);

            //// Get MIME type based on the file extension
            //string mimeType = MimeMapping.GetMimeMapping(fileName);

            //// Return the file content to be viewed in the browser
            //return File(fileBytes, mimeType);
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


        public virtual async ValueTask<Upload> UploadAsyncOld(IEnumerable<HttpPostedFileBase> files, Upload model, string user, DateTime date)
        {            
            if (files == null || !files.Any())
            {
                throw new RecordNotFoundException("No files to upload!");
            }

            ValidateIfSubmitted(model);

            if (string.IsNullOrWhiteSpace(model.Description))
            {
                throw new InvalidValueException("Description is Required!");
            }

            //foreach (var file in files)
            //{
            //    var fileName = Path.GetFileName(file.FileName);
            //    if (await IsFileNameExistAsync(fileName))
            //    {
            //        throw new RecordAlreadyExistsException($"File name {fileName} already exists!");
            //    }
            //}

            using (var ctx = new AppManEntities())
            {
                foreach (var file in files)
                {
                    model.Id = Guid.NewGuid();
                    var fileName = model.Id + "-" + file.FileName;

                    if (await ctx.Uploads.AnyAsync(a => a.FileName.ToUpper() == fileName.ToUpper()))
                    {
                        throw new RecordAlreadyExistsException($"File name '{fileName}' already exists.");
                    }

                    if (file.ContentLength > 1024 * 1024 * model.FileSize)
                    {
                        throw new InvalidValueException($"The size of the file should not exceeed {model.FileSize} MB");
                    }
                    var fileExt = System.IO.Path.GetExtension(file.FileName).Substring(1).ToLower();
                    if (!_supportedTypes.Contains(fileExt))
                    {
                        throw new InvalidValueException("Invalid file type.");
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
                        Remarks = model.Remarks,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    ctx.Uploads.Add(entity);
                    await ctx.SaveChangesAsync();
                }
                return model;
            }
        }

        public virtual async ValueTask<Upload> UploadAsync(
            IEnumerable<HttpPostedFileBase> files,
            Upload model,
            string user,
            DateTime date)
        {
            if (files == null || !files.Any())
                throw new RecordNotFoundException("No files to upload!");

            if (string.IsNullOrWhiteSpace(model.Description))
                throw new InvalidValueException("Description is required.");

            ValidateIfSubmitted(model);

            // Validate all first
            foreach (var file in files)
            {
                string originalName = Path.GetFileName(file.FileName);

                if (await IsFileNameExistAsync(originalName))
                    throw new RecordAlreadyExistsException($"File name '{originalName}' already exists.");

                if (file.ContentLength > (1024 * 1024 * model.FileSize))
                    throw new InvalidValueException($"File '{originalName}' exceeds the allowed size of {model.FileSize} MB.");

                string ext = Path.GetExtension(originalName)?.TrimStart('.').ToLower();
                if (string.IsNullOrEmpty(ext) || !_supportedTypes.Contains(ext))
                    throw new InvalidValueException($"Invalid file type: {originalName}");
            }

            // Process files
            Upload lastEntity = null;
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                foreach (var file in files)
                {
                    model.Id = Guid.NewGuid();
                    var fileName = model.Id + "-" + file.FileName;

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
                        Remarks = model.Remarks,
                        InsertedBy = model.InsertedBy,
                        InsertedDt = model.InsertedDt,
                        UpdatedBy = model.UpdatedBy,
                        UpdatedDt = model.UpdatedDt
                    };

                    ctx.Uploads.Add(entity);
                    lastEntity = entity; // Keep reference to return
                }

                await ctx.SaveChangesAsync();
            }

            return lastEntity; // Return the last successfully saved file
        }


        private void ValidateIfSubmitted(Upload model)
        {
            Guid? reportId = null;
            Guid? locationId = null;
            var custodianReportItem = _db.CustodianReportItems.FirstOrDefault(f => f.Id == model.ImageId);
            if (custodianReportItem != null)
            {
                reportId = custodianReportItem.ReportId;
                locationId = custodianReportItem.LocationId;
            }
            else
            {
                var custodianReportBldgItem = _db.CustodianReportBldgItems.FirstOrDefault(f => f.Id == model.ImageId);
                if (custodianReportBldgItem != null)
                {
                    reportId = custodianReportBldgItem.ReportId;
                    locationId = custodianReportBldgItem.LocationId;
                }
                else
                {
                    var custodianReportLandItem = _db.CustodianReportLandItems.FirstOrDefault(f => f.Id == model.ImageId);
                    if (custodianReportLandItem != null)
                    {
                        reportId = custodianReportLandItem.ReportId;
                        locationId = custodianReportLandItem.LocationId;
                    }
                }
            }

            if (reportId != null)
            {
                var submitForCount = _db.CustodianReportSubmitForCounts.FirstOrDefault(f => f.ReportId == reportId && f.LocationId == locationId && f.Status == "Submit");
                if (submitForCount != null)
                {
                    var msg = $"Record already submitted for count by {submitForCount.UpdatedBy} on {submitForCount.UpdatedDt}, cannot update!";
                    throw new RecordAlreadyPostedException(msg);
                }
            }
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
            ValidateIfSubmitted(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.Uploads.FindAsync(model.Id);

                entity.ImageId = model.ImageId;
                entity.FileName = model.FileName;
                entity.Description = model.Description;
                entity.ServerIpAddress = model.ServerIpAddress;
                entity.VirtualDirectory = model.VirtualDirectory;
                entity.Remarks = model.Remarks;
                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //_db.Uploads.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                return model;
            }
        }

        public virtual async ValueTask<Upload> DeleteAsync(Upload model, string user, DateTime date)
        {
            ValidateIfSubmitted(model);
            model.UpdatedBy = user;
            model.UpdatedDt = date;

            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var entity = await ctx.Uploads.FindAsync(model.Id);

                entity.UpdatedBy = model.UpdatedBy;
                entity.UpdatedDt = model.UpdatedDt;

                //_db.Uploads.Attach(entity);
                //_db.Entry(entity).State = EntityState.Modified;
                await ctx.SaveChangesAsync();

                ctx.Uploads.Remove(entity);
                //_db.Entry(entity).State = EntityState.Deleted;
                await ctx.SaveChangesAsync();

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
        }

        public async Task<bool> IsFileNameExistAsync(string fileName)
        {
            return await _db.Uploads.AnyAsync(a => a.FileName.ToUpper() == fileName.ToUpper());
        }

        public async ValueTask CopyAsync(Guid? imageId, string directoryPath, string user, DateTime date)
        {
            using (var ctx = await _contextFactory.CreateContextAsync())
            {
                var uploads = await ctx.Uploads.Where(w => w.ImageId == imageId).ToListAsync();
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
                                Remarks = upload.Remarks,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            ctx.Uploads.Add(entity);
                            await ctx.SaveChangesAsync();

                            int index = upload.VirtualDirectory.IndexOf("UPLOADS", StringComparison.OrdinalIgnoreCase);
                            var sourcePath = directoryPath + upload.VirtualDirectory.Substring(index);

                            File.Copy(sourcePath, destinationPath);
                        }
                    }
                }
            }
        }
    }
}