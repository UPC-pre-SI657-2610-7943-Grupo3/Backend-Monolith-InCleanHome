using InCleanHome.API.Shared.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Interfaces.REST.Resources;

namespace InCleanHome.API.Shared.Interfaces.REST.Transform;

public static class PlatformSettingsResourceFromEntityAssembler
{
    public static PlatformSettingsResource ToResource(PlatformSettings s) => new(
        CommissionRate:           s.CommissionRate,
        CommissionPercent:        Math.Round(s.CommissionRate * 100m, 2),
        MinPercent:               PlatformSettings.MinCommissionRate * 100m,
        MaxPercent:               PlatformSettings.MaxCommissionRate * 100m,
        LastUpdatedByAdminUserId: s.LastUpdatedByAdminUserId,
        UpdatedAt:                s.UpdatedDate);
}
