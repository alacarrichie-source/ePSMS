using iLgs.Models;
using iLgs.Services.AuditLog_;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class AuditTrailController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly IAuditLogService _auditLogService;

        public AuditTrailController(AppManEntities db, IAuditLogService auditLogService)
        {
            _db = db;
            _auditLogService = auditLogService;
        }

        // GET: AuditLog
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult _AuditTrail(string trackingId)
        {
            ViewData["trackingId"] = trackingId;
            return PartialView();
        }

        public ActionResult _AuditTrailScripts()
        {
            return PartialView();
        }

        public JsonResult GetAuditLogs([DataSourceRequest] DataSourceRequest request, string trackingId)
        {
            var auditLogs = _auditLogService.GetAuditLogsQueryable(trackingId); // Make this IQueryable

            // Apply filtering, sorting, paging
            var result = auditLogs.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetAuditDetails(Guid? auditLogId, [DataSourceRequest] DataSourceRequest request)
        {
            var auditDetails = _auditLogService.GetAuditDetailsQueryable(auditLogId);

            var result = auditDetails.ToDataSourceResult(request);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        //public JsonResult GetAuditLogs([DataSourceRequest] DataSourceRequest request,
        //string tableName = null, string action = null, string user = null, DateTime? fromDate = null, DateTime? toDate = null)
        //{
        //    var query = _auditLogService.GetAuditLogsQueryable();

        //    // Apply filters
        //    if (!string.IsNullOrEmpty(tableName))
        //        query = query.Where(al => al.TableName.Contains(tableName));

        //    if (!string.IsNullOrEmpty(action))
        //        query = query.Where(al => al.Action == action);

        //    if (!string.IsNullOrEmpty(user))
        //        query = query.Where(al => al.UpdatedBy.Contains(user));

        //    if (fromDate.HasValue)
        //        query = query.Where(al => al.UpdatedDt >= fromDate.Value);

        //    if (toDate.HasValue)
        //        query = query.Where(al => al.UpdatedDt <= toDate.Value.AddDays(1));

        //    var result = query
        //        .Select(al => new AuditLogVM
        //        {
        //            Id = al.Id,
        //            TableName = al.TableName,
        //            Action = al.Action,
        //            RecordId = al.RecordId,
        //            UpdatedBy = al.UpdatedBy,
        //            UpdatedDt = al.UpdatedDt,
        //            IpAddress = al.IpAddress,
        //            DetailsCount = al.DetailsCount
        //        })
        //        .OrderByDescending(al => al.UpdatedDt)
        //        .ToDataSourceResult(request);

        //    return Json(result, JsonRequestBehavior.AllowGet);
        //}
    }
}