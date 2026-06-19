using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;
using InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;
using InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Controllers;

/// <summary>
///     Endpoints para apelaciones de suspensión.
/// </summary>
/// <remarks>
///     <para>Flujo del usuario suspendido:</para>
///     <list type="number">
///         <item><description><c>GET  /api/suspension-appeals/me</c> — ver mi reclamo activo si existe.</description></item>
///         <item><description><c>POST /api/suspension-appeals</c> — enviar reclamo nuevo.</description></item>
///     </list>
///     <para>Flujo del admin:</para>
///     <list type="number">
///         <item><description><c>GET   /api/suspension-appeals/pending</c> — bandeja de entrada.</description></item>
///         <item><description><c>PATCH /api/suspension-appeals/{id}/accept</c> — acepta y levanta la suspensión.</description></item>
///         <item><description><c>PATCH /api/suspension-appeals/{id}/reject</c> — rechaza y mantiene la suspensión.</description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/suspension-appeals")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Suspension Appeals — reclamos de suspensión")]
public class SuspensionAppealsController(
    ISuspensionAppealCommandService commandService,
    ISuspensionAppealQueryService queryService) : ControllerBase
{
    /// <summary>El usuario suspendido envía un reclamo con su versión de los hechos.</summary>
    [HttpPost]
    [SwaggerOperation("Submit appeal", "Suspended user submits an appeal explaining why they consider the suspension unfair.")]
    public async Task<IActionResult> Submit([FromBody] SubmitSuspensionAppealResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(resource.Reason))
            return BadRequest(new { error = "El motivo del reclamo no puede estar vacío." });

        try
        {
            var appeal = await commandService.Handle(new SubmitSuspensionAppealCommand(current.Id, resource.Reason));
            return Created($"/api/suspension-appeals/{appeal.Id}",
                SuspensionAppealResourceFromEntityAssembler.ToResource(appeal));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Devuelve el reclamo activo (pending) del usuario autenticado, si existe.</summary>
    [HttpGet("me")]
    [SwaggerOperation("My active appeal", "Returns the current user's active appeal if any.")]
    public async Task<IActionResult> MyActive()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var appeal = await queryService.Handle(new GetActiveSuspensionAppealByUserIdQuery(current.Id));
        if (appeal is null) return NoContent();
        return Ok(SuspensionAppealResourceFromEntityAssembler.ToResource(appeal));
    }

    /// <summary>Devuelve el historial completo de reclamos del usuario.</summary>
    [HttpGet("me/history")]
    [SwaggerOperation("My appeal history", "Returns the full history of appeals for the current user.")]
    public async Task<IActionResult> MyHistory()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var appeals = await queryService.Handle(new GetSuspensionAppealsByUserIdQuery(current.Id));
        return Ok(appeals.Select(SuspensionAppealResourceFromEntityAssembler.ToResource));
    }

    // ── Endpoints de admin ───────────────────────────────────────────────

    /// <summary>Bandeja de entrada del admin: reclamos pendientes de revisión.</summary>
    [HttpGet("pending")]
    [SwaggerOperation("Pending appeals", "Admin-only list of pending appeals (oldest first).")]
    public async Task<IActionResult> Pending()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Admin) return Forbid();

        var pending = await queryService.Handle(new GetPendingSuspensionAppealsQuery());
        return Ok(pending.Select(SuspensionAppealResourceFromEntityAssembler.ToResource));
    }

    /// <summary>Admin acepta el reclamo: levanta la suspensión automáticamente.</summary>
    [HttpPatch("{id:int}/accept")]
    [SwaggerOperation("Accept appeal", "Admin accepts the appeal and clears the user's suspension.")]
    public async Task<IActionResult> Accept(int id, [FromBody] ReviewSuspensionAppealResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Admin) return Forbid();

        try
        {
            var appeal = await commandService.Handle(new AcceptSuspensionAppealCommand(id, current.Id, resource.Response ?? ""));
            if (appeal is null) return NotFound();
            return Ok(SuspensionAppealResourceFromEntityAssembler.ToResource(appeal));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Admin rechaza el reclamo: mantiene la suspensión.</summary>
    [HttpPatch("{id:int}/reject")]
    [SwaggerOperation("Reject appeal", "Admin rejects the appeal and keeps the suspension active.")]
    public async Task<IActionResult> Reject(int id, [FromBody] ReviewSuspensionAppealResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Admin) return Forbid();

        try
        {
            var appeal = await commandService.Handle(new RejectSuspensionAppealCommand(id, current.Id, resource.Response ?? ""));
            if (appeal is null) return NotFound();
            return Ok(SuspensionAppealResourceFromEntityAssembler.ToResource(appeal));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
