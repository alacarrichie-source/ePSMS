using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using iLgs.Models;
using iLgs.Services.Codes;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class IcsParRptController : BaseController
    {
        //private AppManEntities _db = new AppManEntities();
        private ICodextnService _codextnService;

        public IcsParRptController()
        {
            //_db = db;
            _codextnService = new CodextnService(_db);
        }

        public async Task<ActionResult> IcsSetRpt(string icsNo)
        {
            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/IcsSet.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;
            crConnectionInfo.IntegratedSecurity = false;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cRefNo", icsNo);
            rpt.SetParameterValue("@cRefType", "I");
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }

        public async Task<ActionResult> ParSetRpt(string parNo)
        {            
            Sections crSections;
            ReportDocument rpt, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            rpt = new ReportDocument();
            rpt.FileName = Server.MapPath(Url.Content("~/Reports/ParSet.rpt"));
            rpt.Refresh();

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);
            string rptKey = ConfigurationManager.AppSettings["RptKey"];
            string un = decoder.UserID;
            string pw = rptKey; // decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = rpt.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;
            crConnectionInfo.IntegratedSecurity = false;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = rpt.ReportDefinition.Sections;
            // loop through all the sections to find all the report objects 
            foreach (CrystalDecisions.CrystalReports.Engine.Section crSection in crSections)
            {
                crReportObjects = crSection.ReportObjects;
                //loop through all the report objects in there to find all subreports 
                foreach (ReportObject crReportObject in crReportObjects)
                {
                    if (crReportObject.Kind == ReportObjectKind.SubreportObject)
                    {
                        crSubreportObject = (SubreportObject)crReportObject;
                        //open the subreport object and logon as for the general report 
                        crSubreportDocument = crSubreportObject.OpenSubreport(crSubreportObject.SubreportName);
                        crDatabase = crSubreportDocument.Database;
                        crTables = crDatabase.Tables;
                        foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
                        {
                            crTableLogOnInfo = aTable.LogOnInfo;
                            crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                            aTable.ApplyLogOnInfo(crTableLogOnInfo);
                        }
                    }
                }
            }

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault().Description;

            rpt.SetParameterValue("@cRefNo", parNo);
            rpt.SetParameterValue("@cRefType", "P");
            rpt.SetParameterValue("LGU", lgu);

            Stream stream = rpt.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
            rpt.Close();
            rpt.Dispose();
            return File(stream, "application/pdf");
        }
    }
}