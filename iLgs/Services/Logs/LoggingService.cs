using iLgs.Models;
using iLgs.Services.Logs;
using Microsoft.Extensions.Logging;
using Microsoft.Owin.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Services.Logs
{    
    public interface ILoggingService
    {
        Task LogInformation(string message);
        Task LogTrace(string message);
        Task LogDebug(string message);
        Task LogWarning(string message);
        Task LogError(Exception exception);
        Task LogCritical(Exception exception);
    }
    
    public class LoggingService : ILoggingService
    {        
        public LoggingService()
        {            
        }

        public Task LogInformation(string message) => LogToDb("Information", null, message);
        public Task LogTrace(string message) => LogToDb("Trace", null, message);
        public Task LogDebug(string message) => LogToDb("Debug", null, message);
        public Task LogWarning(string message) => LogToDb("Warning", null, message);

        public Task LogError(Exception exception)
        {
            return LogToDb("Error", GetErrorCode(exception), GetExceptionDetails(exception));
        }

        public Task LogCritical(Exception exception)
        {
            return LogToDb("Critical", GetErrorCode(exception), GetExceptionDetails(exception));
        }

        private async Task LogToDb(string source, string errorCode, string description)
        {
            try
            {
                using (var _db = new AppManEntities()) // 👈 new instance each time
                {
                    var log = new ErrorLog
                    {
                        Id = Guid.NewGuid(),
                        ErrorSource = source,
                        ErrorCode = errorCode,
                        Description = description,
                        InsertedDt = DateTime.Now,
                        InsertedBy = HttpContext.Current?.User?.Identity?.Name ?? "System"
                    };

                    _db.ErrorLogs.Add(log);
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                // As a safety net, fall back to console logging if DB write fails
                Console.WriteLine($"[LoggingService] Failed to log to DB: {ex.Message}");
                Console.WriteLine($"Original Log: {description}");
            }
        }

        private string GetErrorCode(Exception ex)
        {
            return $"{ex.GetType().Name}:{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        }

        private string GetExceptionDetails(Exception ex)
        {
            //return $"{ex.Message}{Environment.NewLine}{ex.StackTrace}";
            return GetFullExceptionDetails(ex);
        }

        //private static string GetFullExceptionDetails(Exception ex)
        //{
        //    if (ex == null)
        //        return string.Empty;

        //    var sb = new StringBuilder();

        //    int level = 0;
        //    Exception currentEx = ex;

        //    while (currentEx != null)
        //    {
        //        sb.AppendLine($"--- Exception Level {level} ---");
        //        sb.AppendLine($"Type      : {currentEx.GetType().FullName}");
        //        sb.AppendLine($"Message   : {currentEx.Message}");
        //        sb.AppendLine($"Source    : {currentEx.Source}");
        //        sb.AppendLine("StackTrace:");
        //        sb.AppendLine(currentEx.StackTrace ?? "[No stack trace available]");
        //        sb.AppendLine(new string('-', 80)); // visual separator

        //        currentEx = currentEx.InnerException;
        //        level++;
        //    }

        //    return sb.ToString();
        //}

        private static string GetFullExceptionDetails(Exception ex)
        {
            if (ex == null)
                return string.Empty;

            var sb = new StringBuilder();

            int level = 0;
            Exception currentEx = ex;

            while (currentEx != null)
            {
                sb.AppendLine($"--- Exception Level {level} ---");
                sb.AppendLine($"Type      : {currentEx.GetType().FullName}");
                sb.AppendLine($"Message   : {currentEx.Message}");
                sb.AppendLine($"Source    : {currentEx.Source}");
                sb.AppendLine("StackTrace:");
                sb.AppendLine(currentEx.StackTrace ?? "[No stack trace available]");

                // 🔍 Handle Entity Framework validation errors
                if (currentEx is System.Data.Entity.Validation.DbEntityValidationException dbEx)
                {
                    sb.AppendLine("Entity Validation Errors:");

                    foreach (var eve in dbEx.EntityValidationErrors)
                    {
                        string entityName = eve.Entry.Entity.GetType().Name;
                        sb.AppendLine($"  - Entity: {entityName}, State: {eve.Entry.State}");

                        foreach (var ve in eve.ValidationErrors)
                        {
                            sb.AppendLine($"    • Property: {ve.PropertyName}");
                            sb.AppendLine($"      Error   : {ve.ErrorMessage}");
                        }
                    }
                }

                sb.AppendLine(new string('-', 80)); // visual separator

                currentEx = currentEx.InnerException;
                level++;
            }

            return sb.ToString();
        }

    }

}