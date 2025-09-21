using iLgs.Exceptions;
using iLgs.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace iLgs.Services
{
    public interface IUserService
    {
        ValueTask<bool> UserInRole(string userId, string role);
        ValueTask<bool> IsAdminAsync(string userId);

        bool IsUserNameAdmin(string userName);
        //bool IsAnnexDUser(string userName);

        AspNetUser GetByUserName(string userName);
        AspNetUser GetById(string userId);
    }

    public class UserService : IUserService
    {
        private static string _sysCode = "PSMS";
        private static string _sysAdmin = "PSMS_ADMIN";

        private readonly AppManEntities _db;
        private readonly ICreateAndLogExceptions _exceptions;
        private HttpClient _client;
        private string _iLgsApiUrl = new SqlConnectionStringBuilder(ConfigurationManager.ConnectionStrings["APPMAN_API_URL"].ToString()).DataSource;
        //private IAnnexDService _annexDService;

        public UserService(AppManEntities db,
            ICreateAndLogExceptions exceptions)
        {
            _db = db;
            _exceptions = exceptions;
            _client = new HttpClient();
            _client.BaseAddress = new Uri(_iLgsApiUrl);
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //_annexDService = new AnnexDService(_db);
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

        public async ValueTask<bool> IsAdminAsync(string userId)
        {
            var isAdmin = await UserInRole(userId, "ADMIN");
            var isSysAdmin = await UserInRole(userId, _sysAdmin);
            return isAdmin || isSysAdmin;
        }

        public bool IsUserNameAdmin(string userName)
        {
            var user = _db.AspNetUsers.Where(w => w.UserName == userName).SingleOrDefault();
            var userId = user.Id;            
            var isAdmin = UserInRole(userId, "ADMIN").Result;
            var isSysAdmin = UserInRole(userId, _sysAdmin).Result;
            return isAdmin || isSysAdmin;
        }

        public AspNetUser GetByUserName(string userName)
        {
            var data = _db.AspNetUsers.Where(w => w.UserName == userName).SingleOrDefault();
            return data;
        }

        public AspNetUser GetById(string userId)
        {
            var data = _db.AspNetUsers.Where(w => w.Id == userId).SingleOrDefault();
            return data;
        }

        //public bool IsAnnexDUser(string userName)
        //{
        //    bool retVal = false;
        //    if (IsUserNameAdmin(userName))
        //    {
        //        retVal = true;
        //    }
        //    else
        //    {
        //        retVal = _annexDService.IsAny(userName);
        //    }
        //    return retVal;
        //}
    }
}