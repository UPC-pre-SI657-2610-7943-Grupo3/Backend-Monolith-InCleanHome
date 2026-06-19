using System.Net.Mime;
using InCleanHome.API.IAM.Application.Internal.OutboundServices;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Services.External;
using InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.REST.Transform;
using InCleanHome.API.Shared.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.IAM.Infrastructure.ExternalServices.Auth0;

/// <summary>
/// Endpoints para el flujo de autenticación con Auth0.
///
///   GET  /api/auth/auth0/status                  — saber si Auth0 está habilitado
///   POST /api/auth/auth0/login                   — intercambiar token Auth0 por JWT propio
///   POST /api/auth/auth0/complete-registration   — completar el registro con rol + datos del perfil
///
/// FLUJO:
///   1) Frontend → Universal Login en Auth0 → callback con access_token.
///   2) Frontend → POST /login { accessToken }.
///   3) Si el usuario EXISTE en BD → JWT propio + datos → frontend redirige según rol.
///   4) Si NO existe → backend devuelve { needsRoleSelection: true, email, name }
///      SIN crear nada en BD. El frontend va a /welcome.
///   5) En /welcome el usuario elige rol y rellena los datos del perfil.
///   6) Frontend → POST /complete-registration { accessToken, role, name, phone, ... }
///      → backend crea User + perfil completo, devuelve JWT propio.
/// </summary>
[ApiController]
[Route("api/auth/auth0")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Auth0 — login con proveedor externo de identidad")]
public class Auth0LoginController(
    IIdentityProvider identityProvider,
    IUserRepository userRepository,
    ITokenService tokenService,
    IClientProfileCommandService clientProfileCommandService,
    IClientProfileQueryService clientProfileQueryService,
    IWorkerProfileCommandService workerProfileCommandService,
    IWorkerProfileQueryService workerProfileQueryService,
    IUnitOfWork unitOfWork,
    IConfiguration configuration) : ControllerBase
{
    public record Auth0LoginRequest(string AccessToken);

    /// <summary>
    /// Cuerpo del /complete-registration. Trae el rol elegido en /welcome más
    /// todos los datos del perfil (los que antes pedía el RegisterView clásico).
    /// Email y password no se mandan: el email viene de Auth0 y la contraseña
    /// del usuario ya está en Auth0 — la app guarda un password aleatorio para
    /// satisfacer el dominio (que nunca se va a validar acá).
    /// </summary>
    public record Auth0CompleteRegistrationRequest(
        string AccessToken,
        string Role,
        // Comunes
        string? Name,
        string? Phone,
        // Sólo para worker:
        int? Age,
        string? Gender,
        List<string>? ServiceTypes,
        List<string>? Zones,
        decimal? HourlyRate,
        decimal? HourlyRateSunday,
        int? ExperienceYears,
        string? Bio);

    [HttpGet("status")]
    [AllowAnonymous]
    [SwaggerOperation("Auth0 Status",
        "Devuelve si Auth0 está habilitado en este backend.")]
    public IActionResult Status()
        => Ok(new { enabled = identityProvider.IsEnabled });

    [HttpPost("login")]
    [AllowAnonymous]
    [SwaggerOperation("Auth0 Login",
        "Recibe el access_token de Auth0, valida la firma y consulta /userinfo. " +
        "Si el usuario existe en BD devuelve JWT propio. Si no existe, devuelve " +
        "needsRoleSelection=true para que el frontend pase a /welcome.")]
    public async Task<IActionResult> Login([FromBody] Auth0LoginRequest body)
    {
        if (!identityProvider.IsEnabled)
            return StatusCode(503, new { error = "Auth0 is not enabled on this backend" });

        if (string.IsNullOrWhiteSpace(body?.AccessToken))
            return BadRequest(new { error = "accessToken is required" });

        var info = await identityProvider.ValidateAndGetUserInfoAsync(body.AccessToken);
        if (info is null)
            return Unauthorized(new { error = "Invalid Auth0 token" });

        // Buscar usuario por email. El AdminSeed crea automáticamente al admin al
        // arrancar el backend, así que el admin entra acá sin pasar por /welcome.
        var user = await userRepository.FindByEmailAsync(info.Email);

        // Protección extra: si el email es el del admin configurado pero no está
        // en BD por cualquier motivo, lo creamos automáticamente como Admin.
        if (user is null && IsAdminEmail(info.Email))
        {
            user = new User(info.Email, "AUTH0_" + Guid.NewGuid().ToString("N"), UserRole.Admin);
            user.Verify();
            await userRepository.AddAsync(user);
            await unitOfWork.CompleteAsync();
            Console.WriteLine($"[Auth0] Admin auto-provisioned from Auth0 login: {info.Email}");
        }

        if (user is null)
        {
            // Usuario nuevo no-admin: el frontend pasará a /welcome.
            return Ok(new
            {
                needsRoleSelection = true,
                email = info.Email,
                name = info.Name,
                picture = info.PictureUrl
            });
        }

        // Usuario existente: emitir JWT propio y devolver el payload completo.
        return Ok(await BuildLoginResponse(user, info.Name));
    }

    [HttpPost("complete-registration")]
    [AllowAnonymous]
    [SwaggerOperation("Auth0 — Completar registro",
        "El usuario eligió rol en /welcome y rellenó los datos. Validamos otra " +
        "vez el token de Auth0 (por seguridad) y creamos el usuario en BD con el " +
        "rol y el perfil completo. Workers quedan pendientes de subir documentos.")]
    public async Task<IActionResult> CompleteRegistration([FromBody] Auth0CompleteRegistrationRequest body)
    {
        if (!identityProvider.IsEnabled)
            return StatusCode(503, new { error = "Auth0 is not enabled on this backend" });

        if (string.IsNullOrWhiteSpace(body?.AccessToken))
            return BadRequest(new { error = "accessToken is required" });

        // Solo "client" o "worker" son válidos. Admin no se elige acá.
        var roleLower = (body.Role ?? string.Empty).Trim().ToLowerInvariant();
        if (roleLower != "client" && roleLower != "worker")
            return BadRequest(new { error = "role must be 'client' or 'worker'" });

        // Datos comunes obligatorios.
        if (string.IsNullOrWhiteSpace(body.Name))
            return BadRequest(new { error = "name is required" });
        if (string.IsNullOrWhiteSpace(body.Phone))
            return BadRequest(new { error = "phone is required" });

        // Validaciones específicas de worker.
        if (roleLower == "worker")
        {
            if (body.Age is null || body.Age < 18 || body.Age > 70)
                return BadRequest(new { error = "age must be between 18 and 70" });
            if (string.IsNullOrWhiteSpace(body.Gender) ||
                !Profiles.Domain.Model.ValueObjects.Gender.IsValid(body.Gender!))
                return BadRequest(new { error = "Invalid gender" });
            if (body.ServiceTypes is null || body.ServiceTypes.Count == 0)
                return BadRequest(new { error = "Select at least one service type" });
            if (body.HourlyRate is null || body.HourlyRate < 10)
                return BadRequest(new { error = "hourlyRate must be >= 10" });
        }

        // Validar token Auth0.
        var info = await identityProvider.ValidateAndGetUserInfoAsync(body.AccessToken);
        if (info is null)
            return Unauthorized(new { error = "Invalid Auth0 token" });

        // Si el usuario YA existe en BD, devolvemos su sesión actual (no
        // sobrescribimos su perfil). Esto cubre el caso del usuario que entra
        // a /welcome dos veces por error.
        var existing = await userRepository.FindByEmailAsync(info.Email);
        if (existing is not null)
            return Ok(await BuildLoginResponse(existing, info.Name));

        // El email del admin no se puede usar para registrarse como client/worker.
        if (IsAdminEmail(info.Email))
            return StatusCode(403, new { error = "Reserved email" });

        // Crear usuario.
        var randomPwd = "AUTH0_" + Guid.NewGuid().ToString("N");
        var role = roleLower == "worker" ? UserRole.Worker : UserRole.Client;
        var user = new User(info.Email, randomPwd, role);
        user.Verify();
        await userRepository.AddAsync(user);
        await unitOfWork.CompleteAsync();

        // Crear perfil completo según el rol.
        if (role == UserRole.Client)
        {
            await clientProfileCommandService.Handle(
                new CreateClientProfileCommand(user.Id, body.Name!, body.Phone!));
        }
        else // Worker
        {
            await workerProfileCommandService.Handle(new CreateWorkerProfileCommand(
                user.Id,
                body.Name!,
                body.Phone!,
                body.Age!.Value,
                body.Gender!,
                body.ServiceTypes!,
                body.Zones ?? new List<string>(),
                body.HourlyRate!.Value,
                // Fallback defensivo: si por alguna razón el frontend no manda la
                // tarifa de domingo, usamos la normal. El form la marca como
                // obligatoria, así que esto solo cubre clientes legacy.
                body.HourlyRateSunday ?? body.HourlyRate!.Value,
                body.ExperienceYears ?? 0,
                body.Bio ?? string.Empty));
        }

        return Ok(await BuildLoginResponse(user, body.Name!, createdNewUser: true));
    }

    // ──────────────────────────────────────────────────────────────────────

    private bool IsAdminEmail(string email)
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL")
                         ?? configuration["AdminSeed:Email"]
                         ?? "admin@incleanhome.pe";
        return string.Equals(email, adminEmail, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<object> BuildLoginResponse(User user, string fallbackName, bool createdNewUser = false)
    {
        var token = tokenService.GenerateToken(user);

        string name = fallbackName;
        string? phone = null;
        if (user.Role == UserRole.Worker)
        {
            var w = await workerProfileQueryService.Handle(new GetWorkerProfileByUserIdQuery(user.Id));
            if (w is not null) { name = w.Name; phone = w.Phone; }
        }
        else if (user.Role == UserRole.Client)
        {
            var c = await clientProfileQueryService.Handle(new GetClientProfileByUserIdQuery(user.Id));
            if (c is not null) { name = c.Name; phone = c.Phone; }
        }

        var payload = UserPayloadFromEntityAssembler.FromUserAndProfile(user, name, phone);
        return new
        {
            user = payload,
            token,
            createdNewUser,
            authProvider = "auth0"
        };
    }
}
