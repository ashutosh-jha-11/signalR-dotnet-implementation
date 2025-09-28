using SignalRNotificationsDemo.Models;

namespace SignalRNotificationsDemo.Services
{
    public interface IAdminService
    {
        // Admin authentication
        Task<Admin?> AuthenticateAsync(string username, string password);
        Task<Admin?> GetAdminByUsernameAsync(string username);
        
        // Member management
        Task<List<Member>> GetAllMembersAsync();
        Task<List<Member>> GetOnlineMembersAsync();
        Task<Member?> GetMemberAsync(string playerId);
        Task UpdateMemberAsync(Member member);
        
        // Group management
        Task<List<Group>> GetAllGroupsAsync();
        Task<Group?> GetGroupAsync(Guid groupId);
        Task<Group> CreateGroupAsync(string name, string description, string color);
        Task UpdateGroupAsync(Group group);
        Task DeleteGroupAsync(Guid groupId);
        Task<List<Member>> GetGroupMembersAsync(Guid groupId);
        Task AddMemberToGroupAsync(string playerId, Guid groupId);
        Task RemoveMemberFromGroupAsync(string playerId, Guid groupId);
        
        // Template management
        Task<List<NotificationTemplate>> GetAllTemplatesAsync();
        Task<NotificationTemplate?> GetTemplateAsync(Guid templateId);
        Task<NotificationTemplate> CreateTemplateAsync(NotificationTemplate template);
        Task UpdateTemplateAsync(NotificationTemplate template);
        Task DeleteTemplateAsync(Guid templateId);
        Task IncrementTemplateUsageAsync(Guid templateId);
        
        // Notification history and analytics
        Task<List<Notification>> GetNotificationHistoryAsync(int days = 7);
        Task<Dictionary<string, int>> GetNotificationStatsAsync();
        Task<List<NotificationDelivery>> GetDeliveryStatsAsync(Guid notificationId);
    }
}
