using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iLgs.Utilities
{
    public class NotificationHub : Hub
    {
        public NotificationHub()
        {

        }

        public void RefreshNotification()
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            hubContext.Clients.All.refreshNotification();
        }

        public void RefreshUserNotification(string userId)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();

            hubContext.Clients.User(userId).refreshNotification();
        }

        public void CancellProcess(string message)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            hubContext.Clients.All.cancellProcess(message);
        }

        public void Announce(string msg)
        {
            Clients.Caller.Announce(msg);
        }
    }        
}