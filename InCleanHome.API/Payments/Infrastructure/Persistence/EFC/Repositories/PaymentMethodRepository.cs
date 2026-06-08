using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Payments.Infrastructure.Persistence.EFC.Repositories;

public class PaymentMethodRepository(AppDbContext context)
    : BaseRepository<PaymentMethod>(context), IPaymentMethodRepository
{
    public async Task<IEnumerable<PaymentMethod>> FindByUserIdAsync(int userId)
        => await Context.Set<PaymentMethod>()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsDefault)
            .ThenByDescending(p => p.CreatedDate)
            .ToListAsync();

    public async Task<PaymentMethod?> FindDefaultByUserIdAsync(int userId)
        => await Context.Set<PaymentMethod>()
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsDefault);
}
