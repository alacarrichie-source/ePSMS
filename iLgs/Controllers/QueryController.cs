using CrystalDecisions.CrystalReports.Engine;
using iLgs.Models;
using iLgs.Services.Codes;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System.Configuration;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class QueryController : BaseController
    {
        private readonly AppManEntities _db;
        private readonly ICodextnService _codextnService;

        public QueryController(AppManEntities db, ICodextnService codextnService)
        {
            _db = db;
            _codextnService = codextnService;
        }        

        public ActionResult Po()
        {
            var data = new PoQueryVM()
            {
                PoStatus = 3
            };
            return View(data);
        }

        public ActionResult PoRead([DataSourceRequest] DataSourceRequest request, string userName, int? poStatus)
        {
            var data = _db.Database.SqlQuery<QueryPoVM>("Exec Card_GetPoNumbers {0}, {1}", userName, poStatus).AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult PoItemRead([DataSourceRequest] DataSourceRequest request, string poNo, string userName)
        {
            var data = _db.PsCardItems
                .Include(i => i.PsCard.ItemCode.ItemType.Description)
                .Where(w => (w.PoNo == poNo || (w.PoNo == null && string.IsNullOrEmpty(poNo))) && (w.InsertedBy == userName || string.IsNullOrEmpty(userName))).AsNoTracking()
                .Select(s => new PsCardItemVM {
                    Id = s.Id,
                    Account = s.PsCard.ItemCode.ItemType.Description,
                    StockNo = s.PsCard.PsNo,
                    Description = s.Description,
                    Unit = s.Unit,
                    Qty = s.Qty,
                    Amount = s.Amount,
                    InsertedBy = s.InsertedBy,
                    InsertedDt = s.InsertedDt,
                    UpdatedBy = s.UpdatedBy,
                    UpdatedDt = s.UpdatedDt
                })
                .AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult PoRpt(string poNo, int originalSw)
        {
            string stringname = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(stringname);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            ReportClass rpt = new ReportClass();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/Card_Po_.rpt"));
            rpt.SetDatabaseLogon(un, pw, svr, db_);

            rpt.Load();
            rpt.Refresh();

            foreach (Table table in rpt.Database.Tables)
            {
                var logonInfo = table.LogOnInfo;
                logonInfo.ConnectionInfo.ServerName = svr;
                logonInfo.ConnectionInfo.DatabaseName = db_;
                logonInfo.ConnectionInfo.UserID = un;
                logonInfo.ConnectionInfo.Password = pw;
                logonInfo.ConnectionInfo.IntegratedSecurity = false;
                table.ApplyLogOnInfo(logonInfo);
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;
            
            rpt.SetParameterValue("@cPoNo", string.IsNullOrWhiteSpace(poNo) ? null : poNo);
            rpt.SetParameterValue("LGU", lgu);
            rpt.SetParameterValue("IsOriginal", originalSw == 1);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

    }
}