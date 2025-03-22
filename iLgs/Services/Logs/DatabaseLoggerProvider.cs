using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Services.Logs
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {        
        public ILogger CreateLogger(string categoryName) => new DatabaseLogger();

        public void Dispose() { }
    }
}