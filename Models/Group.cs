using System.ComponentModel.DataAnnotations;

namespace SignalRNotificationsDemo.Models
{
    public class Group
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "#4f46e5";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public virtual ICollection<MemberGroup> MemberGroups { get; set; } = new List<MemberGroup>();
    }

    public class MemberGroup
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        public string PlayerId { get; set; } = string.Empty;
        public Guid GroupId { get; set; }
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        public virtual Member Member { get; set; } = null!;
        public virtual Group Group { get; set; } = null!;
    }
}
