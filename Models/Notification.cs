using System;
using System.ComponentModel.DataAnnotations;

namespace SignalRNotificationsDemo.Models
{
    // Notification entity: represents a message an admin can broadcast.
    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        // short title of notification
        public string Title { get; set; } = string.Empty;

        // body content
        public string Message { get; set; } = string.Empty;

        // optional metadata (JSON string)
        public string? MetadataJson { get; set; }

        // creation time in UTC
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // optional expiry
        public DateTime? ExpiresAt { get; set; }

        // broadcast vs targeted (not used in this demo, reserved for extensions)
        public bool IsBroadcast { get; set; } = true;
    }
}
