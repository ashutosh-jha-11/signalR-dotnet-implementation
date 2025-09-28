using SignalRNotificationsDemo.Models;

namespace SignalRNotificationsDemo.Services
{
    // Service interface for notification business logic.
    public interface INotificationService
    {
        // Broadcast to all connected users
        Task PublishBroadcastAsync(Notification notification);
        
        // Send notification to specific player ID
        Task SendToPlayerAsync(string playerId, string title, string message, string? metadataJson = null);
        
        // Send notification to multiple player IDs
        Task SendToPlayersAsync(List<string> playerIds, string title, string message, string? metadataJson = null);
        
        // Get undelivered notifications for a user
        Task<IEnumerable<NotificationDelivery>> GetUndeliveredNotificationsForUserAsync(string userId);
        
        // Mark notification as delivered
        Task MarkDeliveredAsync(Guid notificationId, string userId);
        
        // Mark notification as seen
        Task MarkSeenAsync(Guid notificationId, string userId);
        
        // Check if player is currently connected
        Task<bool> IsPlayerConnectedAsync(string playerId);
    }
}
