using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.SearchAndCatalog.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.SearchAndCatalog.Infrastructure.Persistence.EFC.Repositories;

public class AvailabilitySlotRepository(AppDbContext context)
    : BaseRepository<AvailabilitySlot>(context), IAvailabilitySlotRepository
{
    public async Task<IEnumerable<AvailabilitySlot>> FindByWorkerUserIdAsync(int workerUserId)
        => await Context.Set<AvailabilitySlot>()
            .Where(a => a.WorkerUserId == workerUserId)
            .OrderBy(a => a.DayOfWeek)
            .ToListAsync();

    public async Task RemoveByWorkerUserIdAsync(int workerUserId)
    {
        var existing = Context.Set<AvailabilitySlot>().Where(a => a.WorkerUserId == workerUserId);
        Context.Set<AvailabilitySlot>().RemoveRange(existing);
        await Task.CompletedTask;
    }
}
