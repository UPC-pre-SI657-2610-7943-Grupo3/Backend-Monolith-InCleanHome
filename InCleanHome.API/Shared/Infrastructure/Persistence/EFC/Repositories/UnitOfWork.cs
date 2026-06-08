using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;

namespace InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync() => await context.SaveChangesAsync();
}
