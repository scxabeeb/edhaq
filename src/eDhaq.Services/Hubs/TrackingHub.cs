using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace eDhaq.Services.Hubs;

[Authorize]
public class TrackingHub : Hub
{
    public async Task JoinOrderGroup(string orderNumber)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderNumber}");
    }

    public async Task LeaveOrderGroup(string orderNumber)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order-{orderNumber}");
    }
}
