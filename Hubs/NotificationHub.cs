using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRNotificationsDemo.Data;
using SignalRNotificationsDemo.Models;
using SignalRNotificationsDemo.Services;

namespace SignalRNotificationsDemo.Hubs
{
    // Enhanced SignalR Hub with member connection tracking and admin features
    public class NotificationHub : Hub
    {
        private readonly INotificationService _notificationService;
        private readonly ApplicationDbContext _db;

        public NotificationHub(INotificationService notificationService, ApplicationDbContext db)
        {
            _notificationService = notificationService;
            _db = db;
        }

        // Enhanced connection tracking with member management
        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            Console.WriteLine($"[NotificationHub] User connected - UserIdentifier: {userId}, ConnectionId: {Context.ConnectionId}");
            
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId));
                Console.WriteLine($"[NotificationHub] Added user {userId} to group: {GetUserGroup(userId)}");

                // Track member connection
                await TrackMemberConnection(userId, Context.ConnectionId, true);

                // Send undelivered notifications
                var list = await _notificationService.GetUndeliveredNotificationsForUserAsync(userId);
                Console.WriteLine($"[NotificationHub] Found {list.Count()} undelivered notifications for user {userId}");
                
                foreach (var nd in list)
                {
                    var n = nd.Notification!;
                    await Clients.Caller.SendAsync("ReceiveNotification", new {
                        id = n.Id,
                        title = n.Title,
                        message = n.Message,
                        createdAt = n.CreatedAt,
                        metadataJson = n.MetadataJson
                    });

                    await _notificationService.MarkDeliveredAsync(n.Id, userId);
                }

                // Notify admins of new member connection
                await Clients.Group("admins").SendAsync("MemberConnected", new {
                    playerId = userId,
                    connectionTime = DateTime.UtcNow,
                    connectionId = Context.ConnectionId
                });
            }
            else
            {
                Console.WriteLine("[NotificationHub] Warning: User connected without UserIdentifier!");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            if (!string.IsNullOrEmpty(userId))
            {
                // Update member offline status
                await TrackMemberConnection(userId, Context.ConnectionId, false);

                // Notify admins of member disconnection
                await Clients.Group("admins").SendAsync("MemberDisconnected", new {
                    playerId = userId,
                    disconnectionTime = DateTime.UtcNow
                });
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Client calls this when it wants to mark notification as seen
        public async Task AckNotification(string notificationId)
        {
            var userId = Context.UserIdentifier;
            if (string.IsNullOrEmpty(userId)) return;
            if (!Guid.TryParse(notificationId, out var nid)) return;
            
            await _notificationService.MarkSeenAsync(nid, userId);
            
            // Update last activity
            await UpdateMemberActivity(userId);
        }

        // Admin methods
        public async Task JoinAdminGroup()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
        }

        public async Task LeaveAdminGroup()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "admins");
        }

        // Member activity tracking
        private async Task UpdateMemberActivity(string playerId)
        {
            var member = await _db.Members.FindAsync(playerId);
            if (member != null)
            {
                member.LastActivity = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }
        }

        // Member connection tracking
        private async Task TrackMemberConnection(string playerId, string connectionId, bool isConnecting)
        {
            var member = await _db.Members.FindAsync(playerId);
            
            if (member == null && isConnecting)
            {
                // Create new member record
                member = new Member
                {
                    PlayerId = playerId,
                    DisplayName = playerId, // Default to playerId, can be updated later
                    FirstConnected = DateTime.UtcNow,
                    LastConnected = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow,
                    IsOnline = true,
                    ConnectionId = connectionId
                };
                _db.Members.Add(member);
            }
            else if (member != null)
            {
                member.LastConnected = DateTime.UtcNow;
                member.LastActivity = DateTime.UtcNow;
                member.IsOnline = isConnecting;
                member.ConnectionId = isConnecting ? connectionId : string.Empty;
            }

            await _db.SaveChangesAsync();
        }

        private string GetUserGroup(string userId) => $"user_{userId}";
    }
}
