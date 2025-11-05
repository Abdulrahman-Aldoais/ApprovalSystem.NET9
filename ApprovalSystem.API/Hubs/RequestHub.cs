using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ApprovalSystem.API.Hubs
{
    [Authorize]
    public class RequestHub : Hub
    {
        public async Task JoinRequestGroup(string requestId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"request_{requestId}");
        }

        public async Task LeaveRequestGroup(string requestId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request_{requestId}");
        }
    }
}
