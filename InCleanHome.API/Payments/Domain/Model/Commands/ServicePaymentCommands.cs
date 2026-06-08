namespace InCleanHome.API.Payments.Domain.Model.Commands;

/// <summary>
/// Comandos del agregado ServicePayment.
///
/// PayBookingCommand: registra que el cliente pagó el booking por un canal manual
/// (yape / plin / bank_transfer / cash). NO se usa para izipay_card / paypal —
/// esos casos se disparan desde sus respectivos controllers de pasarela.
///
/// ConfirmIzipayPaymentCommand: registra que un pago Izipay sandbox fue exitoso.
/// Se llama internamente desde IzipayController.
///
/// ConfirmPayPalPaymentCommand: registra que un pago PayPal fue capturado.
/// Se llama internamente desde PayPalController tras capture-order exitoso.
///
/// RequestPayoutCommand: el worker pide el cobro de todos sus payments con
/// PayoutStatus = Pending. Marca esos payments como Completed.
/// </summary>
public record PayBookingCommand(int BookingId, int ClientId, string Channel);

public record ConfirmIzipayPaymentCommand(
    int BookingId, int ClientId, string IzipayOrderId, string? IzipayTransactionId);

public record ConfirmPayPalPaymentCommand(
    int BookingId, int ClientId, string PayPalOrderId, string? PayPalCaptureId);

public record RequestPayoutCommand(int WorkerId);
