namespace InCleanHome.API.Shared.Interfaces.REST.Resources;

/// <summary>
///     Vista pública de la configuración global.
/// </summary>
/// <param name="CommissionRate">Tasa decimal (ej. 0.10 para 10%).</param>
/// <param name="CommissionPercent">Mismo valor como entero por conveniencia (10).</param>
/// <param name="MinPercent">Mínimo permitido (0%).</param>
/// <param name="MaxPercent">Máximo permitido (60%).</param>
public record PlatformSettingsResource(
    decimal CommissionRate,
    decimal CommissionPercent,
    decimal MinPercent,
    decimal MaxPercent,
    int? LastUpdatedByAdminUserId,
    DateTimeOffset? UpdatedAt);

/// <summary>
///     Body del PUT que envía el admin.
/// </summary>
/// <remarks>
///     El admin manda el valor como entero (% 1–60) para evitar confusión con
///     decimales. El backend convierte a decimal (12 → 0.12).
/// </remarks>
public record UpdateCommissionRateResource(int CommissionPercent);
