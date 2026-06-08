using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;
using InCleanHome.API.Payments.Domain.Model.ValueObjects;

namespace InCleanHome.API.Payments.Domain.Model.Aggregates;

/// <summary>
///     ServicePayment aggregate root — registra el pago de un servicio completado.
/// </summary>
/// <remarks>
///     A diferencia de <see cref="PaymentMethod"/> (que solo registra qué método
///     usaría el cliente), <see cref="ServicePayment"/> registra que el pago
///     EFECTIVAMENTE OCURRIÓ. Hay un único ServicePayment por BookingRequest
///     (asumimos que un servicio solo se paga una vez).
///
///     <para>Modelo de datos:</para>
///     <list type="bullet">
///         <item><description><c>Amount</c>: monto total del servicio (= BookingRequest.TotalAmount).</description></item>
///         <item><description><c>PlatformFee</c>: 10% del amount, excepto para canal <c>cash</c>.</description></item>
///         <item><description><c>WorkerEarning</c>: lo que le corresponde al worker (Amount - PlatformFee).</description></item>
///         <item><description><c>Channel</c>: canal usado (izipay_card | yape | plin | bank_transfer | cash).</description></item>
///         <item><description><c>PayoutStatus</c>: solo relevante para izipay_card (el resto = NotApplicable).</description></item>
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

    public string Channel      { get; private set; } = PaymentChannel.Cash;
    public string PayoutStatus { get; private set; } = ValueObjects.PayoutStatus.NotApplicable;

    public DateTimeOffset PaidAt { get; private set; }
    public DateTimeOffset? PayoutRequestedAt { get; private set; }
    public DateTimeOffset? PayoutCompletedAt { get; private set; }

    // Solo aplica a izipay_card. Lo guardamos para el comprobante.
    public string? IzipayOrderId { get; private set; }
    public string? IzipayTransactionId { get; private set; }

    // Solo aplica a paypal. Lo guardamos para el comprobante / auditoría.
    public string? PayPalOrderId { get; private set; }
    public string? PayPalCaptureId { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public ServicePayment() { }

    /// <summary>
    /// Crea un pago de servicio. La comisión se calcula automáticamente según
    /// el canal: cash = 0, todo lo demás = 10%.
    /// </summary>
    public ServicePayment(int bookingId, int clientId, int workerId, decimal amount,
        string channel,
        string? izipayOrderId = null, string? izipayTransactionId = null,
        string? paypalOrderId = null, string? paypalCaptureId = null)
    {
        if (!PaymentChannel.IsValid(channel))
            throw new ArgumentException($"Invalid payment channel: {channel}");
        if (amount <= 0)
            throw new ArgumentException("Amount must be positive");

        BookingId = bookingId;
        ClientId  = clientId;
        WorkerId  = workerId;
        Amount    = amount;
        Channel   = channel;

        // Comisión 10% para todos los canales excepto cash.
        if (PaymentChannel.ChargesPlatformFee(channel))
        {
            PlatformFee   = Math.Round(amount * 0.10m, 2);
            WorkerEarning = amount - PlatformFee;
        }
        else
        {
            PlatformFee   = 0m;
            WorkerEarning = amount;
        }

        // TODOS los canales entran como "Pending" para que la trabajadora pueda
        // presionar "Solicitar cobro" desde /worker/payments, sin importar si el
        // pago fue por pasarela (Izipay/PayPal) o manual (Yape, Plin, transferencia,
        // efectivo). Para los canales manuales el botón funciona como confirmación
        // de recepción ("el cliente ya me pagó por fuera"); para los canales con
        // pasarela funciona como liberación de fondos en la simulación.
        PayoutStatus = ValueObjects.PayoutStatus.Pending;

        PaidAt = DateTimeOffset.UtcNow;
        IzipayOrderId = izipayOrderId;
        IzipayTransactionId = izipayTransactionId;
        PayPalOrderId = paypalOrderId;
        PayPalCaptureId = paypalCaptureId;
    }

    /// <summary>
    /// El worker pidió cobrar este pago (solo aplica a canales con pasarela).
    /// </summary>
    public void MarkPayoutRequested()
    {
        if (PayoutStatus != ValueObjects.PayoutStatus.Pending)
            return; // idempotente: si ya está completed o not_applicable, no hace nada
        PayoutRequestedAt = DateTimeOffset.UtcNow;
        PayoutCompletedAt = DateTimeOffset.UtcNow; // en simulación es instantáneo
        PayoutStatus      = ValueObjects.PayoutStatus.Completed;
    }
}
