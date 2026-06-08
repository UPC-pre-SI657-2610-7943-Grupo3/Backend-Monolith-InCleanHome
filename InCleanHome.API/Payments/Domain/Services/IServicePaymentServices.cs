using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Model.Queries;

namespace InCleanHome.API.Payments.Domain.Services;

public interface IServicePaymentCommandService
{
    /// <summary>Registra un pago manual (Yape/Plin/Bank/Cash).</summary>
    Task<ServicePayment> Handle(PayBookingCommand command);

    /// <summary>Registra un pago Izipay exitoso (lo llama IzipayController).</summary>
    Task<ServicePayment> Handle(ConfirmIzipayPaymentCommand command);

    /// <summary>Registra un pago PayPal exitoso (lo llama PayPalController tras capture).</summary>
    Task<ServicePayment> Handle(ConfirmPayPalPaymentCommand command);

    /// <summary>Marca todos los payments Pending del worker como Completed.</summary>
    Task<int> Handle(RequestPayoutCommand command);
}

public interface IServicePaymentQueryService
{
    Task<ServicePayment?> Handle(GetServicePaymentByBookingIdQuery query);
    Task<IEnumerable<ServicePayment>> Handle(GetServicePaymentsByWorkerIdQuery query);
    Task<WorkerBalanceResult> Handle(GetWorkerBalanceQuery query);
}

/// <summary>
/// Resultado consolidado del balance del worker. Se usa tanto para
/// /worker/payments (resumen de pagos) como para /worker/dashboard (las 3 tarjetas
/// de ganancias).
/// </summary>
public record WorkerBalanceResult(
    decimal TotalEarnings,     // Suma del WorkerEarning de TODOS los payments del worker (todos canales).
    decimal PlatformFeeTotal,  // Suma del PlatformFee.
    decimal NetEarnings,       // = TotalEarnings (las "netas" son lo que se le acreditó tras la comisión).
    decimal PendingPayout,     // Suma del WorkerEarning de payments con PayoutStatus = Pending (Izipay no cobrado).
    int     PendingPayoutCount,// Cantidad de payments Pending (para el "De N servicios pagados").
    int     CompletedServices  // Cantidad total de payments (servicios efectivamente pagados).
);
