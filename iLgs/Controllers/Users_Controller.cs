using iLgs.Models;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace iLgs.Controllers
{
    public class Users_Controller : ApiController
    {
        private readonly AppManEntities _db;

        public Users_Controller(AppManEntities db)
        {
            _db = db;
        }

        // GET: api/Users_
        public IQueryable<AspNetUser> GetAspNetUsers()
        {
            //return db.AspNetUsers.Include(i => i.UserInfo); //.Where(p => p.UserInfo.RecId != null);
            return _db.AspNetUsers.Include(i => i.UserProfile); //.Where(p => p.UserInfo.RecId != null);
        }

        // GET: api/Users_/IsAdmin/82814eba-0738-4edd-a11f-66c8112e20de
        [Route("api/Users_/IsAdmin/{id}")]
        public bool GetAspNetUserAdmins(string id)
        {
            var db2 = _db;
            db2.Configuration.LazyLoadingEnabled = true;
            bool retVal = false;
            var users = db2.AspNetUsers.Where(p => p.Id == id);
            if (users.Count() > 0)
            {
                foreach (var user in users)
                {
                    foreach (var role in user.AspNetUserRoles)
                    {
                        if (role.RoleId.ToUpper() == "ADMIN" || role.RoleId.ToUpper() == "APPMAN_ADMIN")
                        {
                            retVal = true;
                        }
                    }
                }
            }
            return retVal;
        }


        // GET: api/Users_/log/82814eba-0738-4edd-a11f-66c8112e20de
        //[Route("api/Users_/log/{id}")]
        //public IEnumerable<UserLog> GetUserLog(string id)
        //{
        //    AspNetUser aspNetUser = db.AspNetUsers.Find(id);
        //    if (aspNetUser != null)
        //    {
        //        return aspNetUser.UserLogs;
        //    }
        //    else
        //    {
        //        return Enumerable.Empty<UserLog>();
        //    }
        //    //return db.UserLogs.Where(p => p.UserId == id);
        //}

        //// GET: api/Users_/rpt/82814eba-0738-4edd-a11f-66c8112e20de
        //[Route("api/Users_/rpt/{id}")]
        //public IEnumerable<OnlineRpusView> GetUserProperty(string id)
        //{
        //    return db.OnlineRpusViews.Where(p => p.IPayUserId == id);
        //}

        //// GET: api/Users_/btax/82814eba-0738-4edd-a11f-66c8112e20de
        //[Route("api/Users_/btax/{id}")]
        //public IEnumerable<OnlineBusinessView> GetUserBusiness(Guid id)
        //{
        //    return db.OnlineBusinessViews.Where(p => p.RecId == id);
        //}


        //[Route("api/Users_/log2/{id}")]
        //public IEnumerable<UserLog> GetUserLog2(string id)
        //{
        //    //AspNetUser aspNetUser = db.AspNetUsers.Find(id);
        //    //if (aspNetUser != null)
        //    //{
        //    //    return aspNetUser.UserLogs;
        //    //}
        //    //else
        //    //{
        //    //    return Enumerable.Empty<UserLog>();
        //    //}
        //    return db.UserLogs.Where(p => p.UserId == id);
        //}

        // GET: api/Users_/5
        [ResponseType(typeof(AspNetUser))]
        public async Task<IHttpActionResult> GetAspNetUser(string id)
        {
            AspNetUser aspNetUser = await _db.AspNetUsers.FindAsync(id);
            if (aspNetUser == null)
            {
                return NotFound();
            }

            return Ok(aspNetUser);
        }

        // GET: api/Users_/Profile/
        [Route("api/Users_/Profile/{id}")]
        [ResponseType(typeof(AspNetUser))]
        public async Task<IHttpActionResult> GetAspNetUserProfile(string id)
        {
            UserProfile userProfile = await _db.UserProfiles.Where(w => w.UserId == id).SingleOrDefaultAsync();
            if (userProfile == null)
            {
                return NotFound();
            }

            return Ok(userProfile);
        }

        // PUT: api/Users_/5
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutAspNetUser(string id, AspNetUser aspNetUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != aspNetUser.Id)
            {
                return BadRequest();
            }

            _db.Entry(aspNetUser).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AspNetUserExists(id))
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

        // POST: api/Users_
        [ResponseType(typeof(AspNetUser))]
        public async Task<IHttpActionResult> PostAspNetUser(AspNetUser aspNetUser)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _db.AspNetUsers.Add(aspNetUser);

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                if (AspNetUserExists(aspNetUser.Id))
                {
                    return Conflict();
                }
                else
                {
                    throw;
                }
            }

            return CreatedAtRoute("DefaultApi", new { id = aspNetUser.Id }, aspNetUser);
        }

        // DELETE: api/Users_/5
        [ResponseType(typeof(AspNetUser))]
        public async Task<IHttpActionResult> DeleteAspNetUser(string id)
        {
            AspNetUser aspNetUser = await _db.AspNetUsers.FindAsync(id);
            if (aspNetUser == null)
            {
                return NotFound();
            }

            _db.AspNetUsers.Remove(aspNetUser);
            await _db.SaveChangesAsync();

            return Ok(aspNetUser);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool AspNetUserExists(string id)
        {
            return _db.AspNetUsers.Count(e => e.Id == id) > 0;
        }
    }
}