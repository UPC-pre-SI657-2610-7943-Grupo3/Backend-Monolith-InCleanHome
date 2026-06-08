using InCleanHome.API.Booking.Application.Internal.CommandServices;
using InCleanHome.API.Booking.Application.Internal.QueryServices;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.Booking.Domain.Services;
using InCleanHome.API.Booking.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.IAM.Application.Internal.CommandServices;
using InCleanHome.API.IAM.Application.Internal.OutboundServices;
using InCleanHome.API.IAM.Application.Internal.QueryServices;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Services;
using InCleanHome.API.IAM.Infrastructure.ExternalServices.Auth0;
using InCleanHome.API.IAM.Infrastructure.Hashing.BCrypt.Services;
using InCleanHome.API.IAM.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;
using InCleanHome.API.IAM.Infrastructure.Tokens.JWT.Configuration;
using InCleanHome.API.IAM.Infrastructure.Tokens.JWT.Services;
using InCleanHome.API.IAM.Interfaces.ACL;
using InCleanHome.API.IAM.Interfaces.ACL.Services;
using InCleanHome.API.Messaging.Application.Internal.CommandServices;
using InCleanHome.API.Messaging.Application.Internal.QueryServices;
using InCleanHome.API.Messaging.Domain.Repositories;
using InCleanHome.API.Messaging.Domain.Services;
using InCleanHome.API.Messaging.Infrastructure.ExternalServices;
using InCleanHome.API.Messaging.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Notifications.Application.ACL;
using InCleanHome.API.Notifications.Application.Internal.CommandServices;
using InCleanHome.API.Notifications.Application.Internal.QueryServices;
using InCleanHome.API.Notifications.Domain.Repositories;
using InCleanHome.API.Notifications.Domain.Services;
using InCleanHome.API.Notifications.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Notifications.Infrastructure.External.Firebase;
using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.Payments.Application.Internal.CommandServices;
using InCleanHome.API.Payments.Application.Internal.QueryServices;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.Payments.Domain.Services;
using InCleanHome.API.Payments.Infrastructure.ExternalServices.Izipay;
using InCleanHome.API.Payments.Infrastructure.ExternalServices.PayPal;
using InCleanHome.API.Payments.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Profiles.Application.ACL;
using InCleanHome.API.Profiles.Application.Internal.CommandServices;
using InCleanHome.API.Profiles.Application.Internal.QueryServices;
using InCleanHome.API.Profiles.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Profiles.Interfaces.ACL;
using InCleanHome.API.Reports.Application.Internal.CommandServices;
using InCleanHome.API.Reports.Application.Internal.QueryServices;
using InCleanHome.API.Reports.Domain.Repositories;
using InCleanHome.API.Reports.Domain.Services;
using InCleanHome.API.Reports.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Application.Internal.CommandServices;
using InCleanHome.API.ReviewsAndEvaluation.Application.Internal.QueryServices;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;
using InCleanHome.API.ReviewsAndEvaluation.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.SearchAndCatalog.Application.Internal.CommandServices;
using InCleanHome.API.SearchAndCatalog.Application.Internal.QueryServices;
using InCleanHome.API.SearchAndCatalog.Domain.Repositories;
using InCleanHome.API.SearchAndCatalog.Domain.Services;
using InCleanHome.API.SearchAndCatalog.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Interfaces.ASP.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Registrar el servicio de mensajería push de Firebase
builder.Services.AddSingleton<InCleanHome.API.Notifications.Domain.Services.IFirebaseMessagingService, 
    InCleanHome.API.Notifications.Infrastructure.External.Firebase.FirebaseNotificationService>();


// Routing & Controllers
builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddControllers(options =>
    options.Conventions.Add(new KebabCaseRouteNamingConvention()));


// CORS — fully open for the Vite dev server (refine in production)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllPolicy",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyMethod()
                        .AllowAnyHeader());
});


// PostgreSQL via Npgsql
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (connectionString.StartsWith("postgres://") || connectionString.StartsWith("postgresql://"))
{
    var uri = new Uri(connectionString);
    var userInfo = uri.UserInfo.Split(':');
    var port = uri.Port > 0 ? uri.Port : 5432;

    connectionString = $"Host={uri.Host};Port={port};Database={uri.LocalPath.TrimStart('/')};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true;";
}

