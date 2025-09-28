using Microsoft.AspNetCore.SignalR;

namespace SignalRNotificationsDemo.Helpers
{
    // Demo-only: This extracts a user id from the HTTP request query string or header.
    // In production, replace this with a provider that uses the authenticated ClaimsPrincipal.
    public class QueryUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            var http = connection.GetHttpContext();
            if (http == null) return null;

            var q = http.Request.Query["user"].FirstOrDefault();
            if (!string.IsNullOrEmpty(q)) return q;

            var h = http.Request.Headers["X-User-Id"].FirstOrDefault();
            return string.IsNullOrEmpty(h) ? null : h;
        }
    }
}
