using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Commands;
using InCleanHome.API.IAM.Domain.Model.Queries;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Services;
using InCleanHome.API.Notifications.Interfaces.ACL;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.IAM.Interfaces.REST.Controllers;

/// <summary>
///     Administrative endpoints for account verification / moderation.
///     Restricted to users with the <c>admin</c> role.
/// </summary>
/// <remarks>
///     Frontend wiring (admin panel, optional):
///     <list type="bullet">
///         <item><description>GET  /api/admin/users</description></item>
///         <item><description>PATCH /api/admin/users/{id}/verify</description></item>
///         <item><description>PATCH /api/admin/users/{id}/approve-documents</description></item>
///     </list>
/// </remarks>
public record SuspendUserResource(int Days, string? Reason);

[ApiController]
[Route("api/admin")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Administration & verification")]
public class AdminController(
    IUserCommandService userCommandService,
    IUserQueryService userQueryService,
    INotificationsContextFacade notificationsFacade,
    IWorkerDocumentRepository workerDocumentRepository) : ControllerBase
{
    private bool IsAdmin(out User? current)
    {
        current = (User?)HttpContext.Items["User"];
        return current is not null && current.Role == UserRole.Admin;
    }

    [HttpGet("users")]
    [SwaggerOperation("List Users", "Returns all users (admin only).")]
    public async Task<IActionResult> ListUsers()
    {
        if (!IsAdmin(out _)) return Forbid();
        var users = await userQueryService.Handle(new GetAllUsersQuery());
        return Ok(users);
    }

    [HttpPatch("users/{id:int}/verify")]
    [SwaggerOperation("Verify User", "Activates a user account (admin only).")]
    public async Task<IActionResult> Verify(int id)
    {
        if (!IsAdmin(out _)) return Forbid();
        try
        {
            await userCommandService.Handle(new VerifyUserCommand(id));
            return Ok(new { message = "User verified" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPatch("users/{id:int}/approve-documents")]
    [SwaggerOperation("Approve Worker Documents", "Approves a worker's documents and activates the account (admin only).")]
    public async Task<IActionResult> ApproveDocuments(int id)
    {
        if (!IsAdmin(out _)) return Forbid();
        try
        {
            await userCommandService.Handle(new ApproveWorkerDocumentsCommand(id));
            // Notify the worker that their account is approved
            try {
                await notificationsFacade.CreateNotification(id, "approved",
                    "¡Cuenta aprobada!",
                    "Tu perfil ha sido verificado por el administrador. Ya puedes recibir reservas y aparecer en búsquedas.",
                    "/worker/dashboard");
            } catch { }
            return Ok(new { message = "Worker documents approved" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPatch("users/{id:int}/reject-documents")]
    [SwaggerOperation("Reject Worker Documents", "Rejects a worker's documents. The account stays but is marked as unverified — the worker can re-upload (admin only).")]
    public async Task<IActionResult> RejectDocuments(int id)
    {
        if (!IsAdmin(out _)) return Forbid();
        try
        {
            await userCommandService.Handle(new RejectWorkerDocumentsCommand(id));
            // Notify the worker that their documents were rejected. The link goes
            // to /worker/profile because that's where the "Editar documentos" button
            // lives now (the dashboard no longer shows a rejection banner).
            try {
                await notificationsFacade.CreateNotification(id, "rejected",
                    "Documentos rechazados",
                    "El administrador rechazó tus documentos. Edítalos desde tu perfil para reactivar tu cuenta.",
                    "/worker/profile");
            } catch { }
            return Ok(new { message = "Worker documents rejected" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPatch("users/{id:int}/suspend")]
    [SwaggerOperation("Suspend User", "Temporarily suspends a user account (admin only).")]
    public async Task<IActionResult> Suspend(int id, [FromBody] SuspendUserResource resource)
    {
        if (!IsAdmin(out _)) return Forbid();
        try
        {
            var days = resource.Days <= 0 ? 1 : resource.Days;
            await userCommandService.Handle(new SuspendUserCommand(id, TimeSpan.FromDays(days), resource.Reason ?? "Suspensión administrativa"));
            // Notify the suspended user
            try {
                await notificationsFacade.CreateNotification(id, "suspension",
                    "Cuenta suspendida",
                    $"Tu cuenta ha sido suspendida por {days} día(s). Motivo: {resource.Reason ?? "Suspensión administrativa"}.",
                    "/");
            } catch { }
            return Ok(new { message = "User suspended", days });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPatch("users/{id:int}/clear-suspension")]
    [SwaggerOperation("Clear Suspension", "Removes the active suspension from a user account (admin only).")]
    public async Task<IActionResult> ClearSuspension(int id)
    {
        if (!IsAdmin(out _)) return Forbid();
        try
        {
            await userCommandService.Handle(new ClearUserSuspensionCommand(id));
            // Notify the user that their suspension has been lifted
            try {
                await notificationsFacade.CreateNotification(id, "suspension_cleared",
                    "Suspensión levantada",
                    "Tu cuenta ha sido reactivada. Ya puedes usar la plataforma con normalidad.",
                    "/");
            } catch { }
            return Ok(new { message = "User suspension cleared" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpGet("users/{id:int}/documents")]
    [SwaggerOperation("Get Worker Documents", "Returns documents uploaded by a worker (admin only).")]
    public async Task<IActionResult> GetDocuments(int id)
    {
        if (!IsAdmin(out _)) return Forbid();
        var docs = await workerDocumentRepository.FindByUserIdAsync(id);
        var result = docs.Select(d => new {
            d.Id,
            d.UserId,
            d.DocumentType,
            d.FileName,
            d.FileBase64,
            d.CreatedDate
        });
        return Ok(result);
    }
    [HttpDelete("users/{id:int}")]
    [SwaggerOperation("Delete User", "Permanently deletes a user account (admin only). Cannot delete admin accounts.")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        if (!IsAdmin(out _)) return Forbid();
        try
        {
            await userCommandService.Handle(new DeleteUserCommand(id));
            return Ok(new { message = "User deleted successfully" });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
