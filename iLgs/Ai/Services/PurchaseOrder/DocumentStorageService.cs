using iLgs.Ai.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace iLgs.Ai.Services.PurchaseOrder
{    
    public interface IDocumentStorageService
    {
        PODocumentViewModel SaveUploadedFile(HttpPostedFileBase file, string subFolder, string category);
        byte[] GetFileBytes(string relativePath);
    }

    public class DocumentStorageService : IDocumentStorageService
    {
        private const int MaximumUploadBytes = 10 * 1024 * 1024;
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };

        public PODocumentViewModel SaveUploadedFile(HttpPostedFileBase file, string subFolder, string category)
        {
            if (file == null || file.ContentLength == 0) throw new InvalidOperationException("Select a file to upload.");
            if (file.ContentLength > MaximumUploadBytes) throw new InvalidOperationException("Files must not exceed 10 MB.");
            var extension = Path.GetExtension(file.FileName);
            if (String.IsNullOrWhiteSpace(extension) || !AllowedExtensions.Contains(extension.ToLowerInvariant()))
                throw new InvalidOperationException("This file type is not allowed.");

            var uploadDir = HttpContext.Current.Server.MapPath($"~/App_Data/Uploads/{subFolder}/");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var uniqueFileName = Guid.NewGuid().ToString("N") + extension.ToLowerInvariant();
            var fullPath = Path.Combine(uploadDir, uniqueFileName);
            file.SaveAs(fullPath);

            var sizeInKb = file.ContentLength / 1024;
            var formattedSize = sizeInKb > 1024 ? $"{(sizeInKb / 1024f):F1} MB" : $"{sizeInKb} KB";

            return new PODocumentViewModel
            {
                FileName = Path.GetFileName(file.FileName),
                FilePath = $"~/App_Data/Uploads/{subFolder}/{uniqueFileName}",
                FileSize = formattedSize,
                Category = category,
                UploadedAt = DateTime.UtcNow
            };
        }

        public byte[] GetFileBytes(string relativePath)
        {
            if (String.IsNullOrWhiteSpace(relativePath)) return null;
            var fullPath = HttpContext.Current.Server.MapPath(relativePath);
            var uploadRoot = Path.GetFullPath(HttpContext.Current.Server.MapPath("~/App_Data/Uploads/"));
            fullPath = Path.GetFullPath(fullPath);
            if (!fullPath.StartsWith(uploadRoot, StringComparison.OrdinalIgnoreCase)) return null;
            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
        }
    }
}
