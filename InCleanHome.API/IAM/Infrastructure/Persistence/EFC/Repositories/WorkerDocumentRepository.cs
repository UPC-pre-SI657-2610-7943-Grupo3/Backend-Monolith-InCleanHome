using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.IAM.Infrastructure.Persistence.EFC.Repositories;

public class WorkerDocumentRepository(AppDbContext context)
    : BaseRepository<WorkerDocument>(context), IWorkerDocumentRepository
{
    public async Task<IEnumerable<WorkerDocument>> FindByUserIdAsync(int userId)
        => await Context.Set<WorkerDocument>().Where(d => d.UserId == userId).ToListAsync();
}
