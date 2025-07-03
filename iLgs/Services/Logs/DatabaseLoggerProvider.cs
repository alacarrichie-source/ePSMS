using iLgs.Models;
using Microsoft.Extensions.Logging;

namespace iLgs.Services.Logs
{
    public class DatabaseLoggerProvider : ILoggerProvider
    {
        private readonly AppManEntities _db;

        public DatabaseLoggerProvider(AppManEntities db)
        {
            _db = db;
        }

        public ILogger CreateLogger(string categoryName) => new DatabaseLogger(_db);

        public void Dispose() { }
    }
}