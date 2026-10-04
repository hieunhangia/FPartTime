using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Service.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    // SignalR Hub to broadcast notifications
}
