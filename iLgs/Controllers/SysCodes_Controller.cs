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
    public class SysCodes_Controller : ApiController
    {
        private string _mastCode = "APPS";
        private AppManEntities db = new AppManEntities();

        // GET: api/syscodes_/82814eba-0738-4edd-a11f-66c8112e20de
        [Route("api/syscodes_/{userId}")]
        public IQueryable<Codextn> GetSysCodes(string userId)
        {
            /*
             * Get user roles
             * if role = admin, view all
             * else view all menu where user is in {role}_admin
             */
            var isAdmin = db.AspNetUserRoles.Where(w => w.RoleId == "admin" && w.UserId == userId).Count() > 0;
            var model = Enumerable.Empty<Codextn>().AsQueryable();

            if (isAdmin)
            {
                model = db.Codextns.Where(w => w.CodeMast.Code == _mastCode);
            }
            else
            {
                model = db.Codextns.Where(w => w.CodeMast.Code == _mastCode
                    && db.AspNetUserRoles.Where(x => x.UserId == userId && x.RoleId.Contains(w.Code) && x.RoleId.Contains("_admin")).Any());
            }

            return model;
        }

        [Route("api/SysCodes/{userId}")]
        public IQueryable<Codextn> GetSysCodesNew(string userId)
        {
            /*
             * Get user roles
             * if role = admin, view all
             * else view all menu where user is in {role}_admin
             */
           
            var isAdmin = db.AspNetUserRoles.Where(w => w.RoleId == "admin" && w.UserId == userId).Count() > 0;
            //var model = Enumerable.Empty<Codextn>().AsQueryable();

            if (isAdmin)
            {
                var data = db.Codextns.Where(w => w.CodeMast.Code == _mastCode);
                return data;
            }
            else
            {
                var data = db.Codextns.Where(w => w.CodeMast.Code == _mastCode
                    && db.AspNetUserRoles.Where(x => x.UserId == userId && x.RoleId.Contains(w.Code) && x.RoleId.Contains("_admin")).Any());
                return data;
            }

            //return model;
        }


        //// GET: api/SysCodes_
        //public IQueryable<SysCode> GetSysCodes()
        //{
        //    return db.SysCodes;
        //}


        // GET: api/SysCodes_/Contains/Integrated
        [Route("api/SysCodes_/Contains/{text}")]
        public IQueryable<Codextn> GetSysCodeContains(string text)
        {
            var data = db.Codextns.Where(w => w.CodeMast.Code == _mastCode && w.Code.Contains(text)).AsQueryable();
            return data;
        }

        // GET: api/SysCodes_/5
        [ResponseType(typeof(Codextn))]
        public async Task<IHttpActionResult> GetSysCode(string id)
        {
            
            var sysCode = db.Codextns.Where(w => w.CodeMast.Code == _mastCode && w.Code == id).FirstOrDefault();

            if (sysCode == null)
            {
                return NotFound();
            }

            return Ok(sysCode);
        }

        //// PUT: api/SysCodes_/5
        //[ResponseType(typeof(void))]
        //public async Task<IHttpActionResult> PutSysCode(string id, SysCode sysCode)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != sysCode.SysCode1)
        //    {
        //        return BadRequest();
        //    }

        //    db.Entry(sysCode).State = EntityState.Modified;

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!SysCodeExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return StatusCode(HttpStatusCode.NoContent);
        //}

        //// POST: api/SysCodes_
        //[ResponseType(typeof(SysCode))]
        //public async Task<IHttpActionResult> PostSysCode(SysCode sysCode)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    db.SysCodes.Add(sysCode);

        //    try
        //    {
        //        await db.SaveChangesAsync();
        //    }
        //    catch (DbUpdateException)
        //    {
        //        if (SysCodeExists(sysCode.SysCode1))
        //        {
        //            return Conflict();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return CreatedAtRoute("DefaultApi", new { id = sysCode.SysCode1 }, sysCode);
        //}

        //// DELETE: api/SysCodes_/5
        //[ResponseType(typeof(SysCode))]
        //public async Task<IHttpActionResult> DeleteSysCode(string id)
        //{
        //    SysCode sysCode = await db.SysCodes.FindAsync(id);
        //    if (sysCode == null)
        //    {
        //        return NotFound();
        //    }

        //    db.SysCodes.Remove(sysCode);
        //    await db.SaveChangesAsync();

        //    return Ok(sysCode);
        //}

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        //private bool SysCodeExists(string id)
        //{
        //    return db.SysCodes.Count(e => e.SysCode1 == id) > 0;
        //}
    }
}