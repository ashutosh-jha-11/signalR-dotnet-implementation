using Microsoft.EntityFrameworkCore;
using SignalRNotificationsDemo.Models;

namespace SignalRNotificationsDemo.Data
{
    // EF Core DB context. Enhanced with member management and admin features.
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Notification> Notifications { get; set; } = null!;
        public DbSet<NotificationDelivery> NotificationDeliveries { get; set; } = null!;
        public DbSet<Member> Members { get; set; } = null!;
        public DbSet<Admin> Admins { get; set; } = null!;
        public DbSet<Group> Groups { get; set; } = null!;
        public DbSet<MemberGroup> MemberGroups { get; set; } = null!;
        public DbSet<NotificationTemplate> NotificationTemplates { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Notification relationships
            modelBuilder.Entity<Notification>().HasKey(n => n.Id);
            modelBuilder.Entity<NotificationDelivery>()
                .HasIndex(nd => new { nd.UserId, nd.NotificationId })
                .IsUnique();

            // Member relationships
            modelBuilder.Entity<Member>().HasKey(m => m.PlayerId);
            
            // Group and MemberGroup relationships
            modelBuilder.Entity<Group>().HasKey(g => g.Id);
            modelBuilder.Entity<MemberGroup>().HasKey(mg => mg.Id);
            modelBuilder.Entity<MemberGroup>()
                .HasIndex(mg => new { mg.PlayerId, mg.GroupId })
                .IsUnique();
            
            modelBuilder.Entity<MemberGroup>()
                .HasOne(mg => mg.Member)
                .WithMany(m => m.MemberGroups)
                .HasForeignKey(mg => mg.PlayerId);
                
            modelBuilder.Entity<MemberGroup>()
                .HasOne(mg => mg.Group)
                .WithMany(g => g.MemberGroups)
                .HasForeignKey(mg => mg.GroupId);

            // Admin relationships
            modelBuilder.Entity<Admin>().HasKey(a => a.Id);
            modelBuilder.Entity<Admin>()
                .HasIndex(a => a.Username)
                .IsUnique();

            // Template relationships
            modelBuilder.Entity<NotificationTemplate>().HasKey(nt => nt.Id);
        }
    }

    // Enhanced DB seeder with admin, groups, templates, and sample data
    public static class DbSeed
    {
        public static void Seed(ApplicationDbContext db)
        {
            try
            {
                var now = DateTime.UtcNow;

                // Seed default admin if none exists
                if (!db.Admins.Any())
                {
                db.Admins.Add(new Admin
                {
                    Username = "admin",
                    DisplayName = "System Administrator",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // Simple demo password
                    Email = "admin@demo.com",
                    Role = "SuperAdmin"
                });
            }

            // Seed default groups if none exist
            if (!db.Groups.Any())
            {
                db.Groups.AddRange(
                    new Group
                    {
                        Name = "VIP Players",
                        Description = "Premium members with special privileges",
                        Color = "#gold"
                    },
                    new Group
                    {
                        Name = "New Players",
                        Description = "Recently joined players",
                        Color = "#green"
                    },
                    new Group
                    {
                        Name = "Active Players",
                        Description = "Highly engaged players",
                        Color = "#blue"
                    }
                );
            }

            // Seed notification templates if none exist
            if (!db.NotificationTemplates.Any())
            {
                db.NotificationTemplates.AddRange(
                    new NotificationTemplate
                    {
                        Name = "Welcome Message",
                        Title = "Welcome to the Game!",
                        Message = "Thanks for joining us. Enjoy your gaming experience!",
                        Category = "Onboarding",
                        CreatedBy = "admin"
                    },
                    new NotificationTemplate
                    {
                        Name = "Achievement Unlocked",
                        Title = "Achievement Unlocked!",
                        Message = "Congratulations! You've unlocked a new achievement.",
                        Category = "Achievement",
                        CreatedBy = "admin"
                    },
                    new NotificationTemplate
                    {
                        Name = "Maintenance Notice",
                        Title = "Scheduled Maintenance",
                        Message = "Server maintenance will begin shortly. Please save your progress.",
                        Category = "System",
                        CreatedBy = "admin"
                    }
                );
            }

            // Seed sample notifications if none exist
            if (!db.Notifications.Any())
            {
                db.Notifications.AddRange(
                    new Notification
                    {
                        Title = "System Maintenance",
                        Message = "Server maintenance completed successfully. Welcome back!",
                        CreatedAt = now.AddMinutes(-10),
                        IsBroadcast = true
                    },
                    new Notification
                    {
                        Title = "New Feature Release",
                        Message = "Check out our latest features in the game!",
                        CreatedAt = now.AddHours(-1),
                        IsBroadcast = true
                    }
                );
            }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Database seeding failed: {ex.Message}");
                // Don't throw - let the app continue even if seeding fails
            }
        }
    }
}
