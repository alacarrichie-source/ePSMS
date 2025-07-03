using iLgs.Models;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace iLgs.Controllers
{
    public class Menubases_Controller : ApiController
    {
        private readonly AppManEntities _db;

        public Menubases_Controller(AppManEntities db)
        {
            _db = db;
        }

        // GET: api/Menubases
        public IQueryable<Menubase> GetMenubases()
        {
            return _db.Menubases;
        }

        // GET: api/Menubases_/read?sysCode=RPTONLINE&parentId=0
        [Route("api/Menubases_/read")]
        public DataSourceResult GetGridMenu([System.Web.Http.ModelBinding.ModelBinder(typeof(WebApiDataSourceRequestModelBinder))]DataSourceRequest request, string sysCode, int? parentId)
        {
            parentId = parentId ?? 0;
            var menu = _db.Menubases.Where(p => p.SysCode == sysCode && p.ParentId == parentId);
            return menu.ToDataSourceResult(request);
        }


        // GET: api/Menubases/RPTONLINE/0
        [Route("api/Menubases_/{sysCode}/{parentId}")]
        public IQueryable<Menubase> GetMenubases(string sysCode, int parentId)
        {
            var menu = _db.Menubases.Where(p => p.SysCode == sysCode && p.ParentId == parentId);
                //.Select(s => new Menubase
                //{
                //    SysCode = s.SysCode,
                //    Sequence = s.Sequence,
                //    ParentId = s.ParentId,
                //    ChildId = s.ChildId,
                //    Description = s.Description,
                //    Action = s.Action,
                //    Controller = s.Controller,
                //    ObjectParam = s.ObjectParam,
                //    MenuId = s.MenuId,
                //    InsertedBy = s.InsertedBy,
                //    InsertedDt = s.InsertedDt,
                //    UpdatedBy = s.UpdatedBy,
                //    UpdatedDt = s.UpdatedDt
                //    //Result = "" //db.Menubases.Where(w => w.ParentId == s.ChildId).Any() ? "Submenu" : "Command"
                //});
            return menu;
        }

        // GET: api/Menubases_/access/RPTONLINE/0/userId
        [Route("api/Menubases_/access/{sysCode}/{parentId}/{userId}")]
        public IQueryable<MenubaseVM> GetMenubases(string sysCode, int parentId, string userId)
        {            
            var menu = _db.Menubases.Include(i => i.MenuAccesses).Where(p => p.SysCode == sysCode && p.ParentId == parentId)
                .Select(s => new MenubaseVM
                {
                    SysCode = s.SysCode,
                    Sequence = s.Sequence,
                    ParentId = s.ParentId,
                    ChildId = s.ChildId,
                    Description = s.Description,
                    Action = s.Action,
                    Controller = s.Controller,
                    ObjectParam = s.ObjectParam,
                    MenuId = s.MenuId,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDt = s.UpdatedDt,
                    IsAllowed = s.MenuAccesses.Any(a => a.MenuId == s.ChildId && a.UserId == userId && a.IsAllowed == true),
                    Result = _db.Menubases.Where(w => w.ParentId == s.ChildId).Any() ? "Submenu" : "Command",
                    //AccessId = s.MenuAccesses.FirstOrDefault().Id 
                    AccessId = s.MenuAccesses.FirstOrDefault(f => f.UserId == userId).Id
                });
                
            return menu;
        }

        // GET: api/Menubases_/access/adminMenu/RPTONLINE/0/userId/adminId/CTO
        [Route("api/Menubases_/access/adminMenu/{sysCode}/{parentId}/{userId}/{deptCode}")]
        public IQueryable<MenubaseVM> GetMenubases(string sysCode, int parentId, string userId, string deptCode)
        {
            if (string.IsNullOrEmpty(deptCode) || deptCode == "CORE")
            {
                return _db.Menubases.Include(i => i.MenuAccesses).Where(p => p.SysCode == sysCode && p.ParentId == parentId)
                    .Select(s => new MenubaseVM
                    {
                        SysCode = s.SysCode,
                        Sequence = s.Sequence,
                        ParentId = s.ParentId,
                        ChildId = s.ChildId,
                        Description = s.Description,
                        Action = s.Action,
                        Controller = s.Controller,
                        ObjectParam = s.ObjectParam,
                        MenuId = s.MenuId,
                        InsertedBy = s.InsertedBy,
                        InsertedDt = s.InsertedDt,
                        UpdatedBy = s.UpdatedBy,
                        UpdatedDt = s.UpdatedDt,
                        IsAllowed = true,
                        Result = _db.Menubases.Where(w => w.ParentId == s.ChildId).Any() ? "Submenu" : "Command",
                        AccessId = s.MenuAccesses.FirstOrDefault(f => f.UserId == userId).Id
                    });
            }
            else
            {
                return _db.Menubases.Include(i => i.MenuAccesses).Where(p => p.SysCode == sysCode && p.ParentId == parentId && p.ObjectParam == (string.IsNullOrEmpty(p.ObjectParam) ? p.ObjectParam : deptCode))
                    .Select(s => new MenubaseVM
                    {
                        SysCode = s.SysCode,
                        Sequence = s.Sequence,
                        ParentId = s.ParentId,
                        ChildId = s.ChildId,
                        Description = s.Description,
                        Action = s.Action,
                        Controller = s.Controller,
                        ObjectParam = s.ObjectParam,
                        MenuId = s.MenuId,
                        InsertedBy = s.InsertedBy,
                        InsertedDt = s.InsertedDt,
                        UpdatedBy = s.UpdatedBy,
                        UpdatedDt = s.UpdatedDt,
                        IsAllowed = s.MenuAccesses.Any(a => a.MenuId == s.ChildId && a.UserId == userId && a.IsAllowed == true),
                        Result = _db.Menubases.Where(w => w.ParentId == s.ChildId).Any() ? "Submenu" : "Command",
                        AccessId = s.MenuAccesses.FirstOrDefault(f => f.UserId == userId).Id
                    });
            }
        }

        
        [Route("api/Menubases_/usermenu/{userId}/{sysCode}")]
        public IQueryable<Menubase> GetUserMenubases(string userId, string sysCode)
        {
            //var menu = db.Menubases.Where(p => p.SysCode == sysCode && p.Accessfiles.Where(a => a.ChildId == p.ChildId && a.UserId == userId).Any());
            var menu = _db.Menubases.Include(i => i.MenuAccesses).Where(w => w.SysCode == sysCode && w.MenuAccesses.Any(a => a.UserId == userId && a.MenuId == w.ChildId && a.IsAllowed == true));
            return menu;
        }


        [Route("api/{sysCode}/{parentId}/test")]
        public IQueryable<Menubase> GetMenubases2(string sysCode, int parentId)
        {
            var menu = _db.Menubases.Where(p => p.SysCode == sysCode && p.ParentId == parentId);
            return menu;
        }

        // GET: api/Menubases/adminMenu/RPTONLINE/CAO
        [Route("api/Menubases_/adminMenu/{sysCode}/{deptCode}")]
        public IQueryable<Menubase> GetMenubasesLevel2(string sysCode, string deptCode)
        {
            //return db.Menubases.Where(p => p.SysCode == sysCode && string.IsNullOrEmpty(p.ObjectParam) ? true : p.ObjectParam == deptCode).OrderBy(o => o.ParentId);       
            //return db.Menubases.Where(p => p.SysCode == sysCode && p.ObjectParam == (string.IsNullOrEmpty(p.ObjectParam) ? p.ObjectParam : deptCode)).OrderBy(o => o.ParentId);
            if (string.IsNullOrEmpty(deptCode) || deptCode == "ICNS")
            {
                return _db.Menubases.Where(p => p.SysCode == sysCode).OrderBy(o => o.ParentId);
            }
            else
            {
                return _db.Menubases.Where(p => p.SysCode == sysCode && p.ObjectParam == (string.IsNullOrEmpty(p.ObjectParam) ? p.ObjectParam : deptCode)).OrderBy(o => o.ParentId);
            }
            
        }

        // GET: api/Menubases/adminMenu/RPTONLINE/CAO
        [Route("api/Menubases_/adminMenu2/{sysCode}/{deptCode}")]
        public IQueryable<Menubase> GetMenubasesLevel3(string sysCode, string deptCode)
        {
            return _db.Menubases.Where(p => p.SysCode == sysCode && p.ObjectParam == (string.IsNullOrEmpty(p.ObjectParam) ? p.ObjectParam : deptCode)).OrderBy(o => o.ParentId);
        }


        // GET: api/Menubases/RPTONLINE
        [Route("api/Menubases_/{sysCode}")]
        public IQueryable<Menubase> GetMenubasesLevel(string sysCode)
        {
            var menu = _db.Menubases.Where(p => p.SysCode == sysCode).OrderBy(o => o.ParentId);
            return menu;
        }        



        // GET: api/Menubases_/IsAuthorize/IPAY/82814eba-0738-4edd-a11f-66c8112e20de/MENU
        [Route("api/Menubases_/IsAuthorize/{sysCode}/{id}/{controllerName}")]
        public bool GetMenubasesAuthorize(string sysCode, string id, string controllerName)
        {
            bool retVal = false;
            var access = _db.Menubases.Include(i => i.MenuAccesses).Where(w => w.SysCode == sysCode && w.Controller.ToUpper() == controllerName.ToUpper() && w.MenuAccesses.Any(a => a.UserId == id)).Count();

            if (access > 0)
            {
                retVal = true;
            }

            return retVal;
        }

        //// GET: api/Menubases_/accessFILE/82814eba-0738-4edd-a11f-66c8112e20de/0/RPTAS
        //[Route("api/Menubases_/accessfiles/{userId}/{childId}/{sysCode}")]
        //public IQueryable<Accessfile> GetMenubasesAccessFiles(string userId, int childId, string sysCode)
        //{
        //    var access = db.Accessfiles.Where(w => w.UserId == userId && w.ChildId == childId && w.SysCode == sysCode);
        //    return access;
        //}

        // GET: api/Menubases_/accessRights/82814eba-0738-4edd-a11f-66c8112e20de/BILLING/RPTAS
        [Route("api/Menubases_/accessRights/{userId}/{menuId}/{sysCode}")]
        public Access GetAccessRights(string userId, int menuId, string sysCode)
        {
            var access = _db.MenuAccesses.Include(i => i.MenuAccessActions)
                .Where(w => w.Menubase.SysCode == sysCode && w.MenuId == menuId && w.UserId == userId && w.IsAllowed == true)
                .Select(s => new Access
                {
                    IsAdmin = false,
                    IsAllowed = s.IsAllowed == true ? true : false,
                    Actions = s.MenuAccessActions.Where(w => w.IsAllowed == true).ToList()
                })
                .SingleOrDefault();
            if (access == null)
            {
                return new Access();
            }
            return access;
        }

        //// GET: api/Menubases_/accessRights/82814eba-0738-4edd-a11f-66c8112e20de/BILLING/RPTAS
        //[Route("api/Menubases_/menuAccessRights/{userId}/{menuId}/{sysCode}")]
        //public Access GetAccessRights(string userId, string menuId, string sysCode)
        //{
        //    var access = db.MenuAccesses.Include(i => i.MenuAccessActions)
        //        .Where(w => w.Menubase.SysCode == sysCode && w.Menubase.MenuId == menuId && w.UserId == userId && w.IsAllowed == true)
        //        .Select(s => new Access
        //        {
        //            IsAdmin = false,
        //            IsAllowed = s.IsAllowed == true ? true : false,
        //            Actions = s.MenuAccessActions.Where(w => w.IsAllowed == true).ToList()
        //        })
        //        .SingleOrDefault();
        //    if (access == null)
        //    {
        //        return new Access();
        //    }
        //    return access;
        //}

        // GET: api/Menubases_/accessRights/82814eba-0738-4edd-a11f-66c8112e20de/BILLING/RPTAS
        [Route("api/Menubases_/menuAccessRights/{userId}/{menuId}/{sysCode}")]
        public Access GetAccessRights(string userId, string menuId, string sysCode)
        {
            var access = _db.MenuAccesses//.Include(i => i.MenuAccessActions).Include(i => i.Menubase.MenuActions)
                .Where(w => w.Menubase.SysCode == sysCode && w.Menubase.MenuId == menuId && w.UserId == userId && w.IsAllowed == true)
                .Select(s => new Access
                {
                    IsAdmin = false,
                    IsAllowed = s.IsAllowed == true ? true : false,
                    AllowAdd = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "ADD").Any(),
                    AllowEdit = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "EDIT").Any(),
                    AllowDelete = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "DELETE").Any(),
                    AllowPrint = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "PRINT").Any(),
                    AllowPost = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "POST").Any(),
                    AllowUnpost = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "UNPOST").Any(),
                    AllowTransfer = s.MenuAccessActions.Where(y => y.IsAllowed == true && y.MenuAction.ActionCode == "TRANSFER").Any(),
                    Actions = s.MenuAccessActions.Where(y => y.IsAllowed == true).ToList()
                })
                .SingleOrDefault();
            if (access == null)
            {
                return new Access();
            }
            return access;
        }

        // GET: api/Menubases/5
        [ResponseType(typeof(Menubase))]
        public async Task<IHttpActionResult> GetMenubase(int id)
        {
            Menubase menubase = await _db.Menubases.FindAsync(id);
            if (menubase == null)
            {
                return NotFound();
            }

            return Ok(menubase);
        }

        // GET: api/Menubases/5/RPTONLINE
        [ResponseType(typeof(Menubase))]
        public async Task<IHttpActionResult> GetMenubase(int id, string sysCode)
        {
            Menubase menubase = await _db.Menubases.Where(p => p.ChildId == id && p.SysCode == sysCode).SingleOrDefaultAsync();
            if (menubase == null)
            {
                return NotFound();
            }

            return Ok(menubase);
        }

        // PUT: api/Menubases/5
        [AppAuthorize("Menu")]
        [ResponseType(typeof(void))]
        [Route("api/Menubases_/update/{id}")]
        public async Task<IHttpActionResult> PutMenubase(int id, Menubase menubase)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != menubase.ChildId)
            {
                return BadRequest();
            }


            _db.Entry(menubase).State = EntityState.Modified;

            try
            {
                await _db.SaveChangesAsync();


            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MenubaseExists(menubase.ChildId))
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

        // POST: api/Menubases
        [AppAuthorize("Menu")]
        [Route("api/Menubases_/create")]
        [ResponseType(typeof(Menubase))]
        public async Task<IHttpActionResult> PostMenubase(Menubase menubase)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _db.Menubases.Add(menubase);
            await _db.SaveChangesAsync();

            //return CreatedAtRoute("DefaultApi", new { id = menubase.ChildId}, menubase); // not working error 500
            return Created(menubase.ChildId.ToString(), menubase);

        }


        // DELETE: api/Menubases/5
        [AppAuthorize("Menu")]
        [ResponseType(typeof(Menubase))]
        [Route("api/Menubases_/delete/{id}")]
        public async Task<IHttpActionResult> DeleteMenubase(int id)
        {
            Menubase menubase = await _db.Menubases.FindAsync(id);
            if (menubase == null)
            {
                return NotFound();
            }

            _db.Menubases.Remove(menubase);
            await _db.SaveChangesAsync();

            return Ok(menubase);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool MenubaseExists(int id)
        {
            return _db.Menubases.Count(e => e.ChildId == id) > 0;
        }
    }
}