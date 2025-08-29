using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Utilities
{
    public static class WebHelper
    {
        public static string GetClientIpAddress()
        {
            var request = HttpContext.Current?.Request;
            if (request == null) return "Unknown";

            // Check for proxy (X-Forwarded-For)
            var forwardedFor = request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0]; // first IP in the chain
            }

            return request.ServerVariables["REMOTE_ADDR"] ?? "Unknown";
        }

        public static string GetUserName()
        {
            var user = HttpContext.Current?.User;
            if (user == null || user.Identity == null || !user.Identity.IsAuthenticated)
                return "Anonymous";

            return user.Identity.Name;  // e.g. DOMAIN\UserName or configured Name
        }
    }
}