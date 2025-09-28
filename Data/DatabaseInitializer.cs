using Microsoft.EntityFrameworkCore;
using SignalRNotificationsDemo.Models;

namespace SignalRNotificationsDemo.Data
{
    public static class DatabaseInitializer
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();
            
            // Check if new tables exist by checking the database schema directly
            var connection = context.Database.GetDbConnection();
            
            // Only open if not already open
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            var isSqlServer = connection.GetType().Name.Contains("SqlConnection");

            bool tablesExist = false;
            if (isSqlServer)
            {
                command.CommandText = "SELECT COUNT(*) FROM sysobjects WHERE name='Members' AND xtype='U'";
                var result = await command.ExecuteScalarAsync();
                tablesExist = Convert.ToInt32(result) > 0;
            }
            else
            {
                command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Members'";
                var result = await command.ExecuteScalarAsync();
                tablesExist = Convert.ToInt32(result) > 0;
            }

            if (!tablesExist)
            {
                // If Members table doesn't exist, create all new tables
                await CreateMemberManagementTablesAsync(context);
            }
        }

        private static async Task CreateMemberManagementTablesAsync(ApplicationDbContext context)
        {
            var connection = context.Database.GetDbConnection();
            
            // Only open if not already open
            if (connection.State != System.Data.ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();

            // Check if we're using SQL Server or SQLite
            var isSqlServer = connection.GetType().Name.Contains("SqlConnection");

            if (isSqlServer)
            {
                // SQL Server commands
                command.CommandText = @"
                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Admins' AND xtype='U')
                    CREATE TABLE [Admins] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Username] nvarchar(450) NOT NULL,
                        [DisplayName] nvarchar(max) NOT NULL,
                        [PasswordHash] nvarchar(max) NOT NULL,
                        [Email] nvarchar(max) NOT NULL,
                        [Role] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [LastLogin] datetime2 NULL,
                        [IsActive] bit NOT NULL,
                        [Avatar] nvarchar(max) NULL
                    );

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Members' AND xtype='U')
                    CREATE TABLE [Members] (
                        [PlayerId] nvarchar(450) NOT NULL PRIMARY KEY,
                        [DisplayName] nvarchar(max) NOT NULL,
                        [FirstConnected] datetime2 NOT NULL,
                        [LastConnected] datetime2 NOT NULL,
                        [LastActivity] datetime2 NULL,
                        [IsOnline] bit NOT NULL,
                        [ConnectionId] nvarchar(max) NOT NULL,
                        [Avatar] nvarchar(max) NULL,
                        [Email] nvarchar(max) NULL
                    );

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Groups' AND xtype='U')
                    CREATE TABLE [Groups] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Name] nvarchar(max) NOT NULL,
                        [Description] nvarchar(max) NOT NULL,
                        [Color] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL
                    );

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='NotificationTemplates' AND xtype='U')
                    CREATE TABLE [NotificationTemplates] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [Name] nvarchar(max) NOT NULL,
                        [Title] nvarchar(max) NOT NULL,
                        [Message] nvarchar(max) NOT NULL,
                        [MetadataJson] nvarchar(max) NULL,
                        [Category] nvarchar(max) NOT NULL,
                        [CreatedAt] datetime2 NOT NULL,
                        [CreatedBy] nvarchar(max) NOT NULL,
                        [IsActive] bit NOT NULL,
                        [UsageCount] int NOT NULL
                    );

                    IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='MemberGroups' AND xtype='U')
                    CREATE TABLE [MemberGroups] (
                        [Id] uniqueidentifier NOT NULL PRIMARY KEY,
                        [PlayerId] nvarchar(450) NOT NULL,
                        [GroupId] uniqueidentifier NOT NULL,
                        [JoinedAt] datetime2 NOT NULL,
                        FOREIGN KEY ([PlayerId]) REFERENCES [Members]([PlayerId]) ON DELETE CASCADE,
                        FOREIGN KEY ([GroupId]) REFERENCES [Groups]([Id]) ON DELETE CASCADE
                    );

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_Admins_Username')
                    CREATE UNIQUE INDEX [IX_Admins_Username] ON [Admins] ([Username]);

                    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name='IX_MemberGroups_PlayerId_GroupId')
                    CREATE UNIQUE INDEX [IX_MemberGroups_PlayerId_GroupId] ON [MemberGroups] ([PlayerId], [GroupId]);
                ";
            }
            else
            {
                // SQLite commands
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS [Admins] (
                        [Id] TEXT NOT NULL PRIMARY KEY,
                        [Username] TEXT NOT NULL,
                        [DisplayName] TEXT NOT NULL,
                        [PasswordHash] TEXT NOT NULL,
                        [Email] TEXT NOT NULL,
                        [Role] TEXT NOT NULL,
                        [CreatedAt] TEXT NOT NULL,
                        [LastLogin] TEXT NULL,
                        [IsActive] INTEGER NOT NULL,
                        [Avatar] TEXT NULL
                    );

                    CREATE TABLE IF NOT EXISTS [Members] (
                        [PlayerId] TEXT NOT NULL PRIMARY KEY,
                        [DisplayName] TEXT NOT NULL,
                        [FirstConnected] TEXT NOT NULL,
                        [LastConnected] TEXT NOT NULL,
                        [LastActivity] TEXT NULL,
                        [IsOnline] INTEGER NOT NULL,
                        [ConnectionId] TEXT NOT NULL,
                        [Avatar] TEXT NULL,
                        [Email] TEXT NULL
                    );

                    CREATE TABLE IF NOT EXISTS [Groups] (
                        [Id] TEXT NOT NULL PRIMARY KEY,
                        [Name] TEXT NOT NULL,
                        [Description] TEXT NOT NULL,
                        [Color] TEXT NOT NULL,
                        [CreatedAt] TEXT NOT NULL,
                        [CreatedBy] TEXT NOT NULL,
                        [IsActive] INTEGER NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS [NotificationTemplates] (
                        [Id] TEXT NOT NULL PRIMARY KEY,
                        [Name] TEXT NOT NULL,
                        [Title] TEXT NOT NULL,
                        [Message] TEXT NOT NULL,
                        [MetadataJson] TEXT NULL,
                        [Category] TEXT NOT NULL,
                        [CreatedAt] TEXT NOT NULL,
                        [CreatedBy] TEXT NOT NULL,
                        [IsActive] INTEGER NOT NULL,
                        [UsageCount] INTEGER NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS [MemberGroups] (
                        [Id] TEXT NOT NULL PRIMARY KEY,
                        [PlayerId] TEXT NOT NULL,
                        [GroupId] TEXT NOT NULL,
                        [JoinedAt] TEXT NOT NULL,
                        FOREIGN KEY ([PlayerId]) REFERENCES [Members]([PlayerId]) ON DELETE CASCADE,
                        FOREIGN KEY ([GroupId]) REFERENCES [Groups]([Id]) ON DELETE CASCADE
                    );

                    CREATE UNIQUE INDEX IF NOT EXISTS [IX_Admins_Username] ON [Admins] ([Username]);
                    CREATE UNIQUE INDEX IF NOT EXISTS [IX_MemberGroups_PlayerId_GroupId] ON [MemberGroups] ([PlayerId], [GroupId]);
                ";
            }

            await command.ExecuteNonQueryAsync();
        }
    }
}
