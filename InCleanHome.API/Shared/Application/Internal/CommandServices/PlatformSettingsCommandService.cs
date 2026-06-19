using InCleanHome.API.Shared.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Model.Commands;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Domain.Services;

namespace InCleanHome.API.Shared.Application.Internal.CommandServices;

public class PlatformSettingsCommandService(
    IPlatformSettingsRepository repository,
    IUnitOfWork unitOfWork) : IPlatformSettingsCommandService
{
    public async Task<PlatformSettings> Handle(UpdateCommissionRateCommand command)
    {
        var settings = await repository.GetOrCreateAsync();
        settings.UpdateCommissionRate(command.NewRate, command.AdminUserId);
        repository.Update(settings);
        await unitOfWork.CompleteAsync();
        // No hace falta invalidar caché: el provider lee directo de BD en cada
        // request, así que la tasa nueva se ve inmediatamente.
        return settings;
    }
}
