using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;
using CrystalDecisions.Shared;
using CrystalDecisions.CrystalReports.Engine;
using System.Data.SqlClient;
using System.IO;
using iLgs.Services.Interfaces;
using iLgs.Services;

namespace iLgs.Controllers
{
    [AppAuthorize("RSMI")]
    public class RSMIController : BaseController
    {
        private AppManEntities _db = new AppManEntities();
        private ICodextnService _codextnService;

        public RSMIController()
        {
            _codextnService = new CodextnService(_db);
        }

        // GET: RSMI
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult RSMIRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = _db.RSMIs.AsQueryable();
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult _RSMIAdd()
        {

            RSMIProcessVM model = new RSMIProcessVM()
            {
                DateFrom = DateTime.Now,
                DateTo = DateTime.Now
            };
            return PartialView(model);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> _RSMIAddSave([DataSourceRequest] DataSourceRequest request, RSMIProcessVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rsmi");
                Access access = await accessTask;
                if (!access.AllowAdd)
                {
                    ModelState.AddModelError("", "Add Access Denied!");
                }
                else
                {
                    if (_db.RSMIs.Any(a => a.Date >= model.DateFrom && a.Date <= model.DateTo))
                    {
                        ModelState.AddModelError("Period", "Period entered already exists..");
                    }
                }

                if (model != null && ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;


                    var rsmiItemList = _db.PsCardItemIssuances.AsNoTracking()
                        .Where(w => w.IssuedDate >= model.DateFrom && w.IssuedDate <= model.DateTo)
                        .Select(s => new RSMIItemVM
                        {
                            RisNo = s.PsCardItem.OrderItem.RequestItem.RisItem.RISs.RisNo,
                            Date = s.IssuedDate,
                            Fund = s.PsCardItem.PsCard.Fund,
                            RCC = s.PsCardItem.Codextn.Code,
                            //s.PsCardItem.Codextn.Description,
                            StockNo = s.PsCardItem.PsCard.PsNo,
                            ItemName = s.PsCardItem.PsCard.ItemCode.ItemType.Code == "W" ? s.PsCardItem.Description : s.PsCardItem.PsCard.Description,
                            Unit = s.PsCardItem.Unit,
                            UnitCost = s.PsCardItem.UnitCost,
                            Qty = s.Qty,
                            Amount = s.Amount,
                            AccountCode = s.PsCardItem.PsCard.ItemCode.AccountCode
                        }).ToList();

                    if (!rsmiItemList.Any())
                    {
                        ModelState.AddModelError("Period", "No Issuances found on period entered.");
                    }
                    else
                    {
                        var rsmiDateList = rsmiItemList.GroupBy(g => new { g.Date, g.Fund, g.RCC })
                            .Select(s => new { s.Key.Date, s.Key.Fund, s.Key.RCC }).ToList();
                        foreach(var rsmiDate in rsmiDateList)
                        {
                            var serialNo = NextSerialNo(rsmiDate.Date);
                            var entity = new RSMI()
                            {
                                Id = Guid.NewGuid(),
                                Date = rsmiDate.Date,
                                Fund = rsmiDate.Fund,
                                SerialNo = serialNo,
                                Custodian = model.Custodian,
                                PostedBy = model.PostedBy,
                                PostedDt = model.PostedDt,
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            var itemIssuedList = rsmiItemList
                                .Where(w => w.Fund == rsmiDate.Fund && w.Date == rsmiDate.Date).ToList();

                            foreach (var itemIssued in itemIssuedList)
                            {                                
                                var rsmiItem = new RSMIItem()
                                {
                                    Id = Guid.NewGuid(),
                                    RsmiId = entity.Id,
                                    RisNo = itemIssued.RisNo,
                                    RCC = itemIssued.RCC,
                                    StockNo = itemIssued.StockNo,
                                    ItemName = itemIssued.ItemName,
                                    Unit = itemIssued.Unit,
                                    UnitCost = itemIssued.UnitCost,
                                    Qty = itemIssued.Qty,
                                    Amount = itemIssued.Amount,
                                    AccountCode = itemIssued.AccountCode,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                entity.RSMIItems.Add(rsmiItem);
                            }

                            var recapList = entity.RSMIItems.GroupBy(g => new { g.StockNo, g.AccountCode, g.UnitCost })
                                .Select(s => new
                                {
                                    StockNo = s.Key.StockNo,
                                    AccountCode = s.Key.AccountCode,
                                    UnitCost = s.Key.UnitCost,
                                    Qty = s.Sum(f => f.Qty),
                                    TotalCost = s.Sum(f => f.Amount)
                                }).ToList();

                            foreach (var recap in recapList)
                            {
                                var rsmiRecap = new RSMIRecap()
                                {
                                    Id = Guid.NewGuid(),
                                    RsmiId = entity.Id,
                                    StockNo = recap.StockNo,
                                    Qty = recap.Qty,
                                    UnitCost = recap.UnitCost,
                                    TotalCost = recap.TotalCost,
                                    AccountCode = recap.AccountCode,
                                    InsertedBy = user,
                                    InsertedDt = date,
                                    UpdatedBy = user,
                                    UpdatedDt = date
                                };
                                entity.RSMIRecaps.Add(rsmiRecap);
                            }

                            _db.RSMIs.Add(entity);
                            await _db.SaveChangesAsync();
                        }
                    }                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }
            else
            {
                return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
            }                        
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public async Task<ActionResult> RSMICreate([DataSourceRequest] DataSourceRequest request, RSMIProcessVM model)
        //{
        //    try
        //    {
        //        Task<Access> accessTask = Access(User.Identity.GetUserId(), "rsmi");
        //        Access access = await accessTask;
        //        if (!access.AllowAdd)
        //        {
        //            ModelState.AddModelError("", "Add Access Denied!");
        //        }
        //        else
        //        {
        //            if (db.RSMIs.Any(a => a.Date >= model.DateFrom && a.Date <= model.DateTo))
        //            {
        //                ModelState.AddModelError("Period", "Period entered already exists..");
        //            }
        //        }

        //        if (model != null && ModelState.IsValid)
        //        {
        //            string user = ControllerContext.HttpContext.User.Identity.Name;
        //            DateTime date = System.DateTime.Now;

        //            //var risDates = db.RISlips.Where(w => w.RisDate >= model.DateFrom && w.RisDate <= model.DateTo)
        //            //                .AsNoTracking()
        //            //                .GroupBy(g => new { g.RisDate, g.Fund })
        //            //                .Select(s => new { Date = s.Key.RisDate, Fund = s.Key.Fund });

        //            var funds = await db.Codextns.Where(w => w.CodeMast.Code == "FUND")
        //                .Select(s => new
        //                {
        //                    Code = s.Code
        //                }).ToListAsync();

        //            //var risDates = db.Orders.Where(a => a.PoDate >= model.DateFrom && a.PoDate <= model.DateTo
        //            //    && a.RISlips.Any())
        //            //    .AsNoTracking()
        //            //    .GroupBy(g => new { g.PoDate })
        //            //    .Select(s => new { Date = s.Key.PoDate });
                    
        //            var risDates = db.RisIssueds.AsNoTracking().Where(w => w.IssuedDate >= model.DateFrom && w.IssuedDate <= model.DateTo
        //                && w.RisItem.RISs.PostedDt != null
        //                ).GroupBy(g => g.IssuedDate)
        //                .Select(s => new { Date = s.Key });


        //            foreach (var fund in funds)
        //            {
        //                foreach (var risDate in risDates)
        //                {
        //                    var serialNo = NextSerialNo(risDate.Date);
        //                    var entity = new RSMI()
        //                    {
        //                        Id = Guid.NewGuid(),
        //                        Date = risDate.Date,
        //                        Fund = fund.Code,
        //                        SerialNo = serialNo,
        //                        Custodian = model.Custodian,
        //                        PostedBy = model.PostedBy,
        //                        PostedDt = model.PostedDt,
        //                        InsertedBy = user,
        //                        InsertedDt = date,
        //                        UpdatedBy = user,
        //                        UpdatedDt = date
        //                    };

        //                    //var riSlips = db.RISlips.Where(w => w.Order.PoDate == risDate.Date && w.Fund == fund.Code)
        //                    //    .AsNoTracking()
        //                    //    .Select(s => new { Id = s.Id, RISlipItems = s.RISlipItems });
        //                    var riSlips = db.RisIssueds.AsNoTracking()
        //                        .Where(w => w.IssuedDate == risDate.Date && w.RisItem.RISs.Fund == fund.Code)
        //                        .Select(s => new { Id = s.Id, RisItemId = s.RisItemId });
        //                    foreach (var riSlip in riSlips)
        //                    {
        //                        var rsmiItem = new RSMIItem()
        //                        {
        //                            Id = Guid.NewGuid(),
        //                            RsmiId = entity.Id,
        //                            RisIssuedId = riSlip.Id,
        //                            InsertedBy = user,
        //                            InsertedDt = date
        //                        };
        //                        entity.RSMIItems.Add(rsmiItem);

        //                        //var orderItems = db.OrderItems.Where(w => w.RequestItem.RisItem.Id == riSlip.RisItemId);

        //                        //foreach (var orderItem in orderItems)
        //                        //{
        //                        //    var psItem = db.PsItems.Where(w => w.OrderItemId == )
        //                        //    var rsmiRecap = new RSMIRecap()
        //                        //    {
        //                        //        Id = Guid.NewGuid(),
        //                        //        RsmiId = entity.Id,
        //                        //        PsItemId = orderItem.PsItem.Id,
        //                        //        StockNo = orderItem.PsItem.PsStock.StockNo,
        //                        //        Qty = orderItem.IssQty,
        //                        //        UnitCost = orderItem.UnitCost,
        //                        //        TotalCost = orderItem.Amount,
        //                        //        AccountCode = ""
        //                        //    };
        //                        //    entity.RSMIRecaps.Add(rsmiRecap);
        //                        //}
        //                    }

        //                    db.RSMIs.Add(entity);
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception e)
        //    {
        //        ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
        //             "please contact tech support with this message: " + e.Message);
        //    }

        //    return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RSMIUpdate([DataSourceRequest] DataSourceRequest request, RsmiVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rsmi");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("", "Update Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model.UpdatedBy = user;
                    model.UpdatedDt = date;

                    var entity = _db.RSMIs.Find(model.Id);
                    entity.Custodian = model.Custodian;
                    entity.PostedBy = model.PostedBy;
                    entity.PostedDt = model.PostedDt;
                    entity.UpdatedBy = model.UpdatedBy;
                    entity.UpdatedDt = model.UpdatedDt;

                    _db.RSMIs.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> RSMIDestroy([DataSourceRequest]DataSourceRequest request, RsmiVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "rsmi");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var entity = _db.RSMIs.Find(model.Id);

                    entity.UpdatedBy = user;
                    entity.UpdatedDt = date;

                    _db.RSMIs.Attach(entity);
                    _db.Entry(entity).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    entity = _db.RSMIs.Find(model.Id);
                    _db.RSMIs.Attach(entity);
                    _db.RSMIs.Remove(entity);
                    await _db.SaveChangesAsync();                    
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", "Unable to save changes, Try again, and if the problem persists " +
                     "please contact tech support with this message: " + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult RSMIItemRead([DataSourceRequest] DataSourceRequest request, Guid? rsmiId)
        {
         
            var data = _db.RSMIItems.Where(w => w.RsmiId == rsmiId)
                .Select(s => new RSMIItemVM
                {
                    Id = s.Id,
                    RisNo = s.RisNo,
                    StockNo = s.StockNo,
                    RCC = s.RCC,
                    ItemName = s.ItemName,
                    Qty = s.Qty,
                    Unit = s.Unit,
                    UnitCost = s.UnitCost,
                    Amount = s.Amount
                }).AsQueryable();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult RSMIRecapRead([DataSourceRequest] DataSourceRequest request, Guid? rsmiId)
        {

            var data = _db.RSMIRecaps.Where(w => w.RsmiId == rsmiId)
                .Select(s => new RSMIRecapVM
                {
                    Id = s.Id,
                    StockNo = s.StockNo,
                    Qty = s.Qty,
                    UnitCost = s.UnitCost,
                    TotalCost = s.TotalCost,
                    AccountCode = s.AccountCode
                }).AsQueryable();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        #region PRINTING
        public ActionResult _PrintRsmi()
        {
            var date = DateTime.Now;
            var model = new RsmiPrintVM()
            {
                DateFrom = date,
                DateTo = date
            };

            return PartialView(model);
        }

        public async Task<ActionResult> RsmiRpt(RsmiPrintVM model)
        {
            Sections crSections;
            ReportDocument crReportDocument, crSubreportDocument;
            SubreportObject crSubreportObject;
            ReportObjects crReportObjects;
            ConnectionInfo crConnectionInfo;
            CrystalDecisions.CrystalReports.Engine.Database crDatabase;
            Tables crTables;
            TableLogOnInfo crTableLogOnInfo;
            crReportDocument = new ReportDocument();
            crReportDocument.FileName = Server.MapPath(Url.Content("~/Reports/Rsmi.rpt"));

            string user = ControllerContext.HttpContext.User.Identity.Name;
            string conString = _db.Database.Connection.ConnectionString.ToString();
            SqlConnectionStringBuilder decoder = new SqlConnectionStringBuilder(conString);

            string un = decoder.UserID;
            string pw = decoder.Password;
            string svr = decoder.DataSource;
            string db_ = decoder.InitialCatalog;

            crDatabase = crReportDocument.Database;
            crTables = crDatabase.Tables;
            crConnectionInfo = new ConnectionInfo();
            crConnectionInfo.ServerName = svr;
            crConnectionInfo.DatabaseName = db_;
            crConnectionInfo.UserID = un;
            crConnectionInfo.Password = pw;

            foreach (CrystalDecisions.CrystalReports.Engine.Table aTable in crTables)
            {
                crTableLogOnInfo = aTable.LogOnInfo;
                crTableLogOnInfo.ConnectionInfo = crConnectionInfo;
                aTable.ApplyLogOnInfo(crTableLogOnInfo);
            }
            // THIS STUFF HERE IS FOR REPORTS HAVING SUBREPORTS 
            // set the sections object to the current report's section 
            crSections = crReportDocument.ReportDefinition.Sections;
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

            var lgu = _codextnService.GetByMastCode("LGU").Where(w => w.Code == "Name").FirstOrDefault()?.Description;

            crReportDocument.SetParameterValue("LGU", lgu);
            crReportDocument.SetParameterValue("@cFund", model.Fund);
            crReportDocument.SetParameterValue("@dBdate", model.DateFrom);
            crReportDocument.SetParameterValue("@dEdate", model.DateTo);            
            
            if (model.SavePrints)
            {
                Stream stream = crReportDocument.ExportToStream(CrystalDecisions.Shared.ExportFormatType.Excel);
                crReportDocument.Close();
                crReportDocument.Dispose();
                return File(stream, "application/xlsx");
            }
            else
            {
                Stream stream = crReportDocument.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat);
                crReportDocument.Close();
                crReportDocument.Dispose();
                return File(stream, "application/pdf");                
            }
        }
        #endregion

        public string NextSerialNo(DateTime? date)
        {
            string yyyy = date.Value.Year.ToString().Trim();
            string mm = date.Value.Month.ToString().Trim();

            mm = mm.Substring(0, mm.Length).PadLeft(2, '0');

            string keyName = yyyy + "-" + mm;
            // yyyy-mm-9999
            // 123456789012

            var data = _db.RSMIs.Where(w => w.Date.Value.Year == date.Value.Year && w.Date.Value.Month == date.Value.Month).OrderByDescending(o => o.SerialNo).FirstOrDefault();
            if (data == null)
            {
                return keyName + "-" + "0001";
            }
            else
            {
                var sequence = (int.Parse(data.SerialNo.Split('-')[2]) + 1).ToString();
                return keyName + "-" + sequence.PadLeft(4, '0');
            }
        }
    }
}