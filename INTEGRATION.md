# Integration Guide

Practical examples for integrating SignalR Notifications into your existing application.

## HTTP Client Integration

```csharp
// Add this service to your existing application
public class NotificationClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _adminKey;

    public NotificationClient(HttpClient httpClient, string baseUrl, string adminKey)
    {
        _httpClient = httpClient;
        _baseUrl = baseUrl;
        _adminKey = adminKey;
        _httpClient.DefaultRequestHeaders.Add("X-ADMIN-KEY", adminKey);
    }

    public async Task SendToPlayerAsync(string playerId, string title, string message, object? metadata = null)
    {
        var payload = new
        {
            playerId,
            title,
            message,
            metadataJson = metadata != null ? JsonSerializer.Serialize(metadata) : null
        };

        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/notifications/send-to-player",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        response.EnsureSuccessStatusCode();
    }

    public async Task SendToMultiplePlayersAsync(List<string> playerIds, string title, string message)
    {
        var payload = new { playerIds, title, message };
        
        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/notifications/send-to-players",
            new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        );

        response.EnsureSuccessStatusCode();
    }

    public async Task<bool> IsPlayerConnectedAsync(string playerId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/notifications/player/{playerId}/status");
        
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<JsonElement>(content);
            return result.GetProperty("isConnected").GetBoolean();
        }
        
        return false;
    }
}
```

## Dependency Injection Setup

```csharp
services.AddHttpClient<NotificationClient>();
services.AddSingleton<NotificationClient>(provider =>
{
    var httpClient = provider.GetRequiredService<HttpClient>();
    return new NotificationClient(httpClient, "http://localhost:5000", "secret-admin-key");
});
```

## Usage Example

```csharp
[ApiController]
[Route("api/[controller]")]
public class GameController : ControllerBase
{
    private readonly NotificationClient _notificationClient;

    public GameController(NotificationClient notificationClient)
    {
        _notificationClient = notificationClient;
    }

    [HttpPost("complete-level")]
    public async Task<IActionResult> CompleteLevel([FromBody] CompleteLevelRequest request)
    {
        // Your existing game logic...
        var player = GetPlayer(request.PlayerId);
        player.CompleteLevel(request.Level);

        // Send notification
        if (await _notificationClient.IsPlayerConnectedAsync(request.PlayerId))
        {
            await _notificationClient.SendToPlayerAsync(
                request.PlayerId,
                "Level Complete!",
                $"Congratulations! You completed level {request.Level}",
                new { level = request.Level, xpGained = 100 }
            );
        }

        return Ok();
    }

    [HttpPost("tournament-start")]
    public async Task<IActionResult> StartTournament([FromBody] StartTournamentRequest request)
    {
        // Your existing tournament logic...
        var tournament = CreateTournament(request);

        // Notify all participants
        await _notificationClient.SendToMultiplePlayersAsync(
            request.ParticipantIds,
            "Tournament Started!",
            $"Tournament '{tournament.Name}' has begun. Good luck!"
        );

        return Ok();
    }
}
```

## JavaScript Client

```typescript
class NotificationManager {
    private connection: signalR.HubConnection | null = null;
    private playerId: string;

    constructor(playerId: string) {
        this.playerId = playerId;
    }

    async connect(): Promise<void> {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl(`/hubs/notifications?user=${encodeURIComponent(this.playerId)}`)
            .withAutomaticReconnect()
            .build();

        this.connection.on('ReceiveNotification', (notification: any) => {
            this.handleNotification(notification);
        });

        await this.connection.start();
        console.log('Connected to notification hub');
    }

    private handleNotification(notification: any): void {
        // Show notification to user
        this.showNotification(notification.title, notification.message);

        // Handle metadata if present
        if (notification.metadataJson) {
            const metadata = JSON.parse(notification.metadataJson);
            this.handleMetadata(metadata);
        }

        // Acknowledge the notification
        this.connection?.invoke('AckNotification', notification.id);
    }

    private showNotification(title: string, message: string): void {
        // Your notification display logic here
        // Could be a toast, modal, or custom UI element
        console.log(`Notification: ${title} - ${message}`);
    }

    private handleMetadata(metadata: any): void {
        // Handle game-specific metadata
        if (metadata.level) {
            // Update level display
        }
        if (metadata.xpGained) {
            // Show XP animation
        }
    }

    disconnect(): void {
        this.connection?.stop();
    }
}

// Usage
const notificationManager = new NotificationManager('player123');
await notificationManager.connect();
```

## Common Use Cases
```csharp
// Achievement unlocked
await _notificationClient.SendToPlayerAsync(
    playerId, 
    "Achievement Unlocked!", 
    "You've earned the 'Dragon Slayer' achievement",
    new { achievementId = "dragon_slayer", points = 500 }
);

// Level up
await _notificationClient.SendToPlayerAsync(
    playerId,
    "Level Up!",
    $"Congratulations! You're now level {newLevel}",
    new { oldLevel = oldLevel, newLevel = newLevel, skillPoints = 3 }
);
```

// Friend request
await _notificationClient.SendToPlayerAsync(
    playerId, "Friend Request", $"{senderName} wants to be your friend");

// System maintenance
await _notificationClient.SendToMultiplePlayersAsync(
    onlinePlayerIds, "Maintenance", "Server maintenance in 15 minutes");

// Purchase confirmation  
await _notificationClient.SendToPlayerAsync(
    playerId, "Purchase Successful", "Premium upgrade activated");
```

## Error Handling

```csharp
public async Task SafelySendNotification(string playerId, string title, string message)
{
    try
    {
        await _notificationClient.SendToPlayerAsync(playerId, title, message);
    }
    catch (HttpRequestException ex)
    {
        // Log the error and continue - don't block your main application flow
        _logger.LogWarning("Failed to send notification to player {PlayerId}: {Error}", playerId, ex.Message);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error sending notification to player {PlayerId}", playerId);
    }
}
```

## Best Practices

- Use `SendToMultiplePlayersAsync` for multiple recipients
- Check `IsPlayerConnectedAsync` before sending to avoid offline players
- Always use async methods to avoid blocking
- Handle errors gracefully - don't break main application flow
- Keep metadata JSON small and focused

The notification system runs independently and won't impact your core functionality.
