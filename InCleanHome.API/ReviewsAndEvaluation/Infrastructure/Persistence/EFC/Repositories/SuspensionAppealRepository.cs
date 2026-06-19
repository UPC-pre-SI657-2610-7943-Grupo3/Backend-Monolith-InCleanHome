using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.ReviewsAndEvaluation.Infrastructure.Persistence.EFC.Repositories;

public class SuspensionAppealRepository(AppDbContext context)
    : BaseRepository<SuspensionAppeal>(context), ISuspensionAppealRepository
{
    public async Task<SuspensionAppeal?> FindActiveByUserIdAsync(int userId)
        => await Context.Set<SuspensionAppeal>()
            .Where(a => a.UserId == userId && a.Status == SuspensionAppeal.StatusPending)
            .OrderByDescending(a => a.CreatedDate)
            .FirstOrDefaultAsync();

    public async Task<IEnumerable<SuspensionAppeal>> FindAllByUserIdAsync(int userId)
        => await Context.Set<SuspensionAppeal>()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<SuspensionAppeal>> FindPendingAsync()
        => await Context.Set<SuspensionAppeal>()
            .Where(a => a.Status == SuspensionAppeal.StatusPending)
            // FIFO: la más vieja primero para que admin atienda en orden.
            .OrderBy(a => a.CreatedDate)
            .ToListAsync();
}
