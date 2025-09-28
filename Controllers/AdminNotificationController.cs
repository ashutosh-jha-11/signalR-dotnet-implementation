using Microsoft.AspNetCore.Mvc;
using SignalRNotificationsDemo.Models;
using SignalRNotificationsDemo.Services;

namespace SignalRNotificationsDemo.Controllers
{
    // Player-focused notification API. For demo we use a header-based key 'X-ADMIN-KEY'.
    // Replace this with proper role-based authorization in production.
    [ApiController]
    [Route("api/notifications")]
    public class AdminNotificationController : ControllerBase
    {
        private readonly INotificationService _service;
        private const string HeaderKey = "X-ADMIN-KEY";
        private const string DemoAdminKey = "secret-admin-key"; // demo only

        public AdminNotificationController(INotificationService service) => _service = service;

        /// <summary>
        /// Send notification to a specific player by their player ID
        /// </summary>
        [HttpPost("send-to-player")]
        public async Task<IActionResult> SendToPlayer([FromBody] PlayerNotificationDto dto)
        {
            if (!Request.Headers.TryGetValue(HeaderKey, out var key) || key != DemoAdminKey)
                return Unauthorized();

            if (string.IsNullOrEmpty(dto.PlayerId))
                return BadRequest("PlayerId is required");

            await _service.SendToPlayerAsync(dto.PlayerId, dto.Title, dto.Message, dto.MetadataJson);
            return Ok(new { success = true, message = $"Notification sent to player {dto.PlayerId}" });
        }

        /// <summary>
        /// Send notification to multiple players by their player IDs
        /// </summary>
        [HttpPost("send-to-players")]
        public async Task<IActionResult> SendToPlayers([FromBody] MultiPlayerNotificationDto dto)
        {
            if (!Request.Headers.TryGetValue(HeaderKey, out var key) || key != DemoAdminKey)
                return Unauthorized();

            if (dto.PlayerIds == null || !dto.PlayerIds.Any())
                return BadRequest("PlayerIds are required");

            await _service.SendToPlayersAsync(dto.PlayerIds, dto.Title, dto.Message, dto.MetadataJson);
            return Ok(new { success = true, message = $"Notification sent to {dto.PlayerIds.Count} players" });
        }

        /// <summary>
        /// Check if a player is currently connected
        /// </summary>
        [HttpGet("player/{playerId}/status")]
        public async Task<IActionResult> GetPlayerStatus(string playerId)
        {
            if (!Request.Headers.TryGetValue(HeaderKey, out var key) || key != DemoAdminKey)
                return Unauthorized();

            var isConnected = await _service.IsPlayerConnectedAsync(playerId);
            return Ok(new { playerId, isConnected, status = isConnected ? "online" : "offline" });
        }

        /// <summary>
        /// Broadcast notification to all connected players
        /// </summary>
        [HttpPost("broadcast")]
        public async Task<IActionResult> Broadcast([FromBody] BroadcastDto dto)
        {
            if (!Request.Headers.TryGetValue(HeaderKey, out var key) || key != DemoAdminKey)
                return Unauthorized();

            var n = new Notification
            {
                Title = dto.Title,
                Message = dto.Message,
                MetadataJson = dto.MetadataJson,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = dto.ExpiresAt,
                IsBroadcast = true
            };

            await _service.PublishBroadcastAsync(n);
            return Ok(new { id = n.Id, success = true });
        }
    }

    public class PlayerNotificationDto
    {
        public string PlayerId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
    }

    public class MultiPlayerNotificationDto
    {
        public List<string> PlayerIds { get; set; } = new();
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
    }

    public class BroadcastDto
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? MetadataJson { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
