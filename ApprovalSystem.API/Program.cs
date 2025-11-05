using System.Text;
using ApprovalSystem.Infrastructure.Data;
using ApprovalSystem.Models.Entities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using ApprovalSystem.API.Hubs;
using ApprovalSystem.API.Middleware;
using ApprovalSystem.Core.Interfaces;
using ApprovalSystem.Services.Services;
using ApprovalSystem.Services.Configuration;
using ApprovalSystem.Services.ElsaActivities;
using Elsa;
using Elsa.Persistence.EntityFramework.Core.Extensions;
using Elsa.Persistence.EntityFramework.SqlServer;
using Elsa.Workflows.Core.Contexts;
using Elsa.Workflows.Core.Extensions;
using Hangfire;
using Hangfire.Server;
using Hangfire.Storage;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/approval-system-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "نظام إدارة الموافقات API",
        Version = "v1",
        Description = "API لنظام إدارة الموافقات المتقدم",
        Contact = new OpenApiContact
        {
            Name = "فريق التطوير",
            Email = "dev@approvalsystem.com"
        }
    });

    // إضافة JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Database Context
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Elsa Workflow Configuration
builder.Services.AddElsa(elsa => elsa
    .UseEntityFrameworkPersistence(ef => ef.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")))
    .AddConsoleActivities()
    .AddHttpActivities()
    .AddEmailActivities() 
    .AddTimerActivities()
    .AddUserTaskActivities()
    .AddWorkflowActivities()
    .AddApprovalSystemActivities() // Custom Activities
    .AddWorkflowsFrom<Program>()
);

// Elsa Server (API for managing workflows)
builder.Services.AddElsaApiEndpoints();

// Identity Configuration
builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.AllowedUserNameCharacters =
        "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey!)),
        ClockSkew = TimeSpan.Zero
    };

    // إضافة دعم SignalR JWT Authentication
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

// Authorization Policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("Admin"));
    options.AddPolicy("RequireManagerRole", policy => policy.RequireRole("Admin", "Manager"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("Admin", "Manager", "User"));
});

// SignalR Configuration
builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
});

// CORS Configuration
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// AutoMapper Configuration
builder.Services.AddAutoMapper(typeof(Program));

// Hangfire Configuration (Background Jobs)
builder.Services.AddHangfire(configuration =>
{
    configuration.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddHangfireServer();

// Application Services
builder.Services.AddScoped<IRequestService, RequestService>();
builder.Services.AddScoped<IApprovalService, ApprovalService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IWorkflowService, WorkflowService>();
builder.Services.AddScoped<IApprovalMatrixService, ApprovalMatrixService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IBackgroundJobService, BackgroundJobService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Configuration Services (نظام الإعدادات الديناميكية)
builder.Services.AddScoped<IRuleEngine, RuleEngine>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();

// HTTP Client for Elsa Workflow Engine
builder.Services.AddHttpClient(" ElsaWorkflow", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Elsa:ElsaServerUri"] ?? "http://localhost:8080/");
});

// Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "نظام إدارة الموافقات API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at apps root
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

app.UseHttpsRedirection();

// Custom Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<TenantMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseStaticFiles();

app.UseRouting();

// Elsa HTTP Activities Middleware
app.UseHttpActivities();

// Enable CORS
app.UseCors("AllowReactApp");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Hangfire Dashboard (Available at /hangfire)
app.UseHangfireDashboard("/hangfire", new Hangfire.Dashboard.DashboardOptions
{
    Authorization = new[]
    {
        new Hangfire.Dashboard.Authorization.BasicAuthorizationAuthorizationFilter(
            user: "admin",
            password: "admin123" // يجب تغييرها في الإنتاج
        )
    }
});

// API Controllers
app.MapControllers();

// Elsa API Endpoints (Workflow Management)
app.UseElsaApiEndpoints();

// SignalR Hubs
app.MapHub<NotificationHub>("/hubs/notifications");
app.MapHub<RequestHub>("/hubs/requests");
app.MapHub<DashboardHub>("/hubs/dashboard");

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new
{
    Status = "Healthy",
    Timestamp = DateTime.UtcNow,
    Environment = app.Environment.EnvironmentName,
    Version = "1.0.0"
}));

// Seed Database (Development Only)
if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            var userManager = services.GetRequiredService<UserManager<User>>();
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            await SeedData.Initialize(services, userManager, roleManager);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "An error occurred seeding the database");
        }
    }
}

Log.Information("نظام إدارة الموافقات بدأ بنجاح على المنفذ {Port}", app.Urls.FirstOrDefault());

app.Run();

namespace ApprovalSystem.API
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider,
            UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Ensure database is created
            context.Database.EnsureCreated();

            // Create roles
            string[] roleNames = { "Admin", "Manager", "User" };
            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Create admin user
            var adminEmail = "admin@approvalsystem.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    Name = "مدير النظام",
                    Role = "Admin",
                    TenantId = Guid.Parse("550e8400-e29b-41d4-a716-446655440000"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                    Log.Information("Admin user created successfully");
                }
                else
                {
                    Log.Warning("Failed to create admin user: {Errors}", string.Join(", ", result.Errors));
                }
            }
        }
    }
}