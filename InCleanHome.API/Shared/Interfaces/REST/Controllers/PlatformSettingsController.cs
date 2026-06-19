using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.Shared.Domain.Model.Commands;
using InCleanHome.API.Shared.Domain.Services;
using InCleanHome.API.Shared.Interfaces.REST.Resources;
using InCleanHome.API.Shared.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Shared.Interfaces.REST.Controllers;

/// <summary>
///     Endpoints para que el admin lea y actualice la configuración global.
/// </summary>
[ApiController]
[Route("api/admin/settings")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Platform settings — configuración global (admin)")]
public class PlatformSettingsController(
    IPlatformSettingsCommandService commandService,
    IPlatformSettingsQueryService queryService) : ControllerBase
{
    /// <summary>Lee la configuración actual.</summary>
    [HttpGet]
    [SwaggerOperation("Get platform settings",
        "Devuelve la configuración global. Admin only.")]
    public async Task<IActionResult> Get()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Admin) return Forbid();

        var s = await queryService.GetCurrent();
        return Ok(PlatformSettingsResourceFromEntityAssembler.ToResource(s));
    }

    /// <summary>Actualiza la tasa de comisión.</summary>
    [HttpPut]
    [SwaggerOperation("Update commission rate",
        "Actualiza la tasa de comisión. Admin only.")]
    public async Task<IActionResult> Update([FromBody] UpdateCommissionRateResource body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Admin) return Forbid();

        try
        {
            // Convertimos % (entero) a decimal (0.xx). Validación final la
            // hace el aggregate al setear UpdateCommissionRate.
            var rate = body.CommissionPercent / 100m;
            var updated = await commandService.Handle(new UpdateCommissionRateCommand(rate, current.Id));
            return Ok(PlatformSettingsResourceFromEntityAssembler.ToResource(updated));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
