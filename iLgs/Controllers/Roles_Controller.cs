using iLgs.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace iLgs.Controllers
{
    public class Roles_Controller : ApiController
    {
        private AppManEntities db = new AppManEntities();

        // GET: api/Roles_/5/RPTONLINE/admin
        [Route("api/roles_/{id}/{role}")]
        //[HttpGet]
        public IQueryable<AspNetUser> GetAspNetUsers(string id, string role)
        //public IEnumerable<AspNetUser> GetAspNetUsers(string id, string role)
        {
            var users = from a in db.AspNetUsers
                        join b in db.AspNetUserRoles on a.Id equals b.UserId
                        where (a.Id == id && b.RoleId == role)
                        select a;

            //return users.ToList();            
            return users;
        }

        [Route("api/roles_/{id}")]
        public IQueryable<AspNetUser> GetAspNetUsers(string id)
        {
            //var users = from a in db.AspNetUsers
            //            join b in db.AspNetUserRoles on a.Id equals b.UserId
            //            where (a.Id == id)
            //            select a;        
            var users = db.AspNetUsers
                .Include("AspNetUserRoles")
                .Where(w => w.Id == id);
            return users;
        }

    }
}