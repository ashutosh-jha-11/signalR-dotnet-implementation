using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using SignalRNotificationsDemo.Data;
using SignalRNotificationsDemo.Services;
using SignalRNotificationsDemo.Hubs;
using SignalRNotificationsDemo.Helpers;
using System.Threading;

var builder = WebApplication.CreateBuilder(args);

// Connection string: prefer environment variable for SQL Server from docker-compose.
// Fallback to SQLite for local dev without docker.
var sqlConn = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrEmpty(sqlConn))
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(sqlConn));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite("Data Source=notifications.db"));
}

// SignalR
builder.Services.AddSignalR();
// For demo: use query string based user id provider. Replace in production with real IUserIdProvider using Claims.
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, QueryUserIdProvider>();

// app services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddControllers();

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var isDocker = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"));
    
    if (isDocker)
    {
        // Docker environment - retry for SQL Server startup
        var attempts = 0;
        var maxAttempts = 30;
        var initialized = false;
        while (!initialized && attempts < maxAttempts)
        {
            try
            {
                db.Database.Migrate();
                await DatabaseInitializer.InitializeAsync(db);
                DbSeed.Seed(db);
                initialized = true;
            }
            catch (Exception ex)
            {
                attempts++;
                Console.WriteLine($"Database initialization attempt {attempts} failed: {ex.Message}");
                Thread.Sleep(2000);
            }
        }
        if (!initialized) Console.WriteLine("Warning: database initialization did not complete after retries.");
    }
    else
    {
        // Local development - use DatabaseInitializer for both SQL Server and SQLite
        try
        {
            Console.WriteLine("Initializing database for local development...");
            
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrEmpty(connectionString))
            {
                Console.WriteLine("Using SQL Server for local development");
                // First ensure database exists, then create tables
                await db.Database.EnsureCreatedAsync();
                await DatabaseInitializer.InitializeAsync(db);
            }
            else
            {
                Console.WriteLine("Using SQLite for local development");
                await db.Database.EnsureCreatedAsync();
                await DatabaseInitializer.InitializeAsync(db);
            }
            
            // Seed data after database is ready
            DbSeed.Seed(db);
            Console.WriteLine("Database initialized successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization failed: {ex.Message}");
            throw;
        }
    }
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
