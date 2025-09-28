using System.ComponentModel.DataAnnotations;

namespace SignalRNotificationsDemo.Models
{
    public class Member
    {
        [Key]
        public string PlayerId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime FirstConnected { get; set; }
        public DateTime LastConnected { get; set; }
        public DateTime? LastActivity { get; set; }
        public bool IsOnline { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public string? Email { get; set; }
        
        // Navigation properties
        public virtual ICollection<MemberGroup> MemberGroups { get; set; } = new List<MemberGroup>();
    }
}
