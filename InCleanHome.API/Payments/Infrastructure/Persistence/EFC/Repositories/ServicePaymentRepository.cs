using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.ValueObjects;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Payments.Infrastructure.Persistence.EFC.Repositories;

public class ServicePaymentRepository(AppDbContext context)
    : BaseRepository<ServicePayment>(context), IServicePaymentRepository
{
    public async Task<ServicePayment?> FindByBookingIdAsync(int bookingId)
        => await Context.Set<ServicePayment>()
            .FirstOrDefaultAsync(p => p.BookingId == bookingId);

    public async Task<IEnumerable<ServicePayment>> FindByWorkerIdAsync(int workerId)
        => await Context.Set<ServicePayment>()
            .Where(p => p.WorkerId == workerId)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();

    public async Task<IEnumerable<ServicePayment>> FindPendingPayoutsByWorkerIdAsync(int workerId)
        => await Context.Set<ServicePayment>()
            .Where(p => p.WorkerId == workerId && p.PayoutStatus == PayoutStatus.Pending)
            .OrderByDescending(p => p.PaidAt)
            .ToListAsync();
}
