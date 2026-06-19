namespace InCleanHome.API.Payments.Interfaces.REST.Resources;

// ── Request bodies ───────────────────────────────────────────────────────────

/// <summary>
///     Body de <c>POST /api/service-payments/booking/{id}/pay-manual</c>.
///     El channel debe ser <c>yape</c> | <c>plin</c> | <c>bank_transfer</c>.
///     Para pagos vía Mercado Pago, el cliente usa el flujo del adapter
///     (<c>POST /api/payments/mercadopago/preference</c>).
/// </summary>
public record PayBookingManualResource(string Channel);

// ── Response payloads ────────────────────────────────────────────────────────

public record ServicePaymentResource(
    int Id,
    int BookingId,
    int ClientId,
    int WorkerId,
    decimal Amount,
    decimal PlatformFee,
    decimal WorkerEarning,
    string Channel,
    string PayoutStatus,
    DateTimeOffset PaidAt,
    DateTimeOffset? PayoutCompletedAt,
    // Solo presente cuando Channel = mercadopago. Útil para mostrar en la boleta
    // y como referencia ante reclamos.
    string? MercadoPagoPaymentId);

public record WorkerBalanceResource(
    decimal TotalEarnings,
    decimal PlatformFeeTotal,
    decimal NetEarnings,
    decimal PendingPayout,
    int     PendingPayoutCount,
    int     CompletedServices);
