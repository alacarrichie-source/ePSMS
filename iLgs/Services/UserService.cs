using iLgs.Exceptions;
using iLgs.Models;
using iLgs.Services.Interfaces;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace iLgs.Controllers
{
    public class UserService : IUserService
    {
        private static string sysCode = "PSMS";
        private static string sysAdmin = "PSMS_ADMIN";

        private readonly AppManEntities db = new AppManEntities();
        private readonly ICreateAndLogExceptions exceptions = new CreateAndLogExceptions();
        private HttpClient client;
        private string iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        public UserService(AppManEntities db)
        {
            this.db = db;
            this.client = new HttpClient();
            this.client.BaseAddress = new Uri(iLgsApiUrl);
            this.client.DefaultRequestHeaders.Accept.Clear();
            this.client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async ValueTask<bool> UserInRole(string userId, string role)
        {
            bool retVal = false;
            HttpResponseMessage responseMessage = await client.GetAsync("roles_/" + userId + "/" + role).ConfigureAwait(false);
            if (responseMessage.IsSuccessStatusCode)
            {
                var responseData = responseMessage.Content.ReadAsStringAsync().Result;
                var aspNetUser = JsonConvert.DeserializeObject<IEnumerable<AspNetUser>>(responseData);

                retVal = aspNetUser.Count() > 0;
            }

            return retVal;
        }

        public async ValueTask<bool> IsAdmin(string userId)
        {
            var isAdmin = await UserInRole(userId, "ADMIN");
            var isSysAdmin = await UserInRole(userId, sysAdmin);
            return isAdmin || isSysAdmin;
        }
    }
}