using iLgs.Models;
using Kendo.Mvc.Extensions;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class SysCodesController : BaseController
    {
        
        public ActionResult GetSysCodes(string text)
        {

            string userId = User.Identity.GetUserId();
            IEnumerable<Codextn> model = Enumerable.Empty<Codextn>().AsQueryable();
            HttpResponseMessage responseMessage = client.GetAsync("SysCodes/" + userId).Result;
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<Codextn>>(responseData);
                if (!string.IsNullOrEmpty(text))
                {
                    model = model.Where(w => w.Code.Contains(text));
                }
            }

            var retVal = model.Select(c => new { Code = c.Code, Description = c.Description }).ToList();
            retVal.Add(new { Code = "", Description = "" });

            return Json(retVal.OrderBy(o => o.Code), JsonRequestBehavior.AllowGet);

        }
    }
}