using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRNotificationsDemo.Data;
using SignalRNotificationsDemo.Hubs;
using SignalRNotificationsDemo.Models;

namespace SignalRNotificationsDemo.Services
{
    // Concrete implementation of the notification logic: broadcasting and per-user delivery tracking.
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly IHubContext<NotificationHub> _hub;

        public NotificationService(ApplicationDbContext db, IHubContext<NotificationHub> hub)
        {
            _db = db;
            _hub = hub;
        }

        // Save notification and broadcast to connected clients.
        public async Task PublishBroadcastAsync(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("ReceiveNotification", new {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                createdAt = notification.CreatedAt,
                metadataJson = notification.MetadataJson
            });
        }

        // Send notification to specific player ID
        public async Task SendToPlayerAsync(string playerId, string title, string message, string? metadataJson = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                MetadataJson = metadataJson,
                CreatedAt = DateTime.UtcNow,
                IsBroadcast = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            Console.WriteLine($"[NotificationService] Sending notification to player: {playerId}");
            Console.WriteLine($"[NotificationService] Notification ID: {notification.Id}");

            // Send to specific player using SignalR user groups
            await _hub.Clients.User(playerId).SendAsync("ReceiveNotification", new {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                createdAt = notification.CreatedAt,
                metadataJson = notification.MetadataJson
            });

            Console.WriteLine($"[NotificationService] Notification sent via SignalR to user: {playerId}");

            // Create delivery record
            await MarkDeliveredAsync(notification.Id, playerId);
        }

        // Send notification to multiple player IDs
        public async Task SendToPlayersAsync(List<string> playerIds, string title, string message, string? metadataJson = null)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                MetadataJson = metadataJson,
                CreatedAt = DateTime.UtcNow,
                IsBroadcast = false
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();

            var notificationData = new {
                id = notification.Id,
                title = notification.Title,
                message = notification.Message,
                createdAt = notification.CreatedAt,
                metadataJson = notification.MetadataJson
            };

            // Send to multiple players
            foreach (var playerId in playerIds)
            {
                await _hub.Clients.User(playerId).SendAsync("ReceiveNotification", notificationData);
                await MarkDeliveredAsync(notification.Id, playerId);
            }
        }

        // Check if player is currently connected
        public async Task<bool> IsPlayerConnectedAsync(string playerId)
        {
            // This is a simple check - in production you might want to maintain
            // a more sophisticated connection tracking system
            try
            {
                await _hub.Clients.User(playerId).SendAsync("ping");
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Returns notifications created today (UTC) that haven't been delivered to this user.
        public async Task<IEnumerable<NotificationDelivery>> GetUndeliveredNotificationsForUserAsync(string userId)
        {
            var todayUtc = DateTime.UtcNow.Date;
            var notifications = await _db.Notifications
                .Where(n => (n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow) && n.CreatedAt.Date == todayUtc)
                .OrderBy(n => n.CreatedAt)
                .ToListAsync();

            var existing = await _db.NotificationDeliveries
                .Where(nd => nd.UserId == userId)
                .Select(nd => nd.NotificationId)
                .ToListAsync();

            var pending = notifications
                .Where(n => !existing.Contains(n.Id))
                .Select(n => new NotificationDelivery { Notification = n, NotificationId = n.Id, UserId = userId })
                .ToList();

            return pending;
        }

        // Create delivery record if not exists or update DeliveredAt.
        public async Task MarkDeliveredAsync(Guid notificationId, string userId)
        {
            var existing = await _db.NotificationDeliveries
                .FirstOrDefaultAsync(nd => nd.NotificationId == notificationId && nd.UserId == userId);

            if (existing == null)
            {
                _db.NotificationDeliveries.Add(new NotificationDelivery
                {
                    NotificationId = notificationId,
                    UserId = userId,
                    DeliveredAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            else if (existing.DeliveredAt == null)
            {
                existing.DeliveredAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        // Mark seen timestamp for analytics / UI.
        public async Task MarkSeenAsync(Guid notificationId, string userId)
        {
            var existing = await _db.NotificationDeliveries
                .FirstOrDefaultAsync(nd => nd.NotificationId == notificationId && nd.UserId == userId);

            if (existing == null)
            {
                _db.NotificationDeliveries.Add(new NotificationDelivery
                {
                    NotificationId = notificationId,
                    UserId = userId,
                    DeliveredAt = DateTime.UtcNow,
                    SeenAt = DateTime.UtcNow
                });
                await _db.SaveChangesAsync();
            }
            else
            {
                existing.SeenAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
    }
}
