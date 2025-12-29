using iLgs.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;

namespace iLgs.Controllers
{
    public class UserMenu_Controller : ApiController
    {
        private readonly AppManEntities _db = new AppManEntities();

        public UserMenu_Controller()
        {
            
        }

        // GET: api/UserMenu_/5/RPTONLINE
        [Route("api/UserMenu_/{userId}/{sysCode}/{controllerName}")]
        public MenuAccess GetMenuAccessRights(string userId, string sysCode, string controllerName)
        {
            var access = _db.MenuAccesses.Include(i => i.MenuAccessActions)
               .Where(w => w.Menubase.SysCode == sysCode && w.Menubase.Controller.ToUpper() == controllerName.ToUpper() && w.UserId == userId && w.IsAllowed == true)               
               .FirstOrDefault();
            return access;
        }
        
    }
}