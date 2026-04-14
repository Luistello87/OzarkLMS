using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OzarkLMS.Hubs
{
    public class ChatHub : Hub
    {
        public async Task JoinChat(string chatId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        }

        public async Task LeaveChat(string chatId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        }

        public async Task SendTyping(string chatId, string username)
        {
            await Clients.OthersInGroup(chatId).SendAsync("UserTyping", username);
        }
    }
}
