using InCleanHome.API.Shared.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;

public class PlatformSettingsRepository(AppDbContext context) : IPlatformSettingsRepository
{
    public async Task<PlatformSettings> GetOrCreateAsync()
    {
        var existing = await context.Set<PlatformSettings>()
            .FirstOrDefaultAsync(s => s.Id == PlatformSettings.SingletonId);
        if (existing is not null) return existing;

        // Si no existe (BD recién creada, o el bootstrap no llegó a correr),
        // lo creamos al vuelo con los defaults del aggregate.
        var fresh = new PlatformSettings();
        await context.Set<PlatformSettings>().AddAsync(fresh);
        await context.SaveChangesAsync();
        return fresh;
    }

    public void Update(PlatformSettings settings) =>
        context.Set<PlatformSettings>().Update(settings);
}
