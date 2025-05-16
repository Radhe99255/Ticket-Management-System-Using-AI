using TicketManagement.Web.Services;
using System.Net.Http;
using System.Net;
using Microsoft.AspNetCore.Http.Connections;
using TicketManagement.Web.Hubs;
using Microsoft.AspNetCore.Authentication.Cookies;

#if DEBUG
// Enable HTTP/2 without TLS (needed for non-HTTPS WebSocket connections)
AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Allow any SSL certificate in development
ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
#endif

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register services
builder.Services.AddHttpContextAccessor();

// Configure HttpClient with SSL certificate validation disabled (for development only)
builder.Services.AddHttpClient("ApiClient", client =>
{
    // Configure client timeout
    client.Timeout = TimeSpan.FromSeconds(30);
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
    // Allow auto-redirection
    AllowAutoRedirect = true,
    // Set credentials to default
    UseDefaultCredentials = true
});

// Add CORS to allow cross-domain SignalR connections
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .SetIsOriginAllowed(_ => true) // Allow all origins for SignalR
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

// Add SignalR with enhanced configuration
builder.Services.AddSignalR(options => 
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 102400; // 100 KB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(120);
    options.KeepAliveInterval = TimeSpan.FromSeconds(30);
    options.HandshakeTimeout = TimeSpan.FromSeconds(60);
    options.StreamBufferCapacity = 20; // Default is 10
}).AddJsonProtocol(options => {
    // Preserve property names (no camelCase conversion)
    options.PayloadSerializerOptions.PropertyNamingPolicy = null;
});

// Register application services - use a single registration for ApiService
builder.Services.AddScoped<IApiService, ApiService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

// Add auth service
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{
    // Special handling for development only
    app.Use(async (context, next) =>
    {
        if (context.Request.Path.StartsWithSegments("/chathub"))
        {
            // For development, ensure proper handling of SignalR requests
            if (context.Request.Headers.ContainsKey("X-Skip-SSL-Verify") &&
                context.Request.Headers["X-Skip-SSL-Verify"] == "true")
            {
                // Add custom header for logging
                context.Request.Headers["X-SignalR-Development-Mode"] = "true";
            }
        }
        await next();
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Use CORS before other middleware - this is critical for OPTIONS preflight requests
app.UseCors("AllowAll");

// Additional middleware for CORS preflight requests
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        // Handle CORS preflight requests, especially for SignalR
        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers.Append("Access-Control-Allow-Origin", origin);
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization, X-Requested-With, X-Skip-SSL-Verify, X-SignalR-User-Agent");
            context.Response.Headers.Append("Access-Control-Allow-Credentials", "true");
            context.Response.Headers.Append("Access-Control-Max-Age", "86400"); // 24 hours
            context.Response.StatusCode = 204; // No content
            return;
        }
    }
    
    await next();
});

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Configure WebSockets with enhanced options
app.UseWebSockets(new Microsoft.AspNetCore.Builder.WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(120),
    ReceiveBufferSize = 4 * 1024 // 4KB
});

// Configure SignalR hub endpoint with detailed options
app.MapHub<ChatHub>("/chathub", options => 
{
    // Enable all transports with WebSockets as preferred
    options.Transports = 
        HttpTransportType.WebSockets | 
        HttpTransportType.ServerSentEvents | 
        HttpTransportType.LongPolling;
        
    // Set buffer sizes
    options.ApplicationMaxBufferSize = 100 * 1024; // 100KB
    options.TransportMaxBufferSize = 100 * 1024;   // 100KB
    
    // Configure transport-specific options
    options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(60);
    options.LongPolling.PollTimeout = TimeSpan.FromSeconds(90);
});

// Configure the regular MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
