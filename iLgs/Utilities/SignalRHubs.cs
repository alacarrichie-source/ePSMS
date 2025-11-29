using iLgs.Models;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

    public class ChatHub : Hub
    {
        public ChatHub()
        {

        }

        public void Send(string name, string message)
        {           
            Clients.All.sendTest($"{name}: {message}");
        }
        
        public void Send(string conversationId, string userName, Chat message)
        {
            var hubContext = GlobalHost.ConnectionManager.GetHubContext<ChatHub>();
            hubContext.Clients.Group(conversationId)
                       .broadcastMessage(userName, message.Text, message.SentAt.Value.ToString("HH:mm"));            
        }

        public Task JoinConversation(string conversationId)
        {
            var connectionId = Context.ConnectionId;
            return Groups.Add(connectionId, conversationId);
        }

        public Task LeaveConversation(string conversationId)
        {
            return Groups.Remove(Context.ConnectionId, conversationId);
        }
    }
}