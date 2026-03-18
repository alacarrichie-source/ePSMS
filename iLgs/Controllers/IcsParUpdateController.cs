using iLgs.Exceptions;
using iLgs.Exceptions.Service;
using iLgs.Models;
using iLgs.Services.ParIcs;
using iLgs.Utilities;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [AppAuthorize("ICSPARUPDATE")]
    public class IcsParUpdateController : BaseController
    {
        //private readonly AppManEntities _db;
        private readonly IIcsParService _icsParService;

        public IcsParUpdateController()
        {
            //_db = new AppManEntities();
            _icsParService = new IcsParService(_db);
        }

        //public IcsParUpdateController(AppManEntities db, IIcsParService icsParService)
        //{
        //    _db = db;
        //    _icsParService = icsParService;
        //}

        // GET: IcsParUpdate
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Read([DataSourceRequest] DataSourceRequest request, string refType)
        {
            var data = _icsParService.GetAll(string.Empty, refType);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        public ActionResult ItemRead([DataSourceRequest] DataSourceRequest request, string refNo, string refType)
        {
            var data = _icsParService.GetAllItems(refNo, refType);
            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };

            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Destroy([DataSourceRequest]DataSourceRequest request, IcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics_par_update");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                else
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    var result = await _icsParService.DeleteUpdatesAsync(model, user, date);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("DeleteError", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("DeleteError", e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }

        public ActionResult _Transfer(string refNo, string refType)
        {
            var issued = _db.Codextns.Where(w => w.CodeMast.Code == "ISSUED-BY").AsNoTracking().OrderByDescending(o => o.Code).FirstOrDefault();
            var date = DateTime.Now;
            var model = new IcsParVM()
            {
                PrevRefNo = refNo,
                RefType = refType,
                RefDate = date,
                ReceivedDate = date,
                IssuedDate = date,
                IssuedBy = issued?.Description,
                IssuedByPosition = issued?.Desc2,
                IssuedDept = issued?.Desc3
            };

            ViewData["refNo"] = refNo;
            ViewData["refType"] = refType;
            return PartialView(model);
        }

        public ActionResult _TransferSelectionSetRead([DataSourceRequest] DataSourceRequest request, string refNo, string refType)
        {
            var data = _icsParService.GetAllItemsForTransfer(refNo, refType);

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> TransferSave(IcsParVM model)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics_par_update");
                Access access = await accessTask;
                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    model = await _icsParService.TransferIcsPar(model, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();
            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "", Id = model.Id }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Post(string refNo, string refType)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics_par_update");
                Access access = await accessTask;
                if (!access.AllowPost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsParService.PostAsync(refNo, refType, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();

            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Unpost(string refNo, string refType)
        {
            try
            {
                Task<Access> accessTask = Access(User.Identity.GetUserId(), "ics_par_update");
                Access access = await accessTask;
                if (!access.AllowUnpost)
                {
                    ModelState.AddModelError("Access", "Access Denied!");
                }

                if (ModelState.IsValid)
                {
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    DateTime date = System.DateTime.Now;

                    await _icsParService.UnPostAsync(refNo, refType, user, date);
                }
            }
            catch (ValidationException validationException) when (validationException.InnerException is InvalidModelException)
            {
                var errors = validationException.GetErrorsForModelState();
                foreach (var error in errors)
                {
                    ModelState.AddModelError(error.Key, error.Message);
                }
            }
            catch (ValidationException validationException)
            {
                ModelState.AddModelError("", validationException.InnerException.Message);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var query = from state in ModelState.Values
                        from error in state.Errors
                        select error.ErrorMessage;

            var errorList = query.ToList();

            if (errorList.Count() > 0)
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.DenyGet);
            }

            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }
    }
}