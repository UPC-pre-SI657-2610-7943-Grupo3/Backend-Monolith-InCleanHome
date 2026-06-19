using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Shared.Domain.Services;

/// <summary>
///     Proveedor de la tasa de comisión actual.
/// </summary>
/// <remarks>
///     <para>
///     Se consulta en cada creación de <c>ServicePayment</c> y de
///     <c>BookingRequest</c>. Hace una lectura simple a BD por cada llamada —
///     el volumen esperado del proyecto (decenas de pagos por día como mucho)
///     no justifica añadir una capa de caché. Si en el futuro el sistema
///     escala a miles de tx/s, se puede envolver con <c>IMemoryCache</c> con
///     TTL sin tocar a los callers.
///     </para>
/// </remarks>
public interface ICommissionRateProvider
{
    /// <summary>Devuelve la tasa actual (ej. 0.10 para 10%).</summary>
    Task<decimal> GetCurrentRateAsync();
}

/// <summary>
///     Implementación simple: lee directamente del repositorio de
///     <c>PlatformSettings</c>, sin caché ni invalidación. Si el admin cambió
///     la tasa hace un segundo, el siguiente pago la ve inmediatamente.
/// </summary>
public class CommissionRateProvider(IPlatformSettingsRepository repository) : ICommissionRateProvider
{
    public async Task<decimal> GetCurrentRateAsync()
    {
        var settings = await repository.GetOrCreateAsync();
        return settings.CommissionRate;
    }
}
