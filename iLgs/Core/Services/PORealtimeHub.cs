using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Hubs;
using System.Threading.Tasks;

namespace iLgs.Core.Services
{
    [HubName("poRealtimeHub")]
    public class PORealtimeHub : Hub
    {
        public async Task BroadcastPOUpdate(string poNumber, string status, string author)
        {
            await Clients.Others.broadcastPOStatusChange(new
            {
                PONumber = poNumber,
                Status = status,
                Author = author,
                Timestamp = System.DateTime.Now.ToString("HH:mm:ss")
            });
        }

        public async Task BroadcastPRAllocationSync(string prNumber, string poGroup)
        {
            await Clients.All.onPRAllocationSynced(new
            {
                PRNumber = prNumber,
                POGroup = poGroup
            });
        }

        public override Task OnConnected()
        {
            Groups.Add(Context.ConnectionId, "ProcurementOperators");
            return base.OnConnected();
        }
    }
}