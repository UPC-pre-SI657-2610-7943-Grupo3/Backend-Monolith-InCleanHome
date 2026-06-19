using InCleanHome.API.Shared.Domain.Model.Aggregates;

namespace InCleanHome.API.Shared.Domain.Repositories;

/// <summary>
///     Repositorio del único registro <see cref="PlatformSettings"/>.
/// </summary>
public interface IPlatformSettingsRepository
{
    /// <summary>
    ///     Devuelve el registro único de configuración. Si no existe, lo crea
    ///     con valores por defecto. Es seguro llamarlo en cualquier momento.
    /// </summary>
    Task<PlatformSettings> GetOrCreateAsync();

    /// <summary>Marca la entidad como modificada para que el UnitOfWork la persista.</summary>
    void Update(PlatformSettings settings);
}
