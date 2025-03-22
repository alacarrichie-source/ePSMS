using iLgs.Services.Logs;
using Microsoft.Extensions.Logging;
using Microsoft.Owin.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services
{    
    public interface ILoggingService
    {
        void LogInformation(string message);
        void LogTrace(string message);
        void LogDebug(string message);
        void LogWarning(string message);
        void LogError(Exception exception);        
        void LogCritical(Exception exception);
    }
    
    public class LoggingService : ILoggingService
    {
        private readonly ILogger<LoggingService> _logger;

        public LoggingService()
        {
            //ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
            //{
            //    builder.AddProvider(new DatabaseLoggerProvider()); // ✅ Use custom DB logger
            //});

            //_logger = loggerFactory.CreateLogger<LoggingService>();            

        }

        public LoggingService(ILogger<LoggingService> logger) =>
        this._logger = logger;

        public void LogInformation(string message) =>
            this._logger.LogInformation(message);

        public void LogTrace(string message) =>
            this._logger.LogTrace(message);

        public void LogDebug(string message) =>
            this._logger.LogDebug(message);

        public void LogWarning(string message) =>
            this._logger.LogWarning(message);

        public void LogError(Exception exception) =>
            this._logger.LogError(exception, exception.Message);

        public void LogCritical(Exception exception) =>
            this._logger.LogCritical(exception, exception.Message);
    }
}