Console.WriteLine("[STARTUP] Connection string parsed successfully.");
builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
        options.UseNpgsql(connectionString)
               .LogTo(Console.WriteLine, LogLevel.Information)
               .EnableSensitiveDataLogging()
               .EnableDetailedErrors();
    else
        options.UseNpgsql(connectionString)
               .LogTo(Console.WriteLine, LogLevel.Error);
});

// Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "InCleanHome.API",
        Version     = "v1",
        Description = "InCleanHome — Domestic Service Hiring Platform API",
        Contact     = new OpenApiContact { Name = "InCleanHome", Email = "contact@incleanhome.pe" },
        License     = new OpenApiLicense
        {
            Name = "Apache 2.0",
            Url  = new Uri("https://www.apache.org/licenses/LICENSE-2.0.html")
        }
    });
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In           = ParameterLocation.Header,
        Description  = "Please enter the JWT token (without the 'Bearer ' prefix)",
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme       = "bearer"
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
            },
            Array.Empty<string>()
        }
    });
    options.EnableAnnotations();
});

// Dependency Injection — per Bounded Context
// Shared
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// IAM
builder.Services.Configure<TokenSettings>(builder.Configuration.GetSection("TokenSettings"));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWorkerDocumentRepository, WorkerDocumentRepository>();
builder.Services.AddScoped<IUserCommandService, UserCommandService>();
builder.Services.AddScoped<IUserQueryService, UserQueryService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IHashingService, HashingService>();
builder.Services.AddScoped<IIamContextFacade, IamContextFacade>();

// Profiles
builder.Services.AddScoped<IClientProfileRepository, ClientProfileRepository>();
builder.Services.AddScoped<IWorkerProfileRepository, WorkerProfileRepository>();
builder.Services.AddScoped<IClientProfileCommandService, ClientProfileCommandService>();
builder.Services.AddScoped<IClientProfileQueryService, ClientProfileQueryService>();
builder.Services.AddScoped<IWorkerProfileCommandService, WorkerProfileCommandService>();
builder.Services.AddScoped<IWorkerProfileQueryService, WorkerProfileQueryService>();
builder.Services.AddScoped<IProfilesContextFacade, ProfilesContextFacade>();

// SearchAndCatalog
builder.Services.AddScoped<IAvailabilitySlotRepository, AvailabilitySlotRepository>();
builder.Services.AddScoped<IAvailabilitySlotCommandService, AvailabilitySlotCommandService>();
builder.Services.AddScoped<IAvailabilitySlotQueryService, AvailabilitySlotQueryService>();

// Booking
builder.Services.AddScoped<IBookingRequestRepository, BookingRequestRepository>();
builder.Services.AddScoped<IBookingRequestCommandService, BookingRequestCommandService>();
builder.Services.AddScoped<IBookingRequestQueryService, BookingRequestQueryService>();

// Payments
builder.Services.AddScoped<IPaymentMethodRepository, PaymentMethodRepository>();
builder.Services.AddScoped<IPaymentMethodCommandService, PaymentMethodCommandService>();
builder.Services.AddScoped<IPaymentMethodQueryService, PaymentMethodQueryService>();
builder.Services.AddScoped<IServicePaymentRepository, ServicePaymentRepository>();
builder.Services.AddScoped<IServicePaymentCommandService, ServicePaymentCommandService>();
builder.Services.AddScoped<IServicePaymentQueryService, ServicePaymentQueryService>();

// ReviewsAndEvaluation
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewCommandService, ReviewCommandService>();
builder.Services.AddScoped<IReviewQueryService, ReviewQueryService>();

// Messaging
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IMessageCommandService, MessageCommandService>();
builder.Services.AddScoped<IMessageQueryService, MessageQueryService>();
builder.Services.AddSingleton<ITwilioConversationsService, TwilioConversationsService>(); // TWILIO

