using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;
using InCleanHome.API.Payments.Domain.Model.ValueObjects;

namespace InCleanHome.API.Payments.Domain.Model.Aggregates;

/// <summary>
///     ServicePayment aggregate root — registra el pago de un servicio completado.
/// </summary>
/// <remarks>
///     <para>
///     Un único ServicePayment por <see cref="Booking.Domain.Model.Aggregates.BookingRequest"/>
///     (un servicio se paga una sola vez).
///     </para>
///     <para>Campos clave:</para>
///     <list type="bullet">
///         <item><description><c>Amount</c>: monto total del servicio (= BookingRequest.TotalAmount).</description></item>
///         <item><description><c>PlatformFee</c>: 10% del amount (la comisión es la misma para todos los canales).</description></item>
///         <item><description><c>WorkerEarning</c>: Amount - PlatformFee.</description></item>
///         <item><description><c>Channel</c>: mercadopago | yape | plin | bank_transfer.</description></item>
///         <item><description><c>PayoutStatus</c>: Pending al crearse; Completed al solicitar cobro la trabajadora.</description></item>
///         <item><description><c>MercadoPagoPaymentId</c> / <c>MercadoPagoPreferenceId</c>: solo cuando Channel = mercadopago.</description></item>
///     </list>
/// </remarks>
public class ServicePayment : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int BookingId { get; private set; }
    public int ClientId  { get; private set; }
    public int WorkerId  { get; private set; }

    public decimal Amount        { get; private set; }
    public decimal PlatformFee   { get; private set; }
    public decimal WorkerEarning { get; private set; }

    public string Channel      { get; private set; } = PaymentChannel.Yape;
    public string PayoutStatus { get; private set; } = ValueObjects.PayoutStatus.NotApplicable;

    public DateTimeOffset PaidAt { get; private set; }
    public DateTimeOffset? PayoutRequestedAt { get; private set; }
    public DateTimeOffset? PayoutCompletedAt { get; private set; }

    /// <summary>
    ///     ID del pago en Mercado Pago (devuelto por la API tras la transacción).
    ///     Solo presente cuando <c>Channel = mercadopago</c>. Lo persistimos para
    ///     consultar estado, mostrar en boleta y auditoría.
    /// </summary>
    public string? MercadoPagoPaymentId { get; private set; }

    /// <summary>
    ///     ID de la preferencia de Mercado Pago creada para iniciar el pago.
    ///     También se persiste para trazabilidad ante reclamos o investigación.
    /// </summary>
    public string? MercadoPagoPreferenceId { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public ServicePayment() { }

    /// <summary>
    ///     Crea un pago de servicio aplicando la tasa de comisión recibida.
    /// </summary>
    /// <remarks>
    ///     La tasa se inyecta como parámetro (no se hardcodea) porque vive en
    ///     el aggregate <c>PlatformSettings</c> y la lee el caller (command
    ///     service) usando <c>ICommissionRateProvider</c>. El valor efectivo
    ///     queda persistido implícitamente vía <c>PlatformFee</c>.
    /// </remarks>
    public ServicePayment(int bookingId, int clientId, int workerId, decimal amount,
        string channel, decimal commissionRate,
        string? mercadoPagoPaymentId = null,
        string? mercadoPagoPreferenceId = null)
    {
        if (!PaymentChannel.IsValid(channel))
            throw new ArgumentException($"Invalid payment channel: {channel}");
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");
        if (commissionRate < 0m || commissionRate > 1m)
            throw new ArgumentException("commissionRate must be between 0 and 1");

        BookingId = bookingId;
        ClientId  = clientId;
        WorkerId  = workerId;
        Amount    = amount;
        Channel   = channel;

        PlatformFee   = Math.Round(amount * commissionRate, 2);
        WorkerEarning = amount - PlatformFee;

        // Todos los canales arrancan Pending: la trabajadora luego solicita cobro
        // ("Solicitar cobro" desde /worker/payments). Para Mercado Pago esto libera
        // los fondos simulados; para canales manuales funciona como confirmación
        // de "el cliente ya me pagó por fuera".
        PayoutStatus = ValueObjects.PayoutStatus.Pending;

        PaidAt = DateTimeOffset.UtcNow;
        MercadoPagoPaymentId    = mercadoPagoPaymentId;
        MercadoPagoPreferenceId = mercadoPagoPreferenceId;
    }

    /// <summary>
    ///     El worker pidió cobrar este pago. Idempotente: si ya está completed o
    ///     not_applicable, no hace nada.
    /// </summary>
    public void MarkPayoutRequested()
    {
        if (PayoutStatus != ValueObjects.PayoutStatus.Pending)
            return;
        PayoutRequestedAt = DateTimeOffset.UtcNow;
        PayoutCompletedAt = DateTimeOffset.UtcNow; // simulación instantánea
        PayoutStatus      = ValueObjects.PayoutStatus.Completed;
    }
}
