using Microsoft.EntityFrameworkCore;
using TicketManagement.Data.DataContext;
using TicketManagement.Data.Repositories;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Serilog;
using Serilog.Events;

// Disable SSL certificate validation completely in development
// #if DEBUG
// ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
// ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
// #endif

#if DEBUG
// Enable HTTP/2 without TLS (needed for non-HTTPS connections)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Allow any SSL certificate in development
ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
#endif

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.File("Logs/info-.txt",
        restrictedToMinimumLevel: LogEventLevel.Information,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/warning-.txt",
        restrictedToMinimumLevel: LogEventLevel.Warning,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("Logs/error-.txt",
        restrictedToMinimumLevel: LogEventLevel.Error,
        rollingInterval: RollingInterval.Day,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog to the application
    builder.Host.UseSerilog();

    // Add services to the container.
    builder.Services.AddDbContext<TicketDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Register repositories
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ITicketRepository, TicketRepository>();
    builder.Services.AddScoped<IMessageRepository, MessageRepository>();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // Add CORS to allow requests from the web app
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowWebApp",
            builder => builder
                .WithOrigins(
                    "https://localhost:7000", 
                    "http://localhost:5162", 
                    "https://localhost:7204", 
                    "http://localhost:5204"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials());
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseHttpsRedirection();
    }

    // Use CORS before routing - this is critical for OPTIONS preflight requests
    app.UseCors("AllowWebApp");

    // Additional middleware for CORS preflight requests
    app.Use(async (context, next) =>
    {
        // For any request coming from allowed origins
        string origin = context.Request.Headers["Origin"];
        if (!string.IsNullOrEmpty(origin))
        {
            // Log incoming request
            Log.Information("Incoming request from origin: {Origin}, Method: {Method}, Path: {Path}", 
                origin, context.Request.Method, context.Request.Path);

            // Explicitly set CORS headers for all responses
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-Skip-SSL-Verify");
            
            // Handle OPTIONS requests immediately
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 204; // No content
                Log.Debug("Handled OPTIONS preflight request for {Path}", context.Request.Path);
                return;
            }
        }
        
        await next();
    });

    app.UseRouting();
    app.UseAuthorization();

    // Map controllers
    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
