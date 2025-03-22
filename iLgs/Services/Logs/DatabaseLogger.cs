using iLgs.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;

namespace iLgs.Services.Logs
{
    public class DatabaseLogger : ILogger
    {
        private readonly AppManEntities _db;        

        public DatabaseLogger()
        {
            _db = new AppManEntities();
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (formatter == null) return;

            string message = formatter(state, exception);
            string exceptionMessage = exception?.ToString();

            //_db.Database.ExecuteSqlCommand("INSERT INTO Exceptions(Timestamp, LogLevel, Message, Exception) VALUES({0}, {1}, {2}, {3})", DateTime.UtcNow, logLevel.ToString(), message, exceptionMessage ?? (object)DBNull.Value);
            LogErrorWithId(logLevel, message, exceptionMessage);
        }

        public int LogErrorWithId(LogLevel logLevel, string message, string exceptionMessage)
        {            
            // Retrieve the newly inserted Identity (Log ID)
            int logId = _db.Database.SqlQuery<int>(
                @"INSERT INTO Exceptions (Timestamp, LogLevel, Message, Exception)
                  OUTPUT INSERTED.Id
                VALUES (@p0, @p1, @p2, @p3)",
                DateTime.UtcNow, logLevel.ToString(), message, exceptionMessage ?? (object)DBNull.Value
            ).FirstOrDefault();

            return logId; // Return the inserted log ID
        }
    }
}