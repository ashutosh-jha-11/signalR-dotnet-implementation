using System.ComponentModel.DataAnnotations;

namespace SignalRNotificationsDemo.Models
{
    public class NotificationTemplate
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
        public string Category { get; set; } = "General";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int UsageCount { get; set; } = 0;
    }
}
