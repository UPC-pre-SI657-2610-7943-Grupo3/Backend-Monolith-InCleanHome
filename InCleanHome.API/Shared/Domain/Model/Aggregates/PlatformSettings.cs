using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.Shared.Domain.Model.Aggregates;

/// <summary>
///     Configuración global de la plataforma (single-record).
/// </summary>
/// <remarks>
///     <para>
///     Esta entidad usa el patrón <i>single-row table</i>: existe a lo sumo un
///     registro con <c>Id = 1</c>. El bootstrap del backend lo crea si no existe.
///     </para>
///     <para>
///     Hoy guarda únicamente la tasa de comisión que se cobra a las
///     trabajadoras por cada servicio pagado. Antes era una constante hardcoded
///     en el dominio (<c>0.10m</c>); haberla movido aquí permite al admin
///     cambiarla desde el UI sin recompilar.
///     </para>
///     <para>
///     <b>Importante</b>: si admin cambia la tasa hoy, los pagos ya registrados
///     NO se recalculan — cada <c>ServicePayment</c> guardó su <c>PlatformFee</c>
///     al momento del pago. Solo los pagos futuros usarán la nueva tasa. Eso es
///     intencional (auditabilidad y predictibilidad).
///     </para>
/// </remarks>
public class PlatformSettings : IEntityWithCreatedUpdatedDate
{
    public const int SingletonId = 1;

    /// <summary>Mínimo permitido (0% no tiene sentido para una plataforma).</summary>
    public const decimal MinCommissionRate = 0.00m;

    /// <summary>Máximo permitido (60% — defensa contra dedos torpes en el admin).</summary>
    public const decimal MaxCommissionRate = 0.60m;

    public int Id { get; private set; } = SingletonId;

    /// <summary>
    ///     Tasa decimal entre 0 y 1 (ej. 0.10 = 10%). Cobrar 10% por defecto.
    /// </summary>
    public decimal CommissionRate { get; private set; } = 0.10m;

    /// <summary>Última admin que tocó la configuración (auditoría).</summary>
    public int? LastUpdatedByAdminUserId { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public PlatformSettings() { }

    public PlatformSettings(decimal commissionRate)
    {
        EnsureValidRate(commissionRate);
        CommissionRate = commissionRate;
    }

    public void UpdateCommissionRate(decimal newRate, int adminUserId)
    {
        EnsureValidRate(newRate);
        CommissionRate           = newRate;
        LastUpdatedByAdminUserId = adminUserId;
    }

    private static void EnsureValidRate(decimal r)
    {
        if (r < MinCommissionRate || r > MaxCommissionRate)
            throw new InvalidOperationException(
                $"La tasa de comisión debe estar entre {MinCommissionRate * 100:F0}% y {MaxCommissionRate * 100:F0}%.");
    }
}
