using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SignalRNotificationsDemo.Models
{
    // Tracks delivery and seen state per user for notifications.
    public class NotificationDelivery
    {
        [Key]
        public long Id { get; set; }

        public Guid NotificationId { get; set; }

        [ForeignKey("NotificationId")]
        public Notification? Notification { get; set; }

        // For demo the UserId is a string (we read from querystring). Replace with exact user PK in production.
        public string UserId { get; set; } = string.Empty;

        public DateTime? DeliveredAt { get; set; }
        public DateTime? SeenAt { get; set; }
        public bool Dismissed { get; set; } = false;
    }
}
