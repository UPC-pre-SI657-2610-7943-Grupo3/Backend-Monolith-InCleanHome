using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Model.Queries;
using InCleanHome.API.Payments.Domain.Services;
using InCleanHome.API.Payments.Interfaces.REST.Resources;
using InCleanHome.API.Payments.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Payments.Interfaces.REST.Controllers;

/// <summary>
///     Off-platform payment-method registry consumed by the Vue frontend.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>GET /payments/methods</description></item>
///         <item><description>POST /payments/methods</description></item>
///         <item><description>PATCH /payments/methods/{id}/default</description></item>
///         <item><description>DELETE /payments/methods/{id}</description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/payments/methods")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Payment methods (off-platform agreements)")]
public class PaymentMethodsController(
    IPaymentMethodCommandService commandService,
    IPaymentMethodQueryService queryService) : ControllerBase
{
    [HttpGet]
    [SwaggerOperation("List My Payment Methods")]
    public async Task<IActionResult> ListMine()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var pms = await queryService.Handle(new GetPaymentMethodsByUserIdQuery(current.Id));
        return Ok(pms.Select(PaymentMethodResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>
    /// Devuelve los métodos de cobro registrados por una trabajadora específica.
    /// Lo usa el cliente al pagar el servicio para saber a qué Yape/Plin/cuenta
    /// debe transferirle. No es información sensible — sólo las etiquetas y los
    /// números de cuenta/celular que la trabajadora ya quiso exponer.
    /// </summary>
    [HttpGet("worker/{workerId:int}")]
    [SwaggerOperation("Get Worker Payment Methods",
        "Used by the client when paying a completed service — they need to know " +
        "where to send the money (Yape number, bank account, etc.).")]
    public async Task<IActionResult> GetByWorkerId(int workerId)
    {
        var pms = await queryService.Handle(new GetPaymentMethodsByUserIdQuery(workerId));
        return Ok(pms.Select(PaymentMethodResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost]
    [SwaggerOperation("Register Payment Method")]
    public async Task<IActionResult> Register([FromBody] RegisterPaymentMethodResource resource)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        try
        {
            var pm = await commandService.Handle(new RegisterPaymentMethodCommand(
                current.Id, resource.Type, resource.Label, resource.Details, resource.IsDefault));
            return Ok(PaymentMethodResourceFromEntityAssembler.ToResourceFromEntity(pm));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpPatch("{id:int}/default")]
    [SwaggerOperation("Set Default Payment Method")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var pm = await commandService.Handle(new SetDefaultPaymentMethodCommand(current.Id, id));
        if (pm is null) return NotFound();
        return Ok(PaymentMethodResourceFromEntityAssembler.ToResourceFromEntity(pm));
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation("Delete Payment Method")]
    public async Task<IActionResult> Delete(int id)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var ok = await commandService.Handle(new DeletePaymentMethodCommand(current.Id, id));
        if (!ok) return NotFound();
        return NoContent();
    }
}
