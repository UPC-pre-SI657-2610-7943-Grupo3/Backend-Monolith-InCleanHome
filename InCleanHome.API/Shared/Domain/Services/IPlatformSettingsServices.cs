using InCleanHome.API.Shared.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Model.Commands;

namespace InCleanHome.API.Shared.Domain.Services;

public interface IPlatformSettingsCommandService
{
    /// <summary>Actualiza la tasa de comisión y refresca el caché del provider.</summary>
    Task<PlatformSettings> Handle(UpdateCommissionRateCommand command);
}

public interface IPlatformSettingsQueryService
{
    /// <summary>Devuelve el registro único (lo crea si no existe).</summary>
    Task<PlatformSettings> GetCurrent();
}
