using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Domain.Model.Commands;
using InCleanHome.API.Notifications.Domain.Model.Queries;
using InCleanHome.API.Notifications.Domain.Services;
using InCleanHome.API.Notifications.Domain.Services.External;
using InCleanHome.API.Notifications.Interfaces.REST.Resources;
using InCleanHome.API.Notifications.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Notifications.Interfaces.REST.Controllers;

/// <summary>
///     Notification endpoints consumed by the Vue frontend.
/// </summary>
/// <remarks>
///     Frontend wiring (see <c>src/Shared/stores/notifications.js</c> and
///     <c>src/Shared/views/NotificationsView.vue</c>):
///     <list type="bullet">
///         <item><description>GET /api/notifications</description></item>
///         <item><description>GET /api/notifications/unread-count</description></item>
///         <item><description>PATCH /api/notifications/{id}/read</description></item>
///         <item><description>PATCH /api/notifications/read-all</description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/notifications")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("In-app notifications")]
public class NotificationsController(
    INotificationCommandService commandService,
    INotificationQueryService queryService,
    IPushNotificationProvider pushProvider) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation("List Notifications", "Returns the current user's notifications, newest first.")]
    public async Task<IActionResult> ListMine()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var notifications = await queryService.Handle(new GetNotificationsByUserIdQuery(current.Id));
        var resources = notifications
            .Select(NotificationResourceFromEntityAssembler.ToResourceFromEntity)
            .ToList();
        return Ok(resources);
    }

    [HttpGet("unread-count")]
    [SwaggerOperation("Unread Count", "Returns the number of unread notifications for the current user.")]
    public async Task<IActionResult> UnreadCount()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var count = await queryService.Handle(new GetUnreadCountByUserIdQuery(current.Id));
        return Ok(new UnreadCountResource(count));
    }

    [HttpPatch("{id:int}/read")]
    [SwaggerOperation("Mark Read", "Marks a single notification as read.")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var ok = await commandService.Handle(new MarkNotificationReadCommand(id, current.Id));
        if (!ok) return NotFound(new { error = "Notification not found" });
        return Ok(new { message = "Notification marked as read" });
    }

    [HttpPatch("read-all")]
    [SwaggerOperation("Mark All Read", "Marks all of the current user's notifications as read.")]
    public async Task<IActionResult> MarkAllRead()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        await commandService.Handle(new MarkAllNotificationsReadCommand(current.Id));
        return Ok(new { message = "All notifications marked as read" });
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation("Delete Notification", "Permanently deletes a notification for the current user.")]
    public async Task<IActionResult> Delete(int id)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var ok = await commandService.Handle(new DeleteNotificationCommand(id, current.Id));
        if (!ok) return NotFound(new { error = "Notification not found" });
        return Ok(new { message = "Notification deleted" });
    }

    [HttpPost("test-send")]
    [SwaggerOperation("Test Firebase Push Notification", "Sends a real-time push message to a specific Firebase device token.")]
    public async Task<IActionResult> TestSend([FromQuery] string token)
    {
        try
        {
            // Mapeado directamente al parámetro del constructor primario: pushProvider
            var messageId = await pushProvider.SendNotificationAsync(
                deviceToken: token,
                title: "¡Prueba de Conexión Exitosa!",
                body: "Si estás leyendo esto, tu backend de .NET y Firebase están perfectamente integrados."
            );

            return Ok(new { success = true, messageId, details = "Conexión con Google FCM exitosa." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
    }
}
