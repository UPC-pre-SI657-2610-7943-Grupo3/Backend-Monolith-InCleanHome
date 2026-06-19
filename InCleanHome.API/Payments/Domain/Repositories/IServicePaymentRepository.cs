using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Payments.Domain.Repositories;

public interface IServicePaymentRepository : IBaseRepository<ServicePayment>
{
    /// <summary>Devuelve el pago de un booking específico (null si aún no se pagó).</summary>
    Task<ServicePayment?> FindByBookingIdAsync(int bookingId);

    /// <summary>Todos los pagos hechos a una worker, ordenados desc por fecha.</summary>
    Task<IEnumerable<ServicePayment>> FindByWorkerIdAsync(int workerId);

    /// <summary>Solo los pagos en estado Pending (pendientes de cobro) de la worker.</summary>
    Task<IEnumerable<ServicePayment>> FindPendingPayoutsByWorkerIdAsync(int workerId);
}
