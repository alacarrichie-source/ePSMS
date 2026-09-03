using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace iLgs.Ai.Services
{
    public interface IDocumentHistoryService
    {
        void AddStatusHistory(string documentType, Guid documentId, string documentNo,
            string fromStatus, string toStatus, string action, string remarks, string changedBy);
        List<DocumentStatusHistory> GetHistory(string documentType, Guid documentId);
        DocumentStatusHistory GetLatestHistory(string documentType, Guid documentId);
        Task<List<DocumentStatusHistory>> GetHistoryAsync(string documentType, Guid documentId);
        Task<DocumentStatusHistory> GetLatestHistoryAsync(string documentType, Guid documentId);
    }

    public class DocumentHistoryService : IDocumentHistoryService
    {
        private readonly AppManEntities _db;

        public DocumentHistoryService(AppManEntities db)
        {
            if (db == null)
                throw new ArgumentNullException("db");

            _db = db;
        }

        public void AddStatusHistory(string documentType, Guid documentId, string documentNo,
            string fromStatus, string toStatus, string action, string remarks, string changedBy)
        {
            Validate(documentType, documentId, toStatus, action, changedBy);

            _db.DocumentStatusHistories.Add(new DocumentStatusHistory
            {
                Id = Guid.NewGuid(),
                DocumentType = documentType.Trim(),
                DocumentId = documentId,
                DocumentNo = documentNo,
                FromStatus = fromStatus,
                ToStatus = toStatus.Trim(),
                Action = action.Trim(),
                Remarks = remarks,
                ChangedBy = changedBy.Trim(),
                ChangedDt = DateTime.Now
            });
        }

        public List<DocumentStatusHistory> GetHistory(string documentType, Guid documentId)
        {
            ValidateDocumentKey(documentType, documentId);
            return HistoryQuery(documentType, documentId)
                .OrderBy(history => history.ChangedDt)
                .ThenBy(history => history.Id)
                .ToList();
        }

        public DocumentStatusHistory GetLatestHistory(string documentType, Guid documentId)
        {
            ValidateDocumentKey(documentType, documentId);
            return HistoryQuery(documentType, documentId)
                .OrderByDescending(history => history.ChangedDt)
                .ThenByDescending(history => history.Id)
                .FirstOrDefault();
        }

        public Task<List<DocumentStatusHistory>> GetHistoryAsync(string documentType, Guid documentId)
        {
            ValidateDocumentKey(documentType, documentId);
            return HistoryQuery(documentType, documentId)
                .OrderBy(history => history.ChangedDt)
                .ThenBy(history => history.Id)
                .ToListAsync();
        }

        public Task<DocumentStatusHistory> GetLatestHistoryAsync(string documentType, Guid documentId)
        {
            ValidateDocumentKey(documentType, documentId);
            return HistoryQuery(documentType, documentId)
                .OrderByDescending(history => history.ChangedDt)
                .ThenByDescending(history => history.Id)
                .FirstOrDefaultAsync();
        }

        private IQueryable<DocumentStatusHistory> HistoryQuery(string documentType, Guid documentId)
        {
            var normalizedDocumentType = documentType.Trim();
            return _db.DocumentStatusHistories.AsNoTracking().Where(history =>
                history.DocumentType == normalizedDocumentType && history.DocumentId == documentId);
        }

        private static void Validate(string documentType, Guid documentId, string toStatus,
            string action, string changedBy)
        {
            ValidateDocumentKey(documentType, documentId);

            if (string.IsNullOrWhiteSpace(toStatus))
                throw new ArgumentException("To status is required.", "toStatus");
            if (string.IsNullOrWhiteSpace(action))
                throw new ArgumentException("Action is required.", "action");
            if (string.IsNullOrWhiteSpace(changedBy))
                throw new ArgumentException("Changed by is required.", "changedBy");
        }

        private static void ValidateDocumentKey(string documentType, Guid documentId)
        {
            if (string.IsNullOrWhiteSpace(documentType))
                throw new ArgumentException("Document type is required.", "documentType");
            if (documentId == Guid.Empty)
                throw new ArgumentException("Document ID is required.", "documentId");
        }
    }
}
