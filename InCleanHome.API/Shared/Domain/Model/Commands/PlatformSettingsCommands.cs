namespace InCleanHome.API.Shared.Domain.Model.Commands;

/// <summary>Admin actualiza la tasa de comisión global.</summary>
/// <param name="NewRate">Decimal entre 0 y 1 (ej. 0.12 = 12%).</param>
/// <param name="AdminUserId">Admin que ejecuta el cambio (auditoría).</param>
public record UpdateCommissionRateCommand(decimal NewRate, int AdminUserId);
