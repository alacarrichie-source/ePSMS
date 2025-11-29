using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Utilities
{
    public interface IAppManEntitiesFactory
    {
        AppManEntities CreateContext();
        Task<AppManEntities> CreateContextAsync();
    }

    public class AppManEntitiesFactory : IAppManEntitiesFactory
    {
        public AppManEntities CreateContext()
        {
            // EF6 will automatically use the connection string from app.config/web.config
            // when you have the named connection string matching your context name
            return new AppManEntities();
        }

        public Task<AppManEntities> CreateContextAsync()
        {
            return Task.FromResult(CreateContext());
        }
    }
}