using InCleanHome.API.Shared.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Domain.Services;

namespace InCleanHome.API.Shared.Application.Internal.QueryServices;

public class PlatformSettingsQueryService(IPlatformSettingsRepository repository)
    : IPlatformSettingsQueryService
{
    public Task<PlatformSettings> GetCurrent() => repository.GetOrCreateAsync();
}
