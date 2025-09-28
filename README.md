# SignalR Notifications — Minimal Demo

Overview:
- ASP.NET Core app with SignalR for realtime notifications.
- SQL Server Docker-compose ready; app auto-applies EF migrations and seeds mock notifications.
- UI: responsive, accessible toasts + admin demo panel.
- Demo admin header: `X-ADMIN-KEY: secret-admin-key`. Default test user: `testUser`.

Run (requires Docker):
1. `docker-compose up --build`
2. Open `http://localhost:5000` and click Login (user prefilled as `testUser`).

Notes:
- This is a demo. Replace header-based admin auth and querystring user-id with proper Identity in production.
- Files include inline comments to help developers.
