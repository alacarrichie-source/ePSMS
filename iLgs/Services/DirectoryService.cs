using iLgs.Models;
using System.Linq;

namespace iLgs.Services
{
    public interface IDirectoryService
    {
        string GetItemImageDirectory();
    }

    public class DirectoryService : IDirectoryService
    {
        private readonly AppManEntities _db;
        public DirectoryService(AppManEntities db)
        {
            _db = db;
        }
        public string GetItemImageDirectory()
        {
            var dir = _db.Codextns.Where(w => w.CodeMast.Code == "DIRS" && w.Code == "IMAGE-ITEMS").FirstOrDefault();
            if (dir != null)
            {
                return dir.Description.Trim();
            }
            return string.Empty;
        }
    }
}