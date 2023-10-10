using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iLgs.Services.Interfaces
{
    public interface IUserService
    {
        ValueTask<bool> UserInRole(string userId, string role);
        ValueTask<bool> IsAdmin(string userId);
    }
}
