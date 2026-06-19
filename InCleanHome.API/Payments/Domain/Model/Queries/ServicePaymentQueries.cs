namespace InCleanHome.API.Payments.Domain.Model.Queries;

/// <summary>
/// Queries del agregado ServicePayment.
///
/// GetServicePaymentByBookingId: usado por el frontend para saber si un booking
///   ya está pagado (reemplaza la lógica de localStorage.inclean_payment_X).
///
/// GetWorkerBalance: devuelve las stats agregadas del worker — ganancias totales,
///   comisión cobrada por la plataforma, ganancias netas, pendiente de cobro
///   (pago pendiente de cobro por la trabajadora). Reemplaza la lógica de adjustedNetEarnings del front.
///
/// GetServicePaymentsByWorkerId: lista todos los payments del worker (para
///   detalle / historial de pagos).
/// </summary>
public record GetServicePaymentByBookingIdQuery(int BookingId);

public record GetWorkerBalanceQuery(int WorkerId);

public record GetServicePaymentsByWorkerIdQuery(int WorkerId);
