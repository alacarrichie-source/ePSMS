using iLgs.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using iLgs.Models;
using System.Threading.Tasks;
using System.Data.Entity;

namespace iLgs.Services
{
    public class DirectoryService : IDirectoryService
    {
        private readonly AppManEntities db = new AppManEntities();
        public DirectoryService(AppManEntities db)
        {
            this.db = db;
        }
        public string GetItemImageDirectory()
        {
            var dir = db.Codextns.Where(w => w.CodeMast.Code == "DIRS" && w.Code == "IMAGE-ITEMS").FirstOrDefault();
            if (dir != null)
            {
                return dir.Description.Trim();
            }
            return string.Empty;
        }
    }
}