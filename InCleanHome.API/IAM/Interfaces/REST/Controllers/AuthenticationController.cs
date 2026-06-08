using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Commands;
using InCleanHome.API.IAM.Domain.Model.Queries;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Domain.Services;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.IAM.Interfaces.REST.Controllers;

/// <summary>
///     Authentication endpoints consumed by the Vue frontend.
/// </summary>
/// <remarks>
///     IMPORTANTE: la autenticación principal (login + alta) corre por Auth0
///     (ver <c>Auth0LoginController</c>). Este controller solo expone endpoints
///     que el frontend SÍ usa después del login:
///     <list type="bullet">
///         <item><description>GET  /api/auth/me</description></item>
///         <item><description>POST /api/auth/worker/upload-document</description></item>
///         <item><description>POST /api/auth/device-token</description></item>
///     </list>
///     Los endpoints <c>/login</c>, <c>/register/client</c>, <c>/register/worker</c>,
///     <c>/forgot-password</c> y <c>/reset-password</c> fueron removidos: Auth0 los
///     reemplazó por completo (Universal Login + flujo /welcome para completar el
///     perfil; el reset de contraseña lo maneja directamente Auth0).
/// </remarks>
[ApiController]
[Route("api/auth")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Authentication & worker onboarding")]
public class AuthenticationController(
    IUserCommandService userCommandService,
    IClientProfileQueryService clientProfileQueryService,
    IWorkerProfileQueryService workerProfileQueryService) : ControllerBase
{
    [HttpGet("me")]
    [SwaggerOperation("Get Current User", "Returns the current user's data including suspension status.")]
    public async Task<IActionResult> Me()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var (name, phone) = await ResolveNamePhone(current);
        var payload = UserPayloadFromEntityAssembler.FromUserAndProfile(current, name, phone);
        return Ok(payload);
    }

    public record UploadDocumentResource(string DocumentType, string FileBase64, string FileName);

    [HttpPost("worker/upload-document")]
    [SwaggerOperation("Upload Worker Document", "Upload a PDF (background_check or experience). Si los documentos fueron rechazados, este endpoint los re-acepta y limpia la marca DocumentsRejected.")]
    public async Task<IActionResult> UploadDocument([FromBody] UploadDocumentResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker) return Forbid();

        try
        {
            await userCommandService.Handle(new UploadWorkerDocumentCommand(
                current.Id, resource.DocumentType, resource.FileName, resource.FileBase64));
            return Ok(new { message = "Document uploaded successfully" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    // Firebase Cloud Messaging — device token registration
    public record DeviceTokenResource(string? Token);

    [HttpPost("device-token")]
    [SwaggerOperation(
        "Register FCM Device Token",
        "Stores the Firebase Cloud Messaging token of the user's current browser/device so the backend can send push notifications. Pass an empty/null token to clear it (e.g. on logout).")]
    public async Task<IActionResult> RegisterDeviceToken([FromBody] DeviceTokenResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        try
        {
            await userCommandService.Handle(new RegisterDeviceTokenCommand(current.Id, resource.Token));
            return Ok(new { message = "Device token registered successfully" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    // Helpers
    private async Task<(string name, string? phone)> ResolveNamePhone(User user)
    {
        if (user.Role == UserRole.Worker)
        {
            var w = await workerProfileQueryService.Handle(new GetWorkerProfileByUserIdQuery(user.Id));
            return (w?.Name ?? user.Email, w?.Phone);
        }
        else
        {
            var c = await clientProfileQueryService.Handle(new GetClientProfileByUserIdQuery(user.Id));
            return (c?.Name ?? user.Email, c?.Phone);
        }
    }
}
