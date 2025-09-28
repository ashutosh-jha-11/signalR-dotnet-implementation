using Microsoft.AspNetCore.Mvc;
using SignalRNotificationsDemo.Models;
using SignalRNotificationsDemo.Services;

namespace SignalRNotificationsDemo.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly INotificationService _notificationService;

        public AdminController(IAdminService adminService, INotificationService notificationService)
        {
            _adminService = adminService;
            _notificationService = notificationService;
        }

        // Authentication
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AdminLoginDto dto)
        {
            var admin = await _adminService.AuthenticateAsync(dto.Username, dto.Password);
            if (admin == null)
                return Unauthorized(new { message = "Invalid username or password" });

            return Ok(new { 
                success = true, 
                admin = new { 
                    id = admin.Id, 
                    username = admin.Username, 
                    displayName = admin.DisplayName,
                    email = admin.Email,
                    role = admin.Role
                }
            });
        }

        // Dashboard stats
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var stats = await _adminService.GetNotificationStatsAsync();
            return Ok(stats);
        }

        // Member management
        [HttpGet("members")]
        public async Task<IActionResult> GetAllMembers()
        {
            var members = await _adminService.GetAllMembersAsync();
            return Ok(members.Select(m => new {
                playerId = m.PlayerId,
                displayName = m.DisplayName,
                isOnline = m.IsOnline,
                firstConnected = m.FirstConnected,
                lastConnected = m.LastConnected,
                lastActivity = m.LastActivity,
                avatar = m.Avatar,
                groups = m.MemberGroups.Select(mg => new {
                    id = mg.Group.Id,
                    name = mg.Group.Name,
                    color = mg.Group.Color
                }).ToList()
            }));
        }

        [HttpGet("members/online")]
        public async Task<IActionResult> GetOnlineMembers()
        {
            var members = await _adminService.GetOnlineMembersAsync();
            return Ok(members.Select(m => new {
                playerId = m.PlayerId,
                displayName = m.DisplayName,
                lastActivity = m.LastActivity,
                groups = m.MemberGroups.Select(mg => new {
                    id = mg.Group.Id,
                    name = mg.Group.Name,
                    color = mg.Group.Color
                }).ToList()
            }));
        }

        [HttpGet("members/{playerId}")]
        public async Task<IActionResult> GetMember(string playerId)
        {
            var member = await _adminService.GetMemberAsync(playerId);
            if (member == null)
                return NotFound();

            return Ok(new {
                playerId = member.PlayerId,
                displayName = member.DisplayName,
                isOnline = member.IsOnline,
                firstConnected = member.FirstConnected,
                lastConnected = member.LastConnected,
                lastActivity = member.LastActivity,
                avatar = member.Avatar,
                email = member.Email,
                groups = member.MemberGroups.Select(mg => new {
                    id = mg.Group.Id,
                    name = mg.Group.Name,
                    color = mg.Group.Color,
                    joinedAt = mg.JoinedAt
                }).ToList()
            });
        }

        // Group management
        [HttpGet("groups")]
        public async Task<IActionResult> GetAllGroups()
        {
            var groups = await _adminService.GetAllGroupsAsync();
            return Ok(groups.Select(g => new {
                id = g.Id,
                name = g.Name,
                description = g.Description,
                color = g.Color,
                createdAt = g.CreatedAt,
                memberCount = g.MemberGroups.Count,
                members = g.MemberGroups.Select(mg => new {
                    playerId = mg.Member.PlayerId,
                    displayName = mg.Member.DisplayName,
                    isOnline = mg.Member.IsOnline
                }).ToList()
            }));
        }

        [HttpPost("groups")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            var group = await _adminService.CreateGroupAsync(dto.Name, dto.Description, dto.Color);
            return Ok(new { success = true, groupId = group.Id });
        }

        [HttpPost("groups/{groupId}/members/{playerId}")]
        public async Task<IActionResult> AddMemberToGroup(Guid groupId, string playerId)
        {
            await _adminService.AddMemberToGroupAsync(playerId, groupId);
            return Ok(new { success = true });
        }

        [HttpDelete("groups/{groupId}/members/{playerId}")]
        public async Task<IActionResult> RemoveMemberFromGroup(Guid groupId, string playerId)
        {
            await _adminService.RemoveMemberFromGroupAsync(playerId, groupId);
            return Ok(new { success = true });
        }

        // Template management
        [HttpGet("templates")]
        public async Task<IActionResult> GetAllTemplates()
        {
            var templates = await _adminService.GetAllTemplatesAsync();
            return Ok(templates);
        }

        [HttpPost("templates")]
        public async Task<IActionResult> CreateTemplate([FromBody] NotificationTemplate template)
        {
            template.CreatedBy = "admin"; // In production, get from current user
            var created = await _adminService.CreateTemplateAsync(template);
            return Ok(new { success = true, templateId = created.Id });
        }

        [HttpPut("templates/{templateId}")]
        public async Task<IActionResult> UpdateTemplate(Guid templateId, [FromBody] NotificationTemplate template)
        {
            template.Id = templateId;
            await _adminService.UpdateTemplateAsync(template);
            return Ok(new { success = true });
        }

        [HttpDelete("templates/{templateId}")]
        public async Task<IActionResult> DeleteTemplate(Guid templateId)
        {
            await _adminService.DeleteTemplateAsync(templateId);
            return Ok(new { success = true });
        }

        // Enhanced notification sending
        [HttpPost("notifications/send-to-member")]
        public async Task<IActionResult> SendToMember([FromBody] SendToMemberDto dto)
        {
            await _notificationService.SendToPlayerAsync(dto.PlayerId, dto.Title, dto.Message, dto.MetadataJson);
            return Ok(new { success = true, message = $"Notification sent to member {dto.PlayerId}" });
        }

        [HttpPost("notifications/send-to-group")]
        public async Task<IActionResult> SendToGroup([FromBody] SendToGroupDto dto)
        {
            var members = await _adminService.GetGroupMembersAsync(dto.GroupId);
            var playerIds = members.Select(m => m.PlayerId).ToList();
            
            if (playerIds.Any())
            {
                await _notificationService.SendToPlayersAsync(playerIds, dto.Title, dto.Message, dto.MetadataJson);
            }
            
            return Ok(new { success = true, message = $"Notification sent to {playerIds.Count} group members" });
        }

        [HttpPost("notifications/send-template")]
        public async Task<IActionResult> SendTemplate([FromBody] SendTemplateDto dto)
        {
            var template = await _adminService.GetTemplateAsync(dto.TemplateId);
            if (template == null)
                return NotFound("Template not found");

            if (dto.PlayerIds != null && dto.PlayerIds.Any())
            {
                await _notificationService.SendToPlayersAsync(dto.PlayerIds, template.Title, template.Message, template.MetadataJson);
            }
            else if (dto.GroupId.HasValue)
            {
                var members = await _adminService.GetGroupMembersAsync(dto.GroupId.Value);
                var playerIds = members.Select(m => m.PlayerId).ToList();
                await _notificationService.SendToPlayersAsync(playerIds, template.Title, template.Message, template.MetadataJson);
            }

            await _adminService.IncrementTemplateUsageAsync(dto.TemplateId);
            return Ok(new { success = true });
        }

        // Notification history
        [HttpGet("notifications/history")]
        public async Task<IActionResult> GetNotificationHistory([FromQuery] int days = 7)
        {
            var notifications = await _adminService.GetNotificationHistoryAsync(days);
            return Ok(notifications);
        }

        [HttpGet("notifications/{notificationId}/delivery-stats")]
        public async Task<IActionResult> GetDeliveryStats(Guid notificationId)
        {
            var stats = await _adminService.GetDeliveryStatsAsync(notificationId);
            return Ok(stats);
        }
    }

    // DTOs
    public class AdminLoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CreateGroupDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Color { get; set; } = "#4f46e5";
    }

    public class SendToMemberDto
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
    }

    public class SendToGroupDto
    {
        public Guid GroupId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
    }

    public class SendTemplateDto
    {
        public Guid TemplateId { get; set; }
        public List<string>? PlayerIds { get; set; }
        public Guid? GroupId { get; set; }
    }
}
