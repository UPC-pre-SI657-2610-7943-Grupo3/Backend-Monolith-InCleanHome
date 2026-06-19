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
using InCleanHome.API.IAM.Domain.Services.External;
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
using InCleanHome.API.Messaging.Domain.Services.External;
using InCleanHome.API.Messaging.Infrastructure.ExternalServices.Twilio;
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
using InCleanHome.API.Payments.Domain.Services.External;
using InCleanHome.API.Payments.Infrastructure.ExternalServices.MercadoPago;
using InCleanHome.API.Payments.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Profiles.Application.ACL;
using InCleanHome.API.Profiles.Application.Internal.CommandServices;
using InCleanHome.API.Profiles.Application.Internal.QueryServices;
using InCleanHome.API.Profiles.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Infrastructure.Persistence.EFC.Repositories;
using InCleanHome.API.Profiles.Interfaces.ACL;
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
using InCleanHome.API.Shared.Application.Internal.CommandServices;
using InCleanHome.API.Shared.Application.Internal.QueryServices;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Domain.Services;
using InCleanHome.API.Shared.Infrastructure.Interfaces.ASP.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Registrar el servicio de mensajería push de Firebase
builder.Services.AddSingleton<InCleanHome.API.Notifications.Domain.Services.External.IPushNotificationProvider, 
    InCleanHome.API.Notifications.Infrastructure.External.Firebase.FirebaseCloudMessagingAdapter>();


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
builder.Services.AddSingleton<IRealtimeMessagingProvider, TwilioRealtimeMessagingAdapter>(); // TWILIO

// Notifications
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationCommandService, NotificationCommandService>();
builder.Services.AddScoped<INotificationQueryService, NotificationQueryService>();
builder.Services.AddScoped<INotificationsContextFacade, NotificationsContextFacade>();

// Reports
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IReportCommandService, ReportCommandService>();
builder.Services.AddScoped<IReportQueryService, ReportQueryService>();

// SuspensionAppeal — reclamos de suspensión (mismo BC que Reports).
builder.Services.AddScoped<ISuspensionAppealRepository, SuspensionAppealRepository>();
builder.Services.AddScoped<ISuspensionAppealCommandService, SuspensionAppealCommandService>();
builder.Services.AddScoped<ISuspensionAppealQueryService, SuspensionAppealQueryService>();

// F3: Platform settings (configuración global parametrizable por admin).
// El provider lee directamente del repositorio en cada request (sin caché).
// Para el volumen esperado del proyecto, una consulta extra a BD por pago es
// trivial. Si en el futuro hace falta optimizar, se puede envolver con
// IMemoryCache sin tocar los callers.
builder.Services.AddScoped<IPlatformSettingsRepository, PlatformSettingsRepository>();
builder.Services.AddScoped<IPlatformSettingsCommandService, PlatformSettingsCommandService>();
builder.Services.AddScoped<IPlatformSettingsQueryService, PlatformSettingsQueryService>();
builder.Services.AddScoped<ICommissionRateProvider, CommissionRateProvider>();

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
builder.Services.AddScoped<IIdentityProvider, Auth0IdentityProviderAdapter>();

