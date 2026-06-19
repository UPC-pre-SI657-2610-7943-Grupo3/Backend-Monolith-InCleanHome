namespace InCleanHome.API.Payments.Domain.Model.Commands;

/// <summary>
///     Comandos del agregado ServicePayment.
/// </summary>
/// <remarks>
///     <para>
///     <c>PayBookingCommand</c>: registra que el cliente pagó el booking por un
///     canal manual (yape / plin / bank_transfer). El cliente confirma en la app
///     que ya pagó por fuera y la trabajadora luego pide cobro.
///     </para>
///     <para>
///     <c>ConfirmMercadoPagoPaymentCommand</c>: registra que un pago vía
///     Mercado Pago Perú fue aprobado. Se llama internamente desde el controller
///     del adapter tras consultar el estado del payment_id devuelto por MP.
///     </para>
///     <para>
///     <c>RequestPayoutCommand</c>: la worker pide el cobro de todos sus
///     payments con <c>PayoutStatus = Pending</c>; los marca como Completed.
///     </para>
/// </remarks>
public record PayBookingCommand(int BookingId, int ClientId, string Channel);

public record ConfirmMercadoPagoPaymentCommand(
    int BookingId,
    int ClientId,
    string MercadoPagoPaymentId,
    string? MercadoPagoPreferenceId);

public record RequestPayoutCommand(int WorkerId);
