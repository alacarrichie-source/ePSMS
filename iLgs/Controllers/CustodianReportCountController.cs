using iLgs.Models;
using iLgs.Services;
using iLgs.Services.CustodianReports;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("CUSTODIANREPORTCOUNT")]
    public class CustodianReportCountController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly ICustodianReportSubmitForCountService _custodianReportSubmitForCountService;
        private readonly IUserService _userService;
        private string _menuId = string.Empty;
        
        public CustodianReportCountController()
        {
            //_db = db;
            _custodianReportSubmitForCountService = new CustodianReportSubmitForCountService(_db);
            _userService = new UserService(_db);            
        }

        // GET: Index
        public async Task<ActionResult> Index()
        {
            _menuId = "custodian_report_count";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["custodian_report_count"] = _menuId;
            ViewBag.ForYear = DateTime.Now.Year;
            return View();
        }

        public async Task<ActionResult> Department()
        {
            _menuId = "custodian_report_count_department";
            var access = await Access(User.Identity.GetUserId(), _menuId);
            if (!access.IsAllowed)
            {
                ViewBag.Error = "Access Denied!";
                return View("Error");
            }

            TempData["custodian_report_count"] = _menuId;
            ViewBag.ForYear = DateTime.Now.Year;
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, int? forYear)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportSubmitForCountService.GetByReportYear(forYear);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }

        public ActionResult DepartmentRead([DataSourceRequest] DataSourceRequest request, int? forYear)
        {
            string user = ControllerContext.HttpContext.User.Identity.Name;
            var data = _custodianReportSubmitForCountService.GetDepartmentsByReportYear(forYear);

            return new JsonNetResult { Data = data.ToDataSourceResult(request), JsonRequestBehavior = JsonRequestBehavior.AllowGet, Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore } };
        }
    }
}