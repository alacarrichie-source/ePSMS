using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;

namespace iLgs.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static bool IsActive(this ClaimsPrincipal user)
        {
            var activeClaim = user.FindFirst("Active")?.Value;
            return bool.TryParse(activeClaim, out var isActive) && isActive;
        }
    }

}