using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.REST.Resources;
using InCleanHome.API.Profiles.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Profiles.Interfaces.REST.Controllers;

/// <summary>
///     Profile endpoints for clients and workers.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>GET /api/my-profile — get current user's profile</description></item>
///         <item><description>PATCH /api/my-profile — update current user's profile</description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/my-profile")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Profiles — user profile management")]
public class ProfilesController(
    IClientProfileCommandService clientCommandService,
    IClientProfileQueryService clientQueryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation("Get My Client Profile", "Returns the current user's profile (client).")]
    public async Task<IActionResult> GetMyProfile()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        if (current.Role == UserRole.Client)
        {
            var profile = await clientQueryService.Handle(new GetClientProfileByUserIdQuery(current.Id));
            if (profile is null) return NotFound(new { error = "Profile not found" });
            return Ok(ClientResourceFromEntityAssembler.ToResourceFromEntity(profile));
        }

        return BadRequest(new { error = "Unsupported role" });
    }

    [HttpPatch]
    [SwaggerOperation("Update My Client Profile", "Updates the current client's profile (name and phone).")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateClientProfileResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        if (current.Role == UserRole.Client)
        {
            try
            {
                var profile = await clientCommandService.Handle(
                    new UpdateClientProfileCommand(current.Id, resource.Name, resource.Phone));
                if (profile is null) return NotFound(new { error = "Profile not found" });
                return Ok(ClientResourceFromEntityAssembler.ToResourceFromEntity(profile));
            }
            catch (Exception e)
            {
                return BadRequest(new { error = e.Message });
            }
        }

        return BadRequest(new { error = "Unsupported role" });
    }

    [HttpPost("photo")]
    [SwaggerOperation("Update My Client Photo", "Sets the current client's profile photo (base64 data URL).")]
    public async Task<IActionResult> UpdateMyPhoto([FromBody] UpdatePhotoResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        if (current.Role == UserRole.Client)
        {
            var profile = await clientCommandService.Handle(
                new UpdateClientPhotoCommand(current.Id, resource.PhotoUrl));
            if (profile is null) return NotFound(new { error = "Profile not found" });
            return Ok(ClientResourceFromEntityAssembler.ToResourceFromEntity(profile));
        }

        return BadRequest(new { error = "Unsupported role" });
    }

    // ── Perfil público de cliente por ID ──────────────────────────────────────
    // Usado por el chat de Twilio: cuando una trabajadora abre un chat con un
    // cliente, el frontend necesita resolver el nombre/foto del cliente por su
    // userId. Se usa "~/" para escapar del [Route("api/my-profile")] de la clase.
    [HttpGet("~/api/clients/{userId:int}")]
    [SwaggerOperation("Get Client Profile By Id", "Returns a client's public profile by their user ID.")]
    public async Task<IActionResult> GetClientById(int userId)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var profile = await clientQueryService.Handle(new GetClientProfileByUserIdQuery(userId));
        if (profile is null) return NotFound(new { error = "Client not found" });

        return Ok(ClientResourceFromEntityAssembler.ToResourceFromEntity(profile));
    }
}

