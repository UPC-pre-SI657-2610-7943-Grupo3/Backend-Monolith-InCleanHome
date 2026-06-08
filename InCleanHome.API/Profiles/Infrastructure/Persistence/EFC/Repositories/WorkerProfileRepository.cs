using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Profiles.Infrastructure.Persistence.EFC.Repositories;

public class WorkerProfileRepository(AppDbContext context)
    : BaseRepository<WorkerProfile>(context), IWorkerProfileRepository
{
    public async Task<WorkerProfile?> FindByUserIdAsync(int userId)
        => await Context.Set<WorkerProfile>().FirstOrDefaultAsync(w => w.UserId == userId);

    public async Task<IEnumerable<WorkerProfile>> SearchAsync(SearchWorkersQuery f)
    {
        var query = Context.Set<WorkerProfile>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(f.ServiceType))
            query = query.Where(w => w.ServiceTypes.Contains(f.ServiceType));

        if (!string.IsNullOrWhiteSpace(f.Zone))
            query = query.Where(w => w.Zones.Contains(f.Zone));

        if (!string.IsNullOrWhiteSpace(f.Gender))
            query = query.Where(w => w.Gender == f.Gender);

        if (f.MinAge.HasValue)         query = query.Where(w => w.Age >= f.MinAge.Value);
        if (f.MaxAge.HasValue)         query = query.Where(w => w.Age <= f.MaxAge.Value);
        if (f.MaxHourlyRate.HasValue)  query = query.Where(w => w.HourlyRate <= f.MaxHourlyRate.Value);
        if (f.MinRating.HasValue)      query = query.Where(w => w.AverageRating >= f.MinRating.Value);

        return await query.OrderByDescending(w => w.AverageRating).ToListAsync();
    }
}
