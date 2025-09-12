using iLgs.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace iLgs.Services.AuditLog_
{
    public interface IAuditLogService
    {
        List<AuditLog> GetAuditLogs(string tableName, string recordId = null);
        string GetAuditHistory(string tableName, int recordId);
        IQueryable<AuditLogVM> GetAuditLogsQueryable(string recordId = null);
        IQueryable<AuditLogDetailVM> GetAuditDetailsQueryable(Guid? auditLogId);
    }

    public class AuditLogService : IAuditLogService
    {
        private readonly AppManEntities _db;

        public AuditLogService(AppManEntities db)
        {
            _db = db;
        }

        public List<AuditLog> GetAuditLogs(string tableName, string recordId = null)
        {
            var query = _db.AuditLogs
                .Where(a => a.TableName == tableName);

            if (!string.IsNullOrEmpty(recordId))
            {
                query = query.Where(a => a.RecordId.Contains(recordId));
            }

            return query.OrderByDescending(a => a.UpdatedDt).ToList();
        }

        public string GetAuditHistory(string tableName, int recordId)
        {
            var audits = _db.AuditLogs
                .Where(a => a.TableName == tableName && a.RecordId.Contains(recordId.ToString()))
                .OrderBy(a => a.UpdatedDt)
                .ToList();

            var history = new StringBuilder();
            foreach (var audit in audits)
            {
                history.AppendLine($"{audit.UpdatedDt} - {audit.Action} by {audit.UpdatedBy}");

                if (!string.IsNullOrEmpty(audit.OldValues))
                {
                    var oldValues = JsonConvert.DeserializeObject<Dictionary<string, object>>(audit.OldValues);
                    foreach (var value in oldValues)
                    {
                        history.AppendLine($"  {value.Key}: {value.Value} → (current value)");
                    }
                }
            }

            return history.ToString();
        }

        public IQueryable<AuditLogVM> GetAuditLogsQueryable(string recordId = null)
        {
            var query = _db.AuditLogs
                .Select(al => new AuditLogVM
                {
                    Id = al.Id,
                    TableName = al.TableName,
                    Action = al.Action,
                    RecordId = al.RecordId,
                    UpdatedBy = al.UpdatedBy,
                    UpdatedDt = al.UpdatedDt,
                    IpAddress = al.IpAddress,
                    DetailsCount = al.AuditLogDetails.Count
                });
                
            if (!string.IsNullOrEmpty(recordId))
            {
                query = query.Where(a => a.RecordId.Contains(recordId));
            }

            return query.OrderByDescending(a => a.UpdatedDt).OrderByDescending(al => al.UpdatedDt);
        }

        public IQueryable<AuditLogDetailVM> GetAuditDetailsQueryable(Guid? auditLogId)
        {
            return _db.AuditLogDetails
                .Where(ad => ad.AuditLogId == auditLogId)
                .Select(ad => new AuditLogDetailVM
                {
                    Id = ad.Id,
                    FieldName = ad.FieldName,
                    OldValue = ad.OldValue,
                    NewValue = ad.NewValue,
                    AuditLogId = ad.AuditLogId
                });
        }
    }
}