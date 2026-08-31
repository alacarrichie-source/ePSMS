using iLgs.Ai.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace iLgs.Services.Ai.PurchaseOrder
{    
    public interface IDocumentStorageService
    {
        PODocumentViewModel SaveUploadedFile(HttpPostedFileBase file, string subFolder, string category);
        byte[] GetFileBytes(string relativePath);
    }

    public class DocumentStorageService : IDocumentStorageService
    {
        public PODocumentViewModel SaveUploadedFile(HttpPostedFileBase file, string subFolder, string category)
        {
            if (file == null || file.ContentLength == 0) return null;

            var uploadDir = HttpContext.Current.Server.MapPath($"~/App_Data/Uploads/{subFolder}/");
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
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
            var fullPath = HttpContext.Current.Server.MapPath(relativePath);
            return File.Exists(fullPath) ? File.ReadAllBytes(fullPath) : null;
        }
    }
}