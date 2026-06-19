using System.Net.Mime;
using InCleanHome.API.Booking.Domain.Model.ValueObjects;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Services;
using InCleanHome.API.Payments.Domain.Services.External;
using InCleanHome.API.Profiles.Interfaces.ACL;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Annotations;

namespace InCleanHome.API.Payments.Infrastructure.ExternalServices.MercadoPago;

/// <summary>
///     Endpoints HTTP que expone la integración con Mercado Pago Perú.
/// </summary>
/// <remarks>
///     <para>Endpoints expuestos:</para>
///     <list type="bullet">
///         <item><description>
///             <c>GET  /api/payments/mercadopago/public-key</c> — devuelve el Public Key
///             para que el frontend inicialice Bricks. Nunca devuelve el Access Token.
///         </description></item>
///         <item><description>
///             <c>POST /api/payments/mercadopago/preference</c> — crea una preferencia
///             de pago para un booking completado (auth). Devuelve <c>checkoutUrl</c>.
///         </description></item>
///         <item><description>
///             <c>POST /api/payments/mercadopago/confirm</c> — confirma un pago tras el
///             redirect. Consulta el estado por API y persiste un ServicePayment.
///         </description></item>
///         <item><description>
///             <c>GET  /api/payments/mercadopago/status</c> — diagnóstico: muestra si
///             las credenciales están configuradas (sin exponer secretos).
///         </description></item>
///     </list>
/// </remarks>
[ApiController]
[Route("api/payments/mercadopago")]
[Produces(MediaTypeNames.Application.Json)]
[SwaggerTag("Mercado Pago Perú — pasarela de pagos")]
public class MercadoPagoController(
    IPaymentGatewayProvider gateway,
    IOptions<MercadoPagoSettings> settings,
    IBookingRequestRepository bookingRepository,
    IServicePaymentCommandService paymentCommandService,
    IServicePaymentRepository paymentRepository,
    IProfilesContextFacade profilesFacade) : ControllerBase
{
    public record CreatePreferenceRequest(int BookingId);
    public record CreatePreferenceResponse(string PreferenceId, string CheckoutUrl, string PublicKey);

    public record ConfirmRequest(int BookingId, string PaymentId, string? PreferenceId);
    public record ConfirmResponse(int ServicePaymentId, decimal Amount, string Status);

    [HttpGet("status")]
    [SwaggerOperation("Mercado Pago Status",
        "Devuelve si Mercado Pago está habilitado (credenciales configuradas).")]
    public IActionResult Status() => Ok(new
    {
        enabled = settings.Value.IsEnabled,
        sandbox = settings.Value.AccessToken.StartsWith("TEST-", StringComparison.OrdinalIgnoreCase),
    });

    [HttpGet("public-key")]
    [SwaggerOperation("Mercado Pago Public Key",
        "Devuelve la Public Key para inicializar el SDK Bricks en el frontend.")]
    public IActionResult PublicKey()
    {
        var key = gateway.GetPublicKey();
        if (string.IsNullOrEmpty(key))
            return StatusCode(503, new { error = "Mercado Pago no está configurado." });
        return Ok(new { publicKey = key });
    }

    /// <summary>
    ///     Crea una preferencia de pago para un booking del cliente autenticado.
    ///     El cliente luego es redirigido a <c>checkoutUrl</c> donde paga en MP.
    /// </summary>
    [HttpPost("preference")]
    [SwaggerOperation("Create Preference",
        "Crea una preferencia de pago en Mercado Pago para el booking dado.")]
    public async Task<IActionResult> CreatePreference([FromBody] CreatePreferenceRequest body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client) return Forbid();

        var booking = await bookingRepository.FindByIdAsync(body.BookingId);
        if (booking is null) return NotFound(new { error = "Booking not found" });
        if (booking.ClientId != current.Id) return Forbid();
        if (booking.Status != BookingStatus.Completed)
            return BadRequest(new { error = "Booking must be completed before payment" });

        // Email del cliente para pre-rellenar en MP. Si no se puede obtener,
        // dejamos un placeholder válido (MP lo ignora si el cliente lo cambia).
        var clientEmail = await profilesFacade.FetchUserEmailByUserId(current.Id)
                          ?? "cliente@incleanhome.com";

        var basePath = settings.Value.FrontendBaseUrl.TrimEnd('/');
        try
        {
            var result = await gateway.CreatePaymentIntentAsync(new CreatePaymentIntentRequest(
                BookingId:   booking.Id,
                Amount:      booking.TotalAmount,
                Description: $"InCleanHome — Servicio #{booking.Id}",
                PayerEmail:  clientEmail,
                SuccessUrl:  $"{basePath}/payment-success?bookingId={booking.Id}",
                FailureUrl:  $"{basePath}/payment-failure?bookingId={booking.Id}",
                PendingUrl:  $"{basePath}/payment-success?bookingId={booking.Id}&pending=1"));

            return Ok(new CreatePreferenceResponse(result.PreferenceId, result.CheckoutUrl, gateway.GetPublicKey()));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Tras el redirect de retorno desde MP, el frontend llama a este endpoint
    ///     con el <c>payment_id</c> que MP agregó al query string. El backend
    ///     consulta el estado real por API y, si está aprobado, persiste el pago.
    /// </summary>
    [HttpPost("confirm")]
    [SwaggerOperation("Confirm MP Payment",
        "Verifica el estado del pago en MP y registra el ServicePayment si está aprobado.")]
    public async Task<IActionResult> Confirm([FromBody] ConfirmRequest body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client) return Forbid();

        if (string.IsNullOrWhiteSpace(body.PaymentId))
            return BadRequest(new { error = "PaymentId is required" });

        // Consulta autoritativa al gateway: NO confiamos en parámetros del query
        // string (cliente podría manipularlos). Solo el adapter sabe la verdad.
        PaymentStatusResult status;
        try { status = await gateway.GetPaymentStatusAsync(body.PaymentId); }
        catch (InvalidOperationException ex) { return BadRequest(new { error = ex.Message }); }

        if (status.Status != "approved")
            return BadRequest(new { error = $"El pago no está aprobado (estado: {status.ProviderRawStatus})." });

        try
        {
            var payment = await paymentCommandService.Handle(new ConfirmMercadoPagoPaymentCommand(
                BookingId:               body.BookingId,
                ClientId:                current.Id,
                MercadoPagoPaymentId:    status.PaymentId,
                MercadoPagoPreferenceId: body.PreferenceId));

            return Ok(new ConfirmResponse(payment.Id, payment.Amount, "approved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    ///     Verifica si hay un pago aprobado en MP para un booking dado, sin
    ///     necesidad del payment_id (lo busca por external_reference). Lo usa
    ///     el flujo "Ya pagué, verificar" en localhost.
    /// </summary>
    public record ConfirmByBookingRequest(int BookingId);

    [HttpPost("confirm-by-booking")]
    [SwaggerOperation("Confirm MP Payment by Booking",
        "Busca en MP un pago aprobado cuyo external_reference sea el bookingId. " +
        "Si lo encuentra, registra el ServicePayment. Útil en localhost cuando MP no redirige automáticamente.")]
    public async Task<IActionResult> ConfirmByBooking([FromBody] ConfirmByBookingRequest body)
    {
        var current = (User?)HttpContext.Items["User"];
        if (current is null) return Unauthorized();
        if (current.Role != UserRole.Client) return Forbid();

        // Si ya está pagado, devolvemos éxito (idempotencia).
        var existing = await paymentRepository.FindByBookingIdAsync(body.BookingId);
        if (existing is not null)
        {
            return Ok(new ConfirmResponse(existing.Id, existing.Amount, "approved"));
        }

        // Buscamos en MP por external_reference.
        var status = await gateway.FindApprovedPaymentByExternalReferenceAsync(body.BookingId.ToString());
        if (status is null)
        {
            // Todavía no hay pago aprobado. El frontend mostrará "Aún no detectamos el pago".
            return NotFound(new { error = "Todavía no se detecta un pago aprobado para esta reserva en Mercado Pago." });
        }

        try
        {
            var payment = await paymentCommandService.Handle(new ConfirmMercadoPagoPaymentCommand(
                BookingId:               body.BookingId,
                ClientId:                current.Id,
                MercadoPagoPaymentId:    status.PaymentId,
                MercadoPagoPreferenceId: null));

            return Ok(new ConfirmResponse(payment.Id, payment.Amount, "approved"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
