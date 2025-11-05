using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ApprovalSystem.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            var tenantId = Context.User?.FindFirst("tenant_id")?.Value;
            if (!string.IsNullOrEmpty(tenantId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant_{tenantId}");
            }
            await base.OnConnectedAsync();
        }
    }
}
