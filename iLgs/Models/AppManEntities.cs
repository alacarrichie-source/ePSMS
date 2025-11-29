using iLgs.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Models
{
    public partial class AppManEntities
    {
        #region Synchronous Methods

        public override int SaveChanges()
        {
            var userName = WebHelper.GetUserName();
            var ipAddress = WebHelper.GetClientIpAddress();
            return SaveChanges(userName, ipAddress);
        }

        public int SaveChanges(string userId)
        {
            var ipAddress = WebHelper.GetClientIpAddress();
            return SaveChanges(userId, ipAddress);
        }

        public int SaveChanges(string userId, string ipAddress)
        {
            //var auditEntries = OnBeforeSaveChanges(userId, ipAddress);
            var result = base.SaveChanges();
            //OnAfterSaveChanges(auditEntries);
            return result;
        }

        #endregion

        #region Asynchronous Methods

        public override Task<int> SaveChangesAsync()
        {
            var userName = WebHelper.GetUserName();
            var ipAddress = WebHelper.GetClientIpAddress();
            return SaveChangesAsync(userName, ipAddress);
        }

        public Task<int> SaveChangesAsync(string userId)
        {
            var ipAddress = WebHelper.GetClientIpAddress();
            return SaveChangesAsync(userId, ipAddress);
        }

        public async Task<int> SaveChangesAsync(string userId, string ipAddress)
        {
            //var auditEntries = await OnBeforeSaveChangesAsync(userId, ipAddress);
            var result = await base.SaveChangesAsync();
            //await OnAfterSaveChangesAsync(auditEntries);
            return result;
        }

        #endregion

        #region Audit Logic

        private List<AuditEntry> OnBeforeSaveChanges(string userId, string ipAddress)
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;

            foreach (DbEntityEntry entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog ||
                    entry.State == EntityState.Detached ||
                    entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = GetTableName(entry.Entity),
                    UserId = userId,
                    IpAddress = ipAddress
                };
                auditEntries.Add(auditEntry);


                if (entry.State == EntityState.Added)
                {
                    auditEntry.Changes.Add(new AuditChange
                    {
                        PropertyName = string.Empty,
                        NewValue = string.Empty
                    });
                    auditEntry.Action = "INSERT";
                }

                //var propertyNames = entry.CurrentValues.PropertyNames;
                var propertyNames = entry.State == EntityState.Deleted
                    ? entry.OriginalValues.PropertyNames : entry.CurrentValues.PropertyNames;

                foreach (var propertyName in propertyNames)
                {
                    if (IsPrimaryKey(entry, propertyName))
                    {
                        auditEntry.KeyValues[propertyName] = entry.State == EntityState.Deleted
                            ? entry.OriginalValues[propertyName] : entry.CurrentValues[propertyName];
                        continue;
                    }


                    if (entry.State == EntityState.Deleted)
                    {
                        var dbValues = entry.GetDatabaseValues();
                        var original = dbValues[propertyName];
                        auditEntry.OldValues[propertyName] = original;
                        auditEntry.Changes.Add(new AuditChange
                        {
                            PropertyName = propertyName,
                            OldValue = original?.ToString()
                        });
                        auditEntry.Action = "DELETE";
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (propertyName == "SeriesNo")
                        {
                            var hit = true;
                        }

                        if (IsPropertyModified(entry, propertyName))
                        {
                            var dbValues = entry.GetDatabaseValues();
                            var original = dbValues[propertyName];
                            auditEntry.OldValues[propertyName] = original;
                            auditEntry.NewValues[propertyName] = entry.CurrentValues[propertyName];
                            auditEntry.Changes.Add(new AuditChange
                            {
                                PropertyName = propertyName,
                                OldValue = original?.ToString(),
                                NewValue = entry.CurrentValues[propertyName]?.ToString()
                            });
                            auditEntry.Action = "UPDATE";
                        }
                    }
                }
            }

            return auditEntries;
        }

        private async Task<List<AuditEntry>> OnBeforeSaveChangesAsync(string userId, string ipAddress)
        {
            ChangeTracker.DetectChanges();
            var auditEntries = new List<AuditEntry>();
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;

            foreach (DbEntityEntry entry in ChangeTracker.Entries())
            {
                if (entry.Entity is AuditLog ||
                    entry.State == EntityState.Detached ||
                    entry.State == EntityState.Unchanged)
                    continue;

                var auditEntry = new AuditEntry(entry)
                {
                    TableName = GetTableName(entry.Entity),
                    UserId = userId,
                    IpAddress = ipAddress
                };
                auditEntries.Add(auditEntry);


                if (entry.State == EntityState.Added)
                {
                    auditEntry.Changes.Add(new AuditChange
                    {
                        PropertyName = string.Empty,
                        NewValue = string.Empty
                    });
                    auditEntry.Action = "INSERT";
                }

                var propertyNames = entry.State == EntityState.Deleted
                    ? entry.OriginalValues.PropertyNames : entry.CurrentValues.PropertyNames;

                foreach (var propertyName in propertyNames)
                {
                    if (IsPrimaryKey(entry, propertyName))
                    {
                        auditEntry.KeyValues[propertyName] = entry.State == EntityState.Deleted
                            ? entry.OriginalValues[propertyName] : entry.CurrentValues[propertyName];
                        continue;
                    }

                    if (entry.State == EntityState.Deleted)
                    {
                        var dbValues = await entry.GetDatabaseValuesAsync();
                        var original = dbValues[propertyName];
                        auditEntry.OldValues[propertyName] = original;
                        auditEntry.Changes.Add(new AuditChange
                        {
                            PropertyName = propertyName,
                            OldValue = original?.ToString()
                        });
                        auditEntry.Action = "DELETE";
                    }
                    if (entry.State == EntityState.Modified)
                    {
                        if (propertyName == "SeriesNo")
                        {
                            var hit = true;
                        }

                        if (await IsPropertyModifiedAsync(entry, propertyName))
                        {
                            var dbValues = await entry.GetDatabaseValuesAsync();
                            var original = dbValues[propertyName];
                            auditEntry.OldValues[propertyName] = original;
                            auditEntry.NewValues[propertyName] = entry.CurrentValues[propertyName];
                            auditEntry.Changes.Add(new AuditChange
                            {
                                PropertyName = propertyName,
                                OldValue = original?.ToString(),
                                NewValue = entry.CurrentValues[propertyName]?.ToString()
                            });
                            auditEntry.Action = "UPDATE";
                        }
                    }
                }
            }

            return auditEntries;
        }

        private void OnAfterSaveChanges(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            using (var auditContext = new AppManEntities())
            {
                foreach (var auditEntry in auditEntries.Where(w => w.Action != null))
                {
                    string recordId = string.Empty;
                    if (!auditEntry.Action.Equals("INSERT"))
                    {
                        recordId = auditEntry.KeyValues.Count > 0
                            ? string.Join(",", auditEntry.KeyValues.Select(kv => $"{kv.Key}={kv.Value}"))
                            : "Unknown";
                    }
                    else
                    {
                        recordId = GetPrimaryKeyValue(auditEntry.Entry);
                    }

                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        TableName = auditEntry.TableName,
                        Action = auditEntry.Action,
                        UpdatedDt = DateTime.UtcNow,
                        UpdatedBy = auditEntry.UserId,
                        IpAddress = auditEntry.IpAddress,
                        RecordId = recordId,
                        OldValues = auditEntry.OldValues.Count == 0 ? null : JsonConvert.SerializeObject(auditEntry.OldValues),
                        NewValues = auditEntry.NewValues.Count == 0 ? null : JsonConvert.SerializeObject(auditEntry.NewValues)
                    };

                    auditContext.AuditLogs.Add(auditLog);

                    if (!auditEntry.Action.Equals("INSERT"))
                    {
                        // Add individual detail records
                        foreach (var change in auditEntry.Changes)
                        {
                            var auditDetail = new AuditLogDetail
                            {
                                Id = Guid.NewGuid(),
                                AuditLogId = auditLog.Id,
                                FieldName = change.PropertyName,
                                OldValue = change.OldValue,
                                NewValue = change.NewValue
                            };
                            auditContext.AuditLogDetails.Add(auditDetail);
                        }
                    }
                }

                auditContext.SaveChanges(); // Save all details
            }
        }

        private async Task OnAfterSaveChangesAsync(List<AuditEntry> auditEntries)
        {
            if (auditEntries == null || auditEntries.Count == 0)
                return;

            using (var auditContext = new AppManEntities())
            {
                foreach (var auditEntry in auditEntries)
                {
                    string recordId = string.Empty;
                    if (!auditEntry.Action.Equals("INSERT"))
                    {
                        recordId = auditEntry.KeyValues.Count > 0
                            ? string.Join(",", auditEntry.KeyValues.Select(kv => $"{kv.Key}={kv.Value}"))
                            : "Unknown";
                    }
                    else
                    {
                        recordId = GetPrimaryKeyValue(auditEntry.Entry);
                    }

                    var auditLog = new AuditLog
                    {
                        Id = Guid.NewGuid(),
                        TableName = auditEntry.TableName,
                        Action = auditEntry.Action,
                        UpdatedDt = DateTime.UtcNow,
                        UpdatedBy = auditEntry.UserId,
                        IpAddress = auditEntry.IpAddress,
                        RecordId = recordId,
                        OldValues = auditEntry.OldValues.Count == 0 ? null : JsonConvert.SerializeObject(auditEntry.OldValues),
                        NewValues = auditEntry.NewValues.Count == 0 ? null : JsonConvert.SerializeObject(auditEntry.NewValues)
                    };

                    auditContext.AuditLogs.Add(auditLog);

                    if (!auditEntry.Action.Equals("INSERT"))
                    {
                        // Add individual detail records
                        foreach (var change in auditEntry.Changes)
                        {
                            var auditDetail = new AuditLogDetail
                            {
                                Id = Guid.NewGuid(),
                                AuditLogId = auditLog.Id,
                                FieldName = change.PropertyName,
                                OldValue = change.OldValue,
                                NewValue = change.NewValue
                            };
                            auditContext.AuditLogDetails.Add(auditDetail);
                        }
                    }
                }

                await auditContext.SaveChangesAsync();
            }
        }

        private string GetPrimaryKeyValue(DbEntityEntry entry)
        {
            var objectStateEntry = ((IObjectContextAdapter)this).ObjectContext
                .ObjectStateManager
                .GetObjectStateEntry(entry.Entity);

            var keyNames = objectStateEntry.EntityKey.EntityKeyValues
                .Select(k => $"{k.Key}={k.Value}");

            return string.Join(",", keyNames);
        }
        #endregion

        #region Helper Methods

        private string GetTableName(object entity)
        {
            var entityType = ObjectContext.GetObjectType(entity.GetType());
            return entityType.Name;
        }

        private bool IsPrimaryKey(DbEntityEntry entry, string propertyName)
        {
            var objectContext = ((IObjectContextAdapter)this).ObjectContext;
            var entityType = ObjectContext.GetObjectType(entry.Entity.GetType());

            // Get metadata for the entity type
            var metadata = objectContext.MetadataWorkspace;

            var entityMeta = metadata
                .GetItems<EntityType>(DataSpace.CSpace)
                .FirstOrDefault(e => e.Name == entityType.Name);

            if (entityMeta == null)
                return false;

            // Get the key property names in CSpace
            var keyNames = entityMeta.KeyProperties.Select(k => k.Name);

            // Map to the OSpace/CLR property names
            var clrProperties = metadata
                .GetItems<EntityType>(DataSpace.OSpace)
                .FirstOrDefault(e => e.Name == entityType.Name)?
                .Properties;

            var mappedKeyNames = clrProperties?
                .Where(p => keyNames.Contains(p.Name))
                .Select(p => p.Name)
                .ToList();

            return mappedKeyNames != null && mappedKeyNames.Contains(propertyName);
        }

        private bool IsPropertyModified(DbEntityEntry entry, string propertyName)
        {
            var dbValues = entry.GetDatabaseValues();
            var original = dbValues[propertyName];
            var current = entry.CurrentValues[propertyName];

            if (original == null && current == null)
                return false;
            if (original == null || current == null)
                return true;

            return !original.Equals(current);
        }

        private async Task<bool> IsPropertyModifiedAsync(DbEntityEntry entry, string propertyName)
        {
            var dbValues = await entry.GetDatabaseValuesAsync();
            var original = dbValues[propertyName];
            var current = entry.CurrentValues[propertyName];

            if (original == null && current == null)
                return false;
            if (original == null || current == null)
                return true;

            return !original.Equals(current);
        }

        #endregion
    }

    public class AuditEntry
    {
        public AuditEntry(DbEntityEntry entry)
        {
            Entry = entry;
        }

        public DbEntityEntry Entry { get; }
        public string TableName { get; set; }
        public string Action { get; set; }
        public string UserId { get; set; }
        public string IpAddress { get; set; }
        public Dictionary<string, object> KeyValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> OldValues { get; } = new Dictionary<string, object>();
        public Dictionary<string, object> NewValues { get; } = new Dictionary<string, object>();
        public List<AuditChange> Changes { get; } = new List<AuditChange>();
    }

    public class AuditChange
    {
        public string PropertyName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}