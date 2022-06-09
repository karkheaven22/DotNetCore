using Microsoft.AspNetCore.SignalR;
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
    }
}
