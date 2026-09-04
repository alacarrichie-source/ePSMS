using iLgs.Models;
using System.Threading.Tasks;

namespace iLgs.Services.Dashboard
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync(string userId, string userName);
    }
}
