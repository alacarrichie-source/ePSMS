using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using System.Web;
using iLgs.Utilities;

namespace iLgs.Controllers.Api
{
    public class MenuAccessActionController : ApiController
    {
        private AppManEntities db = new AppManEntities();
        

        // GET: api/MenuAccessActions/RPTONLINE/0/Guid
        [Route("api/MenuAccessAction/{sysCode}/{childId}/{userId}")]
        public IQueryable<MenuActionSw> GetMenuAccessActions(string sysCode, int childId, string userId)
        {
            var data = db.Database.SqlQuery<MenuActionSw>("Exec MenuAccessAction_Read {0}, {1}, {2}", sysCode, childId, userId).AsQueryable();
            return data;
        }
    }


}