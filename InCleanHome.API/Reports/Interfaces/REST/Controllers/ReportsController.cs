using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.Reports.Domain.Model.Commands;
using InCleanHome.API.Reports.Domain.Model.Queries;
using InCleanHome.API.Reports.Domain.Services;
using InCleanHome.API.Reports.Interfaces.REST.Resources;
using InCleanHome.API.Reports.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Reports.Interfaces.REST.Controllers;

[ApiController]
[Route("api/reports")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Profile reports & moderation")]
public class ReportsController(
    IReportCommandService commandService,
    IReportQueryService queryService,
    INotificationsContextFacade notificationsFacade) : ControllerBase
{
    private bool IsAdmin(out User? current)
    {
        current = (User?)HttpContext.Items["User"];
        return current is not null && current.Role == UserRole.Admin;
    }

    [HttpPost]
    [SwaggerOperation("Create Report", "A user reports another profile (worker or client).")]
    public async Task<IActionResult> Create([FromBody] CreateReportResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        try
        {
            var report = await commandService.Handle(new CreateReportCommand(
                current.Id, resource.ReportedUserId, resource.ReportedRole, resource.Reason, resource.Details));
            return Ok(ReportResourceFromEntityAssembler.ToResourceFromEntity(report));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpGet("my")]
    [SwaggerOperation("My Reports", "Returns reports filed against the current user.")]
    public async Task<IActionResult> MyReports()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var reports = await queryService.Handle(new GetReportsByReportedUserIdQuery(current.Id));
        var resources = reports.Select(ReportResourceFromEntityAssembler.ToResourceFromEntity).ToList();
        return Ok(resources);
    }

    [HttpGet]
    [SwaggerOperation("List Reports", "Returns all reports (admin only).")]
    public async Task<IActionResult> ListAll([FromQuery] int? reportedUserId, [FromQuery] string? status)
    {
        if (!IsAdmin(out _)) return Forbid();

        var reports = reportedUserId.HasValue
            ? await queryService.Handle(new GetReportsByReportedUserIdQuery(reportedUserId.Value))
            : await queryService.Handle(new GetAllReportsQuery());

        if (!string.IsNullOrWhiteSpace(status))
            reports = reports.Where(r => string.Equals(r.Status, status, StringComparison.OrdinalIgnoreCase));

        var resources = reports.Select(ReportResourceFromEntityAssembler.ToResourceFromEntity).ToList();
        return Ok(resources);
    }

    [HttpPatch("{id:int}/confirm")]
    [SwaggerOperation("Confirm Report", "Marks a report as confirmed (admin only).")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ModerateReportResource? resource)
    {
        if (!IsAdmin(out var current)) return Forbid();
        try
        {
            var report = await commandService.Handle(new ConfirmReportCommand(id, current!.Id, resource?.AdminNotes));
            // Notify the reported user that the report was confirmed
            try {
                await notificationsFacade.CreateNotification(report.ReportedUserId, "report_confirmed",
                    "Reporte confirmado",
                    "El administrador ha confirmado un reporte sobre tu cuenta.",
                    "/worker/dashboard");
            } catch { }
            return Ok(ReportResourceFromEntityAssembler.ToResourceFromEntity(report));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPatch("{id:int}/dismiss")]
    [SwaggerOperation("Dismiss Report", "Marks a report as dismissed (admin only).")]
    public async Task<IActionResult> Dismiss(int id, [FromBody] ModerateReportResource? resource)
    {
        if (!IsAdmin(out var current)) return Forbid();
        try
        {
            var report = await commandService.Handle(new DismissReportCommand(id, current!.Id, resource?.AdminNotes));
            // Notify the reported user that the report was dismissed (cleared)
            try {
                await notificationsFacade.CreateNotification(report.ReportedUserId, "report_dismissed",
                    "Reporte descartado",
                    "El administrador ha revisado el reporte sobre tu cuenta y lo ha descartado.",
                    "/worker/dashboard");
            } catch { }
            return Ok(ReportResourceFromEntityAssembler.ToResourceFromEntity(report));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
