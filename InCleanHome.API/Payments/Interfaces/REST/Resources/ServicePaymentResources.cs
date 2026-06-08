namespace InCleanHome.API.Payments.Interfaces.REST.Resources;

// ── Request bodies ───────────────────────────────────────────────────────────

/// <summary>
/// Body de POST /api/service-payments/booking/{id}/pay-manual.
/// El channel debe ser yape|plin|bank_transfer|cash (NO izipay_card —
/// ese va por el flujo Izipay).
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
    string? IzipayOrderId);

public record WorkerBalanceResource(
    decimal TotalEarnings,
    decimal PlatformFeeTotal,
    decimal NetEarnings,
    decimal PendingPayout,
    int     PendingPayoutCount,
    int     CompletedServices);