// Notifications
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationCommandService, NotificationCommandService>();
builder.Services.AddScoped<INotificationQueryService, NotificationQueryService>();
builder.Services.AddScoped<INotificationsContextFacade, NotificationsContextFacade>();

// Reports
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();

// ── External Services ──────────────────────────────────────────────────────
// Auth0 (proveedor externo de identidad). Lee la sección "Auth0" de
// appsettings.json. Si Enabled = false el endpoint /api/auth/auth0/login
// responde 503 y el frontend oculta el botón.
builder.Services.Configure<Auth0Settings>(builder.Configuration.GetSection("Auth0"));
builder.Services.AddHttpClient("auth0", c =>
{
    c.Timeout = TimeSpan.FromSeconds(10);
    c.DefaultRequestHeaders.Add("User-Agent", "InCleanHome-API/1.0");
});
builder.Services.AddScoped<IAuth0Service, Auth0Service>();

// Izipay (pasarela de pagos). Por defecto corre en modo simulación.
// Cuando se completen las credenciales reales en appsettings.json
// ("Simulation": false, "ShopId": "...", etc.) automáticamente pasa a hablar
// con la API de Izipay (https://api.micuentaweb.pe).
builder.Services.Configure<IzipaySettings>(builder.Configuration.GetSection("Izipay"));
builder.Services.AddHttpClient("izipay", c =>
{
    c.Timeout = TimeSpan.FromSeconds(15);
    c.DefaultRequestHeaders.Add("User-Agent", "InCleanHome-API/1.0");
});
builder.Services.AddScoped<IIzipayService, IzipayService>();

// PayPal (pasarela de pagos, Orders API v2 con redirect flow). Habilitado si en
// appsettings.json hay PayPal:ClientId y PayPal:ClientSecret. El environment
// Sandbox/Live se controla con PayPal:Environment.
builder.Services.Configure<PayPalSettings>(builder.Configuration.GetSection("PayPal"));
builder.Services.AddHttpClient("paypal", c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
    c.DefaultRequestHeaders.Add("User-Agent", "InCleanHome-API/1.0");
});
builder.Services.AddScoped<IPayPalService, PayPalService>();

var app = builder.Build();

// Database initialization (auto-create on first run)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context  = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();

    // Idempotent schema patch: EnsureCreated() does not migrate existing databases,
    // so for environments where `users` already exists we add the device_token
    // column on the fly. Safe to run on every startup (uses IF NOT EXISTS).
    try
    {
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS device_token VARCHAR(500) NULL;");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not patch users.device_token column: {ex.Message}");
    }

    // Idempotent schema patch: same approach for the documents_rejected column
    // (added so the worker dashboard can show a "documents rejected" banner).
    try
    {
        context.Database.ExecuteSqlRaw(
            "ALTER TABLE IF EXISTS users ADD COLUMN IF NOT EXISTS documents_rejected BOOLEAN NOT NULL DEFAULT FALSE;");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not patch users.documents_rejected column: {ex.Message}");
    }

    // Optional admin bootstrap for coursework/demo deployments.
    // Set ADMIN_EMAIL and ADMIN_PASSWORD in your environment to create the first admin automatically.
    var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? builder.Configuration["AdminSeed:Email"];
    var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? builder.Configuration["AdminSeed:Password"];
    if (!string.IsNullOrWhiteSpace(adminEmail) && !string.IsNullOrWhiteSpace(adminPassword))
    {
        var userRepository = services.GetRequiredService<IUserRepository>();
        if (!userRepository.ExistsByEmail(adminEmail))
        {
            var hashingService = services.GetRequiredService<IHashingService>();
            var admin = new User(adminEmail, hashingService.HashPassword(adminPassword), UserRole.Admin);
            context.Set<User>().Add(admin);
            context.SaveChanges();
            Console.WriteLine($"[STARTUP] Admin user seeded: {adminEmail}");
        }
    }
}

// HTTP request pipeline
// Swagger is always enabled so the Render healthcheck (/swagger/index.html) works
// in all environments (Development, Staging, Production).
app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAllPolicy");
app.UseRequestAuthorization();
app.MapControllers();

app.Run();
