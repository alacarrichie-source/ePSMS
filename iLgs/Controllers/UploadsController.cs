using iLgs.Models;
using Kendo.Mvc.UI;
using Kendo.Mvc.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using iLgs.Utilities;
using Newtonsoft.Json;
using System.IO;
using System.Configuration;

namespace iLgs.Controllers
{
    public class UploadsController : Controller
    {
        private AppManEntities db = new AppManEntities();
        // GET: Uploads
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult _Upload()
        {
            ViewData["fileSize"] = 10;
            return PartialView();
        }

        public ActionResult UploadRead([DataSourceRequest] DataSourceRequest request)
        {
            var data = db.Uploads.AsQueryable();

            var result = new JsonNetResult
            {
                Data = data.ToDataSourceResult(request),
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                Settings = { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }
            };
            return result;
        }

        public async Task<ActionResult> UploadDestroy([DataSourceRequest]DataSourceRequest request, iLgs.Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "uploads");
                Access access = await accessTask;
                if (!access.AllowDelete)
                {
                    ModelState.AddModelError("DeleteError", "Delete Access Denied!");
                }
                
                if (ModelState.IsValid)
                {
                    var entity = db.Uploads.Find(model.Id);

                    db.Uploads.Attach(entity);
                    // Delete the entity
                    db.Uploads.Remove(entity);
                    // Or use DeleteObject if using a previous version of Entity Framework
                    // Delete the entity in the database
                    //db.Entry(model).State = System.Data.EntityState.Deleted;
                    await db.SaveChangesAsync();
                    //db.Configuration.ValidateOnSaveEnabled = true;  

                    var directory = model.VirtualDirectory;
                    var fileName = model.FileName;
                    var physicalPath = Path.Combine(Server.MapPath(directory), fileName);
                    var appPathDirectory = System.IO.Path.GetDirectoryName(physicalPath);

                    if (System.IO.File.Exists(physicalPath))
                    {
                        // The files are not actually removed in this demo
                        System.IO.File.Delete(physicalPath);
                    }
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
        public async Task<ActionResult> UploadUpdate([DataSourceRequest] DataSourceRequest request, iLgs.Models.Upload model)
        {
            try
            {
                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "uploads");
                Access access = await accessTask;

                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Updating of Record is not allowed, please verify...");
                }


                if (ModelState.IsValid)
                {

                    var entity = db.Uploads.Find(model.Id);

                    if (entity != null)
                    {
                        string user = ControllerContext.HttpContext.User.Identity.Name;
                        entity.Description = model.Description;
                        entity.UpdatedBy = user;
                        entity.UpdatedDt = System.DateTime.Now;

                        db.Uploads.Attach(entity);
                        await db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save/delete changes, Try again, and if the problem persists " +
                "please contact tech support with this message:" + e.Message);
            }

            return Json(new[] { model }.ToDataSourceResult(request, ModelState));
        }


        public async Task<ActionResult> Upload(IEnumerable<HttpPostedFileBase> files, iLgs.Models.Upload model)
        {
            try
            {

                Task<Access> accessTask = new HomeController().Access(User.Identity.GetUserId(), "faas_records");
                Access access = await accessTask;

                if (!access.AllowEdit)
                {
                    ModelState.AddModelError("Access", "Updating of Record is not allowed, please verify...");
                }
                else
                {

                    string directory = ConfigurationManager.AppSettings["ImageFolder"].ToString();
                    var supportedTypes = new[] { "pdf", "jpg", "jpeg", "png" };
                    string user = ControllerContext.HttpContext.User.Identity.Name;
                    var date = DateTime.Now;

                    foreach (var file in files)
                    {
                        var fileSize_MB = 10;
                        if (file.ContentLength > (10240) * 100 * fileSize_MB)
                        {
                            ModelState.AddModelError("photo", string.Format("the size of the file should not exceed {0} MB", fileSize_MB));
                        }
                        var fileExt = System.IO.Path.GetExtension(file.FileName).Substring(1).ToLower();
                        if (!supportedTypes.Contains(fileExt))
                        {
                            ModelState.AddModelError("photo", "Invalid type.");
                            return Content("Invalid type..");
                        }
                        else
                        {
                            var recId = Guid.NewGuid();
                            //var fileName = Path.GetFileName(file.FileName);
                            var fileName = recId.ToString() + '.' + fileExt;
                            //var physicalPath = Path.Combine(Server.MapPath(directory), fileName);
                            var physicalPath = Path.Combine(directory, fileName);
                            //var appPathDirectory = System.IO.Path.GetDirectoryName(physicalPath);
                            var hostaddress = Request.UserHostAddress;
                            file.SaveAs(physicalPath);

                            var entity = new iLgs.Models.Upload()
                            {
                                Id = recId,
                                FileName = fileName,
                                ServerIpAddress = hostaddress,
                                VirtualDirectory = directory,
                                Description = model.Description,                                
                                InsertedBy = user,
                                InsertedDt = date,
                                UpdatedBy = user,
                                UpdatedDt = date
                            };

                            db.Uploads.Add(entity);
                            await db.SaveChangesAsync();
                        }
                    }
                    return Content("");
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", "Unable to save/delete changes, Try again, and if the problem persists " +
                "please contact tech support with this message:" + e.Message);
            }
            //return Json(new[] { model }.ToDataSourceResult(request, ModelState));
            return Content("Error Uploading files..");
        }

        public ActionResult Preview(Guid id)
        {
            string UserName = ControllerContext.HttpContext.User.Identity.Name.ToUpper();
            string directory = ConfigurationManager.AppSettings["ImageFolder"].ToString();

            var physicalPath = "";
            var fileName = "";

            var images = db.Uploads.Find(id);

            fileName = images.FileName;
            physicalPath = Path.Combine(Server.MapPath(directory), fileName);


            var fileExt = System.IO.Path.GetExtension(fileName).Substring(1).ToLower();

            if (fileExt == "pdf") //(System.IO.File.Exists(physicalPath))
            {
                return File(physicalPath, "application/pdf");
            }
            else
            {
                physicalPath = Path.Combine(Server.MapPath(directory), fileName);
                //return new FileStreamResult( new FileStream(physicalPath, FileMode.Open), "image/jpg");
                return File(physicalPath, "image/jpg");
            }
        }
    }
}