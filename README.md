# SignalR Notifications Demo

A comprehensive real-time notification system built with ASP.NET Core and SignalR, featuring separate admin and member portals with enhanced member management capabilities.

## 🚀 Features

### Core Functionality
- **Real-time Notifications**: Instant message delivery using SignalR WebSockets
- **Dual Portal System**: Separate interfaces for administrators and members
- **Member Management**: Track online/offline status, connection history, and activity
- **Targeted Messaging**: Send notifications to specific members or broadcast to all
- **Multi-select Interface**: Modern dropdown for selecting multiple recipients

### Enhanced Features
- **Notification Templates**: Reusable message templates with categories
- **Member Groups**: Organize members into logical groups for targeted messaging
- **Connection Tracking**: Real-time monitoring of member connections and activity
- **Admin Authentication**: Secure admin portal with role-based access
- **Database Flexibility**: Support for both SQL Server and SQLite

### Technical Features
- **Cross-Database Support**: Works with SQL Server (production) and SQLite (development)
- **Docker Ready**: Complete containerization with docker-compose
- **Modern UI**: Responsive design with enhanced user experience
- **API-First Design**: RESTful endpoints for all operations
- **Auto-Initialization**: Automatic database setup and seeding

## 🏃‍♂️ Quick Start

### Option 1: Docker (Recommended)
```bash
git clone https://github.com/ashutosh-jha-11/signalR-dotnet-implementation.git
cd signalR-dotnet-implementation
docker-compose up --build
```

### Option 2: Local Development
```bash
git clone https://github.com/ashutosh-jha-11/signalR-dotnet-implementation.git
cd signalR-dotnet-implementation
dotnet run --urls "http://localhost:5001"
```

## 🌐 Access Points

- **Member Portal**: `http://localhost:5001/` - For members to receive notifications
- **Admin Portal**: `http://localhost:5001/admin.html` - For administrators to manage and send notifications
- **API Documentation**: See `INTEGRATION.md` for complete API reference

## 🔐 Default Credentials

- **Admin Login**: `admin` / `admin123`
- **Admin API Key**: `admin123` (use in `X-ADMIN-KEY` header)

## 📱 Usage

### For Members
1. Open the Member Portal
2. Enter your Player ID and display name
3. Connect to start receiving notifications
4. View real-time notifications and activity stats

### For Administrators
1. Open the Admin Portal and login
2. View dashboard with member statistics
3. Select members from the dropdown
4. Send targeted notifications or broadcasts
5. Manage member groups and notification templates

## 🛠 Technology Stack

- **Backend**: ASP.NET Core 8.0, SignalR, Entity Framework Core
- **Frontend**: Vanilla JavaScript, Modern CSS, Responsive Design
- **Database**: SQL Server (production), SQLite (development)
- **Authentication**: BCrypt password hashing, API key authentication
- **Containerization**: Docker, Docker Compose

## 📊 Database Schema

The system uses the following main entities:
- **Members**: Player information and connection status
- **Admins**: Administrator accounts with role-based access
- **Groups**: Member organization and categorization
- **NotificationTemplates**: Reusable message templates
- **Notifications**: Message history and delivery tracking

## 🔧 Configuration

### Environment Variables
- `DefaultConnection`: Database connection string
- `DOTNET_RUNNING_IN_CONTAINER`: Auto-detected for Docker environments

### Database Options
- **SQL Server**: Full production database with migrations
- **SQLite**: Lightweight development database with auto-creation

## 📚 Documentation

- **Integration Guide**: `INTEGRATION.md` - How to integrate with existing applications
- **API Examples**: `EXAMPLES.md` - Complete API usage examples
- **Testing**: Multiple test pages included for validation

## 🚀 Deployment

### Docker Production
```bash
docker-compose -f docker-compose.yml up -d
```

### Manual Deployment
1. Configure connection string in `appsettings.json`
2. Run `dotnet publish -c Release`
3. Deploy to your preferred hosting platform

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## 📄 License

This project is open source and available under the MIT License.

## 🆘 Support

For issues, questions, or contributions, please visit the [GitHub repository](https://github.com/ashutosh-jha-11/signalR-dotnet-implementation).

---

**Built with ❤️ using ASP.NET Core and SignalR**