using iLgs.Models;
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

namespace iLgs.Controllers
{
    public class UserMenu_Controller : ApiController
    {
        private iLGSEntities db = new iLGSEntities();


        // GET: api/UserMenu_
        public IQueryable<MenubaseAccess> GetMenubaseAccesses()
        {
            return db.MenubaseAccesses;
        }

        // GET: api/UserMenu_/5
        [ResponseType(typeof(MenubaseAccess))]
        public async Task<IHttpActionResult> GetMenubaseAccess(string id)
        {
            MenubaseAccess menubaseAccess = await db.MenubaseAccesses.FindAsync(id);

            if (menubaseAccess == null)
            {
                return NotFound();
            }

            return Ok(menubaseAccess);
        }

        // GET: api/UserMenu_/5/RPTONLINE
        [Route("api/UserMenu_/{id}/{sysCode}")]
        public IQueryable<MenubaseAccess> GetMenubaseAccesses(string id, string sysCode)
        {
            var db2 = db;
            db2.Configuration.LazyLoadingEnabled = true;
            return db2.MenubaseAccesses.Where(p => p.SysCode == sysCode && p.ParentId == 0 && p.UserId == id);
        }

        //// GET: api/UserMenu_/5/RPTONLINE
        //[Route("api/UserMenu_/{id}/{sysCode}")]
        ////public IQueryable<Menubase> GetMenubaseAccesses(string id, string sysCode)
        //public IQueryable<Menubase> GetMenubaseAccesses(string sysCode, int parentId)
        //{
        //    //var db2 = db;
        //    //db2.Configuration.LazyLoadingEnabled = true;
        //    //return db2.Menubases.Where(p => p.SysCode == sysCode && p.ParentId == 0);


        //    var db2 = db;
        //    db2.Configuration.LazyLoadingEnabled = true;
        //    var menu = db2.Menubases.Where(p => p.SysCode == sysCode && p.ParentId == parentId);
        //    return menu;
        //}


        // GET: api/UserMenu_/5/RPTONLINE
        [Route("api/UserMenu_/{id}/{sysCode}/{controllerName}")]
        public IQueryable<MenubaseAccess> GetMenubaseAccesses(string id, string sysCode, string controllerName)
        {
            return db.MenubaseAccesses.Where(p => p.SysCode == sysCode && p.UserId == id && p.Controller.ToUpper() == controllerName.ToUpper());
        }


        //[iLGSAuthorize("Menu")]
        // PUT: api/UserMenu_/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutMenubaseAccess(string id, MenubaseAccess menubaseAccess)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != menubaseAccess.ChildIdAccess)
            {
                return BadRequest();
            }

            db.Entry(menubaseAccess).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MenubaseAccessExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        //[iLGSAuthorize("Menu")]
        // POST: api/UserMenu_
        [ResponseType(typeof(MenubaseAccess))]
        public async Task<IHttpActionResult> PostMenubaseAccess(MenubaseAccess menubaseAccess)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.MenubaseAccesses.Add(menubaseAccess);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (MenubaseAccessExists(menubaseAccess.ChildIdAccess))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = menubaseAccess.ChildIdAccess }, menubaseAccess);
        }

        //[iLGSAuthorize("Menu")]
        // DELETE: api/UserMenu_/5
        [ResponseType(typeof(MenubaseAccess))]
        public async Task<IHttpActionResult> DeleteMenubaseAccess(string id)
        {
            MenubaseAccess menubaseAccess = await db.MenubaseAccesses.FindAsync(id);
            if (menubaseAccess == null)
            {
                return NotFound();
            }

            db.MenubaseAccesses.Remove(menubaseAccess);
            await db.SaveChangesAsync();

            return Ok(menubaseAccess);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool MenubaseAccessExists(string id)
        {
            return db.MenubaseAccesses.Count(e => e.ChildIdAccess == id) > 0;
        }
    }
}