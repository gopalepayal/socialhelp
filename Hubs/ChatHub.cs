using Microsoft.AspNetCore.SignalR;
using SocialHelpDonation.Models;
using System.Threading.Tasks;

namespace SocialHelpDonation.Hubs
{
    public class ChatHub : Hub
    {
        // Join a group for a specific donation chat
        public async Task JoinDonationGroup(int donationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Donation_{donationId}");
        }

        // Send message to a specific donation group
        public async Task SendMessage(int donationId, string senderId, string senderName, string senderRole, string message)
        {
            await Clients.Group($"Donation_{donationId}").SendAsync("ReceiveMessage", donationId, senderId, senderName, senderRole, message, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        // Notify status updates
        public async Task UpdatePickupStatus(int donationId, string status)
        {
            await Clients.Group($"Donation_{donationId}").SendAsync("StatusUpdated", donationId, status);
        }
        public async Task SendNotification(string userId, string message, string type = "info")
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message, type);
        }

        public async Task BroadcastNotification(string message, string type = "info")
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }
    }
}
