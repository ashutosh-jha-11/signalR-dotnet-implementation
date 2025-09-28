using Microsoft.EntityFrameworkCore;
using SignalRNotificationsDemo.Data;
using SignalRNotificationsDemo.Models;

namespace SignalRNotificationsDemo.Services
{
    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _db;

        public AdminService(ApplicationDbContext db)
        {
            _db = db;
        }

        // Admin authentication
        public async Task<Admin?> AuthenticateAsync(string username, string password)
        {
            var admin = await GetAdminByUsernameAsync(username);
            if (admin == null || !BCrypt.Net.BCrypt.Verify(password, admin.PasswordHash))
                return null;

            admin.LastLogin = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return admin;
        }

        public async Task<Admin?> GetAdminByUsernameAsync(string username)
        {
            return await _db.Admins.FirstOrDefaultAsync(a => a.Username == username && a.IsActive);
        }

        // Member management
        public async Task<List<Member>> GetAllMembersAsync()
        {
            return await _db.Members
                .Include(m => m.MemberGroups)
                .ThenInclude(mg => mg.Group)
                .OrderByDescending(m => m.LastConnected)
                .ToListAsync();
        }

        public async Task<List<Member>> GetOnlineMembersAsync()
        {
            return await _db.Members
                .Where(m => m.IsOnline)
                .Include(m => m.MemberGroups)
                .ThenInclude(mg => mg.Group)
                .OrderBy(m => m.DisplayName)
                .ToListAsync();
        }

        public async Task<Member?> GetMemberAsync(string playerId)
        {
            return await _db.Members
                .Include(m => m.MemberGroups)
                .ThenInclude(mg => mg.Group)
                .FirstOrDefaultAsync(m => m.PlayerId == playerId);
        }

        public async Task UpdateMemberAsync(Member member)
        {
            _db.Members.Update(member);
            await _db.SaveChangesAsync();
        }

        // Group management
        public async Task<List<Group>> GetAllGroupsAsync()
        {
            return await _db.Groups
                .Include(g => g.MemberGroups)
                .ThenInclude(mg => mg.Member)
                .Where(g => g.IsActive)
                .OrderBy(g => g.Name)
                .ToListAsync();
        }

        public async Task<Group?> GetGroupAsync(Guid groupId)
        {
            return await _db.Groups
                .Include(g => g.MemberGroups)
                .ThenInclude(mg => mg.Member)
                .FirstOrDefaultAsync(g => g.Id == groupId && g.IsActive);
        }

        public async Task<Group> CreateGroupAsync(string name, string description, string color)
        {
            var group = new Group
            {
                Name = name,
                Description = description,
                Color = color,
                CreatedBy = "admin" // In production, get from current user context
            };

            _db.Groups.Add(group);
            await _db.SaveChangesAsync();
            return group;
        }

        public async Task UpdateGroupAsync(Group group)
        {
            _db.Groups.Update(group);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteGroupAsync(Guid groupId)
        {
            var group = await _db.Groups.FindAsync(groupId);
            if (group != null)
            {
                group.IsActive = false;
                await _db.SaveChangesAsync();
            }
        }

        public async Task<List<Member>> GetGroupMembersAsync(Guid groupId)
        {
            return await _db.MemberGroups
                .Where(mg => mg.GroupId == groupId)
                .Include(mg => mg.Member)
                .Select(mg => mg.Member)
                .ToListAsync();
        }

        public async Task AddMemberToGroupAsync(string playerId, Guid groupId)
        {
            var existing = await _db.MemberGroups
                .FirstOrDefaultAsync(mg => mg.PlayerId == playerId && mg.GroupId == groupId);

            if (existing == null)
            {
                _db.MemberGroups.Add(new MemberGroup
                {
                    PlayerId = playerId,
                    GroupId = groupId
                });
                await _db.SaveChangesAsync();
            }
        }

        public async Task RemoveMemberFromGroupAsync(string playerId, Guid groupId)
        {
            var memberGroup = await _db.MemberGroups
                .FirstOrDefaultAsync(mg => mg.PlayerId == playerId && mg.GroupId == groupId);

            if (memberGroup != null)
            {
                _db.MemberGroups.Remove(memberGroup);
                await _db.SaveChangesAsync();
            }
        }

        // Template management
        public async Task<List<NotificationTemplate>> GetAllTemplatesAsync()
        {
            return await _db.NotificationTemplates
                .Where(nt => nt.IsActive)
                .OrderBy(nt => nt.Category)
                .ThenBy(nt => nt.Name)
                .ToListAsync();
        }

        public async Task<NotificationTemplate?> GetTemplateAsync(Guid templateId)
        {
            return await _db.NotificationTemplates
                .FirstOrDefaultAsync(nt => nt.Id == templateId && nt.IsActive);
        }

        public async Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template)
        {
            _db.NotificationTemplates.Add(template);
            await _db.SaveChangesAsync();
            return template;
        }

        public async Task UpdateTemplateAsync(NotificationTemplate template)
        {
            _db.NotificationTemplates.Update(template);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteTemplateAsync(Guid templateId)
        {
            var template = await _db.NotificationTemplates.FindAsync(templateId);
            if (template != null)
            {
                template.IsActive = false;
                await _db.SaveChangesAsync();
            }
        }

        public async Task IncrementTemplateUsageAsync(Guid templateId)
        {
            var template = await _db.NotificationTemplates.FindAsync(templateId);
            if (template != null)
            {
                template.UsageCount++;
                await _db.SaveChangesAsync();
            }
        }

        // Notification history and analytics
        public async Task<List<Notification>> GetNotificationHistoryAsync(int days = 7)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            return await _db.Notifications
                .Where(n => n.CreatedAt >= cutoffDate)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<Dictionary<string, int>> GetNotificationStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);

            return new Dictionary<string, int>
            {
                ["TotalNotifications"] = await _db.Notifications.CountAsync(),
                ["TodayNotifications"] = await _db.Notifications.CountAsync(n => n.CreatedAt.Date == today),
                ["WeekNotifications"] = await _db.Notifications.CountAsync(n => n.CreatedAt >= weekAgo),
                ["TotalMembers"] = await _db.Members.CountAsync(),
                ["OnlineMembers"] = await _db.Members.CountAsync(m => m.IsOnline),
                ["TotalGroups"] = await _db.Groups.CountAsync(g => g.IsActive),
                ["TotalTemplates"] = await _db.NotificationTemplates.CountAsync(nt => nt.IsActive)
            };
        }

        public async Task<List<NotificationDelivery>> GetDeliveryStatsAsync(Guid notificationId)
        {
            return await _db.NotificationDeliveries
                .Where(nd => nd.NotificationId == notificationId)
                .Include(nd => nd.Notification)
                .ToListAsync();
        }
    }
}
