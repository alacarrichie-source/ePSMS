using iLgs.Models;
using Microsoft.AspNet.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Data.Entity;
using Microsoft.AspNet.SignalR;
using iLgs.Utilities;
using iLgs.Services.CustodianReports;

namespace iLgs.Controllers
{
    [System.Web.Mvc.Authorize]
    public class ChatController : BaseController
    {
        //private AppManEntities _db;
        private readonly INotificationMessageService _notificationMessageService;

        public ChatController()
        {
            //_db = db;
            _notificationMessageService = new NotificationMessageService(_db);
        }

        // GET: Chat
        public ActionResult Index()
        {
            return View();
        }

        public async Task<ActionResult> _Chat(Guid? conversationId, string notificationName, string notificationMessage)
        {
            var messages = await _db.Chats.Include(i => i.AspNetUser)
                .Where(m => m.ConversationId == conversationId)
                .OrderBy(m => m.SentAt).ToListAsync();
            
            ViewBag.ConversationId = conversationId;
            ViewBag.NotificationName = notificationName;
            ViewBag.NotificationMessage = notificationMessage;
            return PartialView(messages);
        }

        public ActionResult _ChatScripts()
        {
            return PartialView();
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public async Task<ActionResult> Send(Guid? conversationId, string text, string notificationName, string notificationMessage)
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var userName = User.Identity.Name;
                var message = new Chat
                {
                    Id = Guid.NewGuid(),
                    ConversationId = conversationId,
                    SenderId = userId,
                    SentAt = DateTime.Now,
                    Text = text
                };

                _db.Chats.Add(message);
                await _db.SaveChangesAsync();
                
                var chatHub = new ChatHub();
                chatHub.Send(conversationId.ToString(), userName, message);

                if (!string.IsNullOrWhiteSpace(notificationName) && !string.IsNullOrWhiteSpace(notificationMessage))
                {                    
                    await _notificationMessageService.NotifyUsers(notificationName, "Update", notificationMessage, userName, DateTime.Now);
                }

                return new HttpStatusCodeResult(200);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            var errorList = ModelState.Where(ms => ms.Value.Errors.Any())
                       .Select(ms => new
                       {
                           Key = ms.Key, // The field name
                           Message = ms.Value.Errors.Select(e =>
                           {
                               var errorMessage = e.ErrorMessage;
                               if (e.Exception != null)
                               {
                                   var exceptionMessage = e.Exception.Message;
                                   var innerExceptionMessage = e.Exception.InnerException?.Message;

                                   // Append exception details
                                   errorMessage += $" Exception: {exceptionMessage}";
                                   if (innerExceptionMessage != null)
                                   {
                                       errorMessage += $" InnerException: {innerExceptionMessage}";
                                   }
                               }

                               return errorMessage;
                           }).ToList() // List of messages for the current field
                       })
                       .ToList();

            if (errorList.Any())
            {
                return Json(new { Errors = errorList }, JsonRequestBehavior.AllowGet);
            }

            //return RedirectToAction("Conversation", new { id = conversationId });
            return Json(new { Errors = "" }, JsonRequestBehavior.AllowGet);
        }        
    }
}