// Mercado Pago Perú (única pasarela de pagos del proyecto). Se registra como
// adapter del patrón Ports & Adapters: el dominio depende de
// IPaymentGatewayProvider y el adapter concreto MercadoPagoAdapter lo
// implementa traduciendo a llamadas REST contra https://api.mercadopago.com.
// Si mañana se cambia de pasarela, basta con crear otro adapter que implemente
// la misma interfaz y registrarlo aquí.
builder.Services.Configure<MercadoPagoSettings>(builder.Configuration.GetSection(MercadoPagoSettings.SectionName));
builder.Services.AddHttpClient<IPaymentGatewayProvider, MercadoPagoAdapter>(c =>
{
    c.Timeout = TimeSpan.FromSeconds(20);
    c.DefaultRequestHeaders.Add("User-Agent", "InCleanHome-API/1.0");
});

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

    // ── F1: Tarifa de domingo en WorkerProfile ───────────────────────────
    // Columna nueva con default = la tarifa normal de cada worker, así las
    // trabajadoras existentes no quedan con 0 hasta que entren a editar perfil.
    try
    {
        context.Database.ExecuteSqlRaw(@"
            ALTER TABLE IF EXISTS worker_profiles
                ADD COLUMN IF NOT EXISTS hourly_rate_sunday NUMERIC(10,2) NULL;
            UPDATE worker_profiles
               SET hourly_rate_sunday = hourly_rate
             WHERE hourly_rate_sunday IS NULL;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not patch worker_profiles.hourly_rate_sunday: {ex.Message}");
    }

    // ── F1: Índices GIN sobre los arrays text[] ──────────────────────────
    // Optimiza las búsquedas WHERE 'x' = ANY(zones) y WHERE 'x' = ANY(service_types)
    // de O(N) a O(log N). Indispensable cuando crezca la cantidad de trabajadoras.
    // No usamos substring ni split por comas en ninguna consulta — el array es
    // un tipo nativo de PostgreSQL.
    try
    {
        context.Database.ExecuteSqlRaw(@"
            CREATE INDEX IF NOT EXISTS ix_worker_profiles_zones
                ON worker_profiles USING GIN (zones);
            CREATE INDEX IF NOT EXISTS ix_worker_profiles_service_types
                ON worker_profiles USING GIN (service_types);
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not create GIN indexes: {ex.Message}");
    }

    // ── F1: Renombrar columnas legacy de pasarela (Izipay/PayPal → MercadoPago) ─
    // Si la BD ya tenía las columnas viejas, intentamos renombrarlas para que
    // los registros históricos sobrevivan. Si las columnas viejas no existen
    // (BD nueva), las "ADD COLUMN IF NOT EXISTS" finales aseguran que las nuevas
    // existan igual.
    try
    {
        context.Database.ExecuteSqlRaw(@"
            ALTER TABLE IF EXISTS service_payments RENAME COLUMN izipay_order_id      TO mercado_pago_payment_id;
        ");
    } catch { /* la columna ya fue renombrada o nunca existió */ }
    try
    {
        context.Database.ExecuteSqlRaw(@"
            ALTER TABLE IF EXISTS service_payments RENAME COLUMN izipay_transaction_id TO mercado_pago_preference_id;
        ");
    } catch { /* idem */ }
    try
    {
        // Garantizamos que las columnas existan en su nombre nuevo.
        context.Database.ExecuteSqlRaw(@"
            ALTER TABLE IF EXISTS service_payments
                ADD COLUMN IF NOT EXISTS mercado_pago_payment_id    VARCHAR(100) NULL;
            ALTER TABLE IF EXISTS service_payments
                ADD COLUMN IF NOT EXISTS mercado_pago_preference_id VARCHAR(100) NULL;
            -- Quitamos columnas paypal legacy si seguían ahí (datos históricos se descartan).
            ALTER TABLE IF EXISTS service_payments DROP COLUMN IF EXISTS pay_pal_order_id;
            ALTER TABLE IF EXISTS service_payments DROP COLUMN IF EXISTS pay_pal_capture_id;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not patch service_payments MP columns: {ex.Message}");
    }

    // ── F2: Tabla de suspension_appeals (reclamos contra suspensión) ─────
    // EnsureCreated() la crea en BDs nuevas; este IF NOT EXISTS cubre BDs ya
    // existentes en producción para no requerir migración manual.
    try
    {
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS suspension_appeals (
                id                        SERIAL PRIMARY KEY,
                user_id                   INTEGER NOT NULL,
                reason                    VARCHAR(2000) NOT NULL,
                status                    VARCHAR(20) NOT NULL,
                reviewed_by_admin_user_id INTEGER NULL,
                reviewed_at               TIMESTAMPTZ NULL,
                admin_response            VARCHAR(1000) NOT NULL DEFAULT '',
                created_at                TIMESTAMPTZ NULL,
                updated_at                TIMESTAMPTZ NULL
            );
            CREATE INDEX IF NOT EXISTS ix_suspension_appeals_user_id_status
                ON suspension_appeals (user_id, status);
            CREATE INDEX IF NOT EXISTS ix_suspension_appeals_status
                ON suspension_appeals (status);
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not ensure suspension_appeals table: {ex.Message}");
    }

    // ── F3: Tabla de platform_settings (single-record) ────────────────────
    // CREATE IF NOT EXISTS + INSERT IF NOT EXISTS para sembrar el único registro
    // con id=1 y comisión 10%. Si admin ya cambió la tasa, no se sobreescribe.
    try
    {
        context.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS platform_settings (
                id                            INTEGER PRIMARY KEY,
                commission_rate               NUMERIC(5,4) NOT NULL DEFAULT 0.10,
                last_updated_by_admin_user_id INTEGER NULL,
                created_at                    TIMESTAMPTZ NULL,
                updated_at                    TIMESTAMPTZ NULL
            );
            INSERT INTO platform_settings (id, commission_rate, created_at, updated_at)
                 VALUES (1, 0.10, NOW(), NOW())
            ON CONFLICT (id) DO NOTHING;
        ");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not ensure platform_settings table: {ex.Message}");
    }

    // Cleanup: si una versión anterior (F5) creó la tabla sales_receipts, la
    // dropeamos. Es idempotente: si no existe no pasa nada.
    try
    {
        context.Database.ExecuteSqlRaw("DROP TABLE IF EXISTS sales_receipts CASCADE;");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[STARTUP] Could not drop legacy sales_receipts table: {ex.Message}");
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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAllPolicy");
app.UseRequestAuthorization();
app.MapControllers();

app.Run();
