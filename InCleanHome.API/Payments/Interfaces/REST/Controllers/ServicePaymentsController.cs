using System.Net.Mime;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Model.Queries;
using InCleanHome.API.Payments.Domain.Services;
using InCleanHome.API.Payments.Interfaces.REST.Resources;
using InCleanHome.API.Payments.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Payments.Interfaces.REST.Controllers;

/// <summary>
///     Endpoints del agregado ServicePayment.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>POST /api/service-payments/booking/{id}/pay-manual — pago manual (Yape/Plin/Bank/Cash)</description></item>
///         <item><description>GET  /api/service-payments/booking/{id} — saber si un booking está pagado</description></item>
///         <item><description>GET  /api/service-payments/worker/balance — stats del worker logueado</description></item>
///         <item><description>GET  /api/service-payments/worker — historial de pagos del worker</description></item>
///         <item><description>POST /api/service-payments/worker/request-payout — worker solicita cobro de pagos pendientes</description></item>
///     </list>
///     El pago con tarjeta NO pasa por este controller — pasa por
///     /api/payments/mercadopago/confirm que internamente crea el
///     ServicePayment vía ConfirmMercadoPagoPaymentCommand.
/// </remarks>
[ApiController]
[Route("api/service-payments")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Service Payments — pagos realizados de servicios completados")]
public class ServicePaymentsController(
    IServicePaymentCommandService commandService,
    IServicePaymentQueryService queryService) : ControllerBase
{
    [HttpPost("booking/{bookingId:int}/pay-manual")]
    [SwaggerOperation("Pay Booking (Manual Channel)",
        "Cliente registra el pago manual de un servicio completado: Yape/Plin/" +
        "Bank. Para Mercado Pago, usar /api/payments/mercadopago/* en su lugar (no por aquí en su lugar.")]
    public async Task<IActionResult> PayManual(int bookingId, [FromBody] PayBookingManualResource body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client)
            return StatusCode(403, new { error = "Only clients can pay bookings" });

        try
        {
            var payment = await commandService.Handle(
                new PayBookingCommand(bookingId, current.Id, body.Channel));
            return Ok(ServicePaymentResourceFromEntityAssembler.ToResourceFromEntity(payment));
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }

    [HttpGet("booking/{bookingId:int}")]
    [SwaggerOperation("Get Booking Payment",
        "Devuelve el pago de un booking, o 404 si aún no se ha pagado. " +
        "Usado por el frontend para mostrar 'Pagado' vs 'Esperando cobro'.")]
    public async Task<IActionResult> GetByBooking(int bookingId)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();

        var p = await queryService.Handle(new GetServicePaymentByBookingIdQuery(bookingId));
        if (p is null) return NotFound();

        // Solo el cliente o la worker involucrados pueden consultar este pago.
        if (p.ClientId != current.Id && p.WorkerId != current.Id && current.Role != UserRole.Admin)
            return StatusCode(403, new { error = "Forbidden" });

        return Ok(ServicePaymentResourceFromEntityAssembler.ToResourceFromEntity(p));
    }

    [HttpGet("worker/balance")]
    [SwaggerOperation("Get Worker Balance",
        "Stats agregadas del worker logueado: ganancias totales, comisión, " +
        "pendiente de cobro. Reemplaza el cálculo en frontend basado en localStorage.")]
    public async Task<IActionResult> GetMyBalance()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker)
            return StatusCode(403, new { error = "Only workers have a balance" });

        var balance = await queryService.Handle(new GetWorkerBalanceQuery(current.Id));
        return Ok(WorkerBalanceResourceFromResultAssembler.ToResource(balance));
    }

    [HttpGet("worker")]
    [SwaggerOperation("Get My Service Payments",
        "Lista todos los pagos recibidos por el worker logueado (orden desc por fecha).")]
    public async Task<IActionResult> ListMine()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker)
            return StatusCode(403, new { error = "Workers only" });

        var payments = await queryService.Handle(new GetServicePaymentsByWorkerIdQuery(current.Id));
        return Ok(payments.Select(ServicePaymentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost("worker/request-payout")]
    [SwaggerOperation("Request Payout",
        "Worker solicita el cobro de TODOS sus pagos pendientes. En esta " +
        "simulación el payout es instantáneo: marca todos como Completed sin " +
        "transferir dinero real.")]
    public async Task<IActionResult> RequestPayout()
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Worker)
            return StatusCode(403, new { error = "Workers only" });

        try
        {
            var count = await commandService.Handle(new RequestPayoutCommand(current.Id));
            return Ok(new { payoutsProcessed = count });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
