using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Queries;
using InCleanHome.API.Payments.Domain.Model.ValueObjects;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.Payments.Domain.Services;

namespace InCleanHome.API.Payments.Application.Internal.QueryServices;

public class ServicePaymentQueryService(IServicePaymentRepository repository)
    : IServicePaymentQueryService
{
    public async Task<ServicePayment?> Handle(GetServicePaymentByBookingIdQuery query)
        => await repository.FindByBookingIdAsync(query.BookingId);

    public async Task<IEnumerable<ServicePayment>> Handle(GetServicePaymentsByWorkerIdQuery query)
        => await repository.FindByWorkerIdAsync(query.WorkerId);

    public async Task<WorkerBalanceResult> Handle(GetWorkerBalanceQuery query)
    {
        var payments = (await repository.FindByWorkerIdAsync(query.WorkerId)).ToList();

        var totalEarnings    = payments.Sum(p => p.WorkerEarning);
        var platformFeeTotal = payments.Sum(p => p.PlatformFee);
        var netEarnings      = totalEarnings; // alias para legibilidad en el frontend
        var pending          = payments.Where(p => p.PayoutStatus == PayoutStatus.Pending).ToList();
        var pendingPayout    = pending.Sum(p => p.WorkerEarning);

        return new WorkerBalanceResult(
            TotalEarnings:      totalEarnings,
            PlatformFeeTotal:   platformFeeTotal,
            NetEarnings:        netEarnings,
            PendingPayout:      pendingPayout,
            PendingPayoutCount: pending.Count,
            CompletedServices:  payments.Count);
    }
}
