# Testing Examples

Practical examples for testing the SignalR Notifications system.

## Setup
1. Start: `docker-compose up --build`
2. Open: `http://localhost:5000`
3. Connect as any Player ID

## Single Player Test
```bash
curl -X POST http://localhost:5000/api/notifications/send-to-player \
  -H "Content-Type: application/json" \
  -H "X-ADMIN-KEY: secret-admin-key" \
  -d '{
    "playerId": "player123",
    "title": "Welcome!",
    "message": "Hello from the notification system!"
  }'
```

## Multiple Players Test
Open 3 browser tabs, connect as different Player IDs:
```bash
curl -X POST http://localhost:5000/api/notifications/send-to-players \
  -H "Content-Type: application/json" \
  -H "X-ADMIN-KEY: secret-admin-key" \
  -d '{
    "playerIds": ["player123", "player456", "player789"],
    "title": "Group Event",
    "message": "All players invited to special event!"
  }'
```

## Broadcast Test
```bash
curl -X POST http://localhost:5000/api/notifications/broadcast \
  -H "Content-Type: application/json" \
  -H "X-ADMIN-KEY: secret-admin-key" \
  -d '{
    "title": "Server Announcement",
    "message": "System maintenance in 10 minutes"
  }'
```

## Player Status Check
```bash
# Check if player123 is connected
curl -H "X-ADMIN-KEY: secret-admin-key" \
  http://localhost:5000/api/notifications/player/player123/status
```

## Game Examples
```bash
# Player unlocks achievement
curl -X POST http://localhost:5000/api/notifications/send-to-player \
  -H "Content-Type: application/json" \
  -H "X-ADMIN-KEY: secret-admin-key" \
  -d '{
    "playerId": "player123",
    "title": "Achievement Unlocked!",
    "message": "Dragon Slayer - Defeat 100 dragons",
    "metadataJson": "{\"achievementId\": \"dragon_slayer\", \"points\": 500, \"xp\": 1000}"
  }'
```

# Level up
curl -X POST http://localhost:5000/api/notifications/send-to-player \
  -H "Content-Type: application/json" \
  -H "X-ADMIN-KEY: secret-admin-key" \
  -d '{"playerId": "player123", "title": "Level Up!", "message": "You are now level 25"}'

# Tournament
curl -X POST http://localhost:5000/api/notifications/send-to-players \
  -H "Content-Type: application/json" \
  -H "X-ADMIN-KEY: secret-admin-key" \
  -d '{"playerIds": ["player123", "player456"], "title": "Tournament!", "message": "Starts in 5 minutes"}'
```

## JavaScript Example

```javascript
async function testNotification() {
    await fetch('http://localhost:5000/api/notifications/send-to-player', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            'X-ADMIN-KEY': 'secret-admin-key'
        },
        body: JSON.stringify({
            playerId: 'player123',
            title: 'Test',
            message: 'Hello from JavaScript'
        })
    });
}
```

## Testing Workflow

1. Start: `docker-compose up --build`
2. Connect players in browser tabs with different Player IDs  
3. Send notifications using curl or web UI
4. Verify real-time delivery and toast notifications
