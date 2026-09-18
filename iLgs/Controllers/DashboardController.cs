using iLgs.Models;
using iLgs.Services;
using iLgs.Services.Dashboard;
using Microsoft.AspNet.Identity;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace iLgs.Controllers
{
    [Authorize]
    public class DashboardController : BaseController
    {
        private readonly IDashboardService _dashboardService;
        private readonly IUserService _userService;

        public DashboardController()
        {
            _userService = new UserService(_db);
            _dashboardService = new DashboardService(_db);
        }

        public async Task<ActionResult> Index()
        {
            var userName = User != null && User.Identity.IsAuthenticated ? User.Identity.Name : "Guest";
            var userId = User != null && User.Identity.IsAuthenticated ? User.Identity.GetUserId() : null;

            var isAdmin = await _userService.IsUserNameAdminAsync(userName);

            var model = await _dashboardService.GetDashboardDataAsync(userId, userName);
            model.IsAdmin = isAdmin;

            if (!string.IsNullOrEmpty(userId))
            {
                var cartService = new iLgs.Services.PurchaseRequest.ProcurementCartService(_db);
                model.CartItemCount = await cartService.GetCartCountAsync(userId);
            }

            return View("~/Views/Home/Index.cshtml", model);
        }
    }
}
