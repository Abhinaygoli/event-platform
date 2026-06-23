using EventPlatform.API.Middleware;
using EventPlatform.Application.Interfaces;
using EventPlatform.Infrastructure.Data;
using EventPlatform.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//1. Serilog
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .WriteTo.Console());

//2. Database — EF Core + Neon PostgreSQL
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

//3. JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // no grace period on expiry
    };
});

builder.Services.AddAuthorization();

//4. Register Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<ISessionService, SessionService>();
builder.Services.AddScoped<ISpeakerService, SpeakerService>();
builder.Services.AddScoped<IAgendaService, AgendaService>();
builder.Services.AddScoped<IFavoriteService, FavoriteService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();
// builder.Services.AddHttpClient<IAIRecommendationService, OpenAIService>();

//5. Controllers
builder.Services.AddControllers();

//6. Swagger with JWT support
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Event Platform API",
        Version = "v1",
        Description = "Event Management & Conference Platform"
    });

    // Add JWT Bearer to Swagger UI
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

//7. CORS (for frontend JS in wwwroot)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.WithOrigins(
                "http://localhost:5000",
                "http://localhost:7000",
                "https://localhost:5001",
                "https://localhost:7001",
                "https://localhost:7290",
                "https://event-platform-api-p117.onrender.com" // ← your Render URL
            )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials()); // ← required for cookies to be sent cross-origin
});

//8. SignalR
builder.Services.AddSignalR();

var app = builder.Build();

//Auto-run EF Migrations on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        Log.Information("Database migrations applied successfully.");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to apply database migrations.");
    }
}

//Middleware pipeline
//middleware can catch exceptions thrown during a request, but only for the part of the pipeline that comes after it.
//it will catch exceptions thrown by:
//controllers
//services
//repository code
//EF Core calls
//anything executed after await _next(context)
app.UseMiddleware<ExceptionMiddleware>();
app.UseSerilogRequestLogging(); // logs every HTTP request

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Event Platform API v1");
    c.RoutePrefix = "swagger"; // access at /swagger
});

app.UseStaticFiles();           // serve wwwroot (HTML/CSS/JS)
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
//app.MapHub<NotificationHub>("/hubs/notifications");
//Health Check endpoint
// Keeps Render alive via UptimeRobot ping
app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

//Fallback: serve index.html for frontend routes
app.MapFallbackToFile("index.html");

Log.Information("Event Platform API starting...");
app.Run();