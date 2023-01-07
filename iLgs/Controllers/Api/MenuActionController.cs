using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace iLgs.Controllers.Api
{
    public class MenuActionController : ApiController
    {
        private AppManEntities db = new AppManEntities();

        // GET: api/Menubases/RPTONLINE/0
        [Route("api/MenuAction/{sysCode}/{menuId}")]
        public IQueryable<MenuActionSw> GetMenuActions(string sysCode, int menuId)
        {
            //var data = db.MenuActions.Where(w => w.MenuId == menuId && w.Menubase.SysCode == sysCode).AsQueryable();
            var data = db.Database.SqlQuery<MenuActionSw>("Exec MenuAction_Read {0}, {1}", sysCode, menuId).AsQueryable();
            return data;
        }

        //// POST: api/Menubases
        ////[AppAuthorize("Menu")]
        //[Route("api/MenuAction/create")]
        //[ResponseType(typeof(MenuAction))]
        //public async Task<IHttpActionResult> PostMenuAction(MenuAction model)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.MenuActions.Add(model);
        //    await db.SaveChangesAsync();

        //    //return CreatedAtRoute("DefaultApi", new { id = menubase.ChildId}, menubase); // not working error 500
        //    return Created(model.Id.ToString(), model);

        //}
    }
}
