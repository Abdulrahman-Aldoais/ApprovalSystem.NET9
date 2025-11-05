using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ApprovalSystem.API.Hubs
{
    [Authorize]
    public class DashboardHub : Hub
    {
        public async Task SubscribeToTenantUpdates(string tenantId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"dashboard_{tenantId}");
        }

        public async Task UnsubscribeFromTenantUpdates(string tenantId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"dashboard_{tenantId}");
        }
    }
}
