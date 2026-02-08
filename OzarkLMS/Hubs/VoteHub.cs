using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace OzarkLMS.Hubs
{
    public class VoteHub : Hub
    {
        public async Task SendVoteUpdate(int postId, int newScore)
        {
            await Clients.All.SendAsync("ReceiveVoteUpdate", postId, newScore);
        }
    }
}
