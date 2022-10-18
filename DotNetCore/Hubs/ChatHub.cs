using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace DotNetCore.Hubs
{
    public class ChatHub : Hub
    {
        // Call the broadcastMessage method to update clients.
        public async Task Send(string name, string message)
        {
            await Clients.All.SendAsync("broadcastMessage", name, message);
        }

        public override async Task OnConnectedAsync()
        {
            await Clients.All.SendAsync("ReceiveSystemMessage", $"{Context.User.Identity.Name} joined.");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            await Clients.All.SendAsync("ReceiveSystemMessage", $"{Context.User.Identity.Name} left.");
            await base.OnDisconnectedAsync(exception);
        }
    }

    public class NameUserIdProvider : IUserIdProvider
    {
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Identity?.Name;
        }
    }
}