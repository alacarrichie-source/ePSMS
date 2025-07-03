using iLgs.Models;
using Kendo.Mvc.Extensions;
using Kendo.Mvc.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    public class MenuAccessActionController : Controller
    {
        private static string sysCode = "PSMS";
        private static string sysAdmin = "PSMS_ADMIN";        

        HttpClient client;

        //The URL of the WEB API Service
        //string iLgsApiUrl = ConfigurationManager.AppSettings["APPMAN_API_URL"];
        string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        //string iLgsApiUrl = "http://localhost:3684/";
        //The HttpClient Class, this will be used for performing 
        //HTTP Operations, GET, POST, PUT, DELETE
        //Set the base address and the Header Formatter

        public MenuAccessActionController()
        {            
            client = new HttpClient();
            client.BaseAddress = new Uri(iLgsApiUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }
        public async Task<ActionResult> MenuAccessActionRead([DataSourceRequest] DataSourceRequest request, string sysCode, string userId, int childId)
        {
            HttpResponseMessage responseMessage = await client.GetAsync("MenuAccessAction/" + sysCode + "/" + childId + "/" + userId);
            IEnumerable<MenuActionSw> model = Enumerable.Empty<MenuActionSw>().AsQueryable();
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                model = JsonConvert.DeserializeObject<List<MenuActionSw>>(responseData);

            }

            return Json(model.ToDataSourceResult(request));
        }
    }
}