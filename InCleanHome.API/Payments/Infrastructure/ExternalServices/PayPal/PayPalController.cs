using System.Net.Mime;
using InCleanHome.API.Booking.Domain.Model.ValueObjects;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.PayPal;

/// <summary>
/// Endpoints HTTP para PayPal (Orders API v2 con redirect flow).
///
///   GET  /api/payments/paypal/status                  — saber si PayPal está habilitado
///   POST /api/payments/paypal/create-order            — crea la orden y devuelve approveLink
///   POST /api/payments/paypal/capture-order           — captura el pago tras el redirect de PayPal
///
/// FLUJO:
///   1) Cliente click "Pagar Servicio" -> frontend hace POST /create-order { bookingId }.
///   2) Backend valida (booking existe, está completed, no está ya pagado) y llama
///      a PayPalService.CreateOrderAsync(monto del booking, USD, ...).
///   3) Backend devuelve { orderId, approveLink } al frontend.
///   4) Frontend hace window.location.href = approveLink.
///   5) Cliente completa el pago en sandbox.paypal.com.
///   6) PayPal redirige a {ReturnUrl}?token={orderId}&PayerID={payerId}.
///   7) Frontend (en PaymentSuccessView) extrae el token y llama a POST /capture-order
///      { bookingId, orderId }.
///   8) Backend llama a PayPalService.CaptureOrderAsync(orderId) y, si COMPLETED,
///      dispara ConfirmPayPalPaymentCommand -> crea el ServicePayment con
///      comisión 10% y payoutStatus=Pending.
///
/// Conversión cosmética: el backend envía el monto del booking tal cual a PayPal,
/// pero declarando currency=USD. Es decir, un booking de S/. 50 se cobra como
/// $50 USD en sandbox. En producción real habría que convertir con tipo de cambio.
/// </summary>
[ApiController]
[Route("api/payments/paypal")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("PayPal — pasarela de pagos con tarjeta o cuenta PayPal (redirect flow)")]
public class PayPalController(
    IPayPalService paypalService,
    IBookingRequestRepository bookingRepository,
    IServicePaymentCommandService servicePaymentCommandService) : ControllerBase
{
    public record CreateOrderRequest(int BookingId);
    public record CaptureOrderRequest(int BookingId, string OrderId);

    [HttpGet("status")]
    [AllowAnonymous]
    [SwaggerOperation("PayPal Status",
        "Devuelve si PayPal está habilitado (hay ClientId/ClientSecret configurados) " +
        "y el ClientId público que el frontend puede usar para mostrar el branding.")]
    public IActionResult Status()
    {
        return Ok(new
        {
            enabled     = paypalService.IsEnabled,
            environment = paypalService.Environment,
            clientId    = paypalService.IsEnabled ? paypalService.ClientIdPublic : null,
            currency    = paypalService.Currency
        });
    }

    [HttpPost("create-order")]
    [SwaggerOperation("Crear orden PayPal",
        "Crea una orden en PayPal Orders API v2 con el monto del booking. " +
        "El monto se toma del booking, no de la request, para que el cliente " +
        "no pueda manipular el total. Devuelve el approveLink al que el frontend " +
        "debe redirigir al usuario.")]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client)
            return StatusCode(403, new { error = "Only clients can initiate payment" });

        if (!paypalService.IsEnabled)
            return StatusCode(503, new { error = "PayPal is not configured" });

        var booking = await bookingRepository.FindByIdAsync(body.BookingId);
        if (booking is null) return NotFound(new { error = "Booking not found" });
        if (booking.ClientId != current.Id)
            return StatusCode(403, new { error = "This booking is not yours" });
        if (booking.Status != BookingStatus.Completed)
            return BadRequest(new { error = "Booking must be completed before payment" });

        // Reference único legible — útil para debugging en el dashboard de PayPal.
        var reference = $"ICH-B{body.BookingId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var description = $"Servicio de limpieza InCleanHome #{body.BookingId}";

        var result = await paypalService.CreateOrderAsync(
            booking.TotalAmount, paypalService.Currency, reference, description);

        if (!result.Success)
            return StatusCode(502, new { error = result.Error ?? "PayPal error" });

        return Ok(new
        {
            orderId     = result.OrderId,
            approveLink = result.ApproveLink,
            amount      = booking.TotalAmount,
            currency    = paypalService.Currency,
            bookingId   = body.BookingId
        });
    }

    [HttpPost("capture-order")]
    [SwaggerOperation("Capturar orden PayPal",
        "Tras el redirect de PayPal a /payment-success, el frontend llama a este " +
        "endpoint con el orderId. Acá disparamos el capture en PayPal y, si es " +
        "exitoso (status=COMPLETED), creamos el ServicePayment con channel=paypal, " +
        "comisión 10% y payoutStatus=Pending.")]
    public async Task<IActionResult> CaptureOrder([FromBody] CaptureOrderRequest body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client)
            return StatusCode(403, new { error = "Only clients can capture payments" });

        if (!paypalService.IsEnabled)
            return StatusCode(503, new { error = "PayPal is not configured" });

        if (string.IsNullOrWhiteSpace(body.OrderId))
            return BadRequest(new { error = "orderId is required" });

        var booking = await bookingRepository.FindByIdAsync(body.BookingId);
        if (booking is null) return NotFound(new { error = "Booking not found" });
        if (booking.ClientId != current.Id)
            return StatusCode(403, new { error = "This booking is not yours" });
        if (booking.Status != BookingStatus.Completed)
            return BadRequest(new { error = "Booking must be completed before payment" });

        var result = await paypalService.CaptureOrderAsync(body.OrderId);
        if (!result.Success || result.Status != "COMPLETED")
        {
            Console.WriteLine($"[PayPal] Capture not completed: status={result.Status} error={result.Error}");
            return StatusCode(502, new
            {
                error = result.Error ?? $"Capture status was {result.Status}",
                status = result.Status
            });
        }

        try
        {
            var payment = await servicePaymentCommandService.Handle(
                new ConfirmPayPalPaymentCommand(
                    body.BookingId, current.Id, body.OrderId, result.CaptureId));

            return Ok(new
            {
                orderId       = body.OrderId,
                captureId     = result.CaptureId,
                status        = "PAID",
                paymentId     = payment.Id,
                amount        = payment.Amount,
                workerEarning = payment.WorkerEarning,
                platformFee   = payment.PlatformFee
            });
        }
        catch (Exception e)
        {
            return BadRequest(new { error = e.Message });
        }
    }
}
