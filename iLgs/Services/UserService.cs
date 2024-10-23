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
        private static string _sysCode = "PSMS";
        private static string _sysAdmin = "PSMS_ADMIN";

        private readonly AppManEntities _db = new AppManEntities();
        private readonly ICreateAndLogExceptions _exceptions = new CreateAndLogExceptions();
        private HttpClient _client;
        private string _iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;

        public UserService(AppManEntities db)
        {
            _db = db;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_iLgsApiUrl);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async ValueTask<bool> UserInRole(string userId, string role)
        {
            bool retVal = false;
            HttpResponseMessage responseMessage = await _client.GetAsync("roles_/" + userId + "/" + role).ConfigureAwait(false);
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
            var isSysAdmin = await UserInRole(userId, _sysAdmin);
            return isAdmin || isSysAdmin;
        }
    }
}