using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;

/// <summary>
///     Reclamo (apelación) de un usuario contra su propia suspensión.
/// </summary>
/// <remarks>
///     <para>
///     Cuando un admin suspende a un usuario (por reports confirmados, infracción
///     de reglas, etc.), el usuario suspendido puede apelar enviando un reclamo
///     con su versión de los hechos. Un admin lo revisa y decide aceptar
///     (levantar la suspensión inmediatamente) o rechazar (mantenerla con una
///     respuesta explicativa).
///     </para>
///     <para>
///     Vive en el bounded context <c>ReviewsAndEvaluation</c> porque
///     conceptualmente es un proceso de revisión humana, mismo tipo de objeto
///     que un Report — solo que su <i>reporter</i> es el propio usuario afectado.
///     </para>
///     <para>Estados:</para>
///     <list type="bullet">
///         <item><description><c>pending</c>: recién enviado, esperando revisión admin.</description></item>
///         <item><description><c>accepted</c>: admin lo aceptó. La suspensión se levantó.</description></item>
///         <item><description><c>rejected</c>: admin lo rechazó. La suspensión sigue.</description></item>
///     </list>
///     <para>
///     Solo se permite UN reclamo activo (pending) por suspensión. Si el reclamo
///     fue rechazado, el usuario puede enviar otro (raro, pero permitido).
///     </para>
/// </remarks>
public class SuspensionAppeal : IEntityWithCreatedUpdatedDate
{
    public const string StatusPending  = "pending";
    public const string StatusAccepted = "accepted";
    public const string StatusRejected = "rejected";

    public int Id { get; private set; }

    /// <summary>Usuario suspendido que apela.</summary>
    public int UserId { get; private set; }

    /// <summary>Razón redactada por el usuario explicando por qué considera injusta su suspensión.</summary>
    public string Reason { get; private set; } = string.Empty;

    public string Status { get; private set; } = StatusPending;

    /// <summary>Admin que revisó. Null hasta que alguien lo revise.</summary>
    public int? ReviewedByAdminUserId { get; private set; }

    public DateTimeOffset? ReviewedAt { get; private set; }

    /// <summary>Respuesta del admin al usuario (especialmente útil cuando se rechaza).</summary>
    public string AdminResponse { get; private set; } = string.Empty;

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public SuspensionAppeal() { }

    public SuspensionAppeal(int userId, string reason)
    {
        if (userId <= 0) throw new ArgumentException("Invalid userId");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("El motivo del reclamo no puede estar vacío.");

        UserId = userId;
        Reason = reason.Trim();
        Status = StatusPending;
    }

    /// <summary>Admin acepta el reclamo. Solo válido si está pending.</summary>
    public void Accept(int adminUserId, string response)
    {
        EnsurePending();
        ReviewedByAdminUserId = adminUserId;
        ReviewedAt            = DateTimeOffset.UtcNow;
        AdminResponse         = (response ?? string.Empty).Trim();
        Status                = StatusAccepted;
    }

    /// <summary>Admin rechaza el reclamo. Solo válido si está pending.</summary>
    public void Reject(int adminUserId, string response)
    {
        EnsurePending();
        ReviewedByAdminUserId = adminUserId;
        ReviewedAt            = DateTimeOffset.UtcNow;
        AdminResponse         = (response ?? string.Empty).Trim();
        Status                = StatusRejected;
    }

    private void EnsurePending()
    {
        if (Status != StatusPending)
            throw new InvalidOperationException(
                $"Solo se pueden revisar reclamos en estado '{StatusPending}'. Este está en '{Status}'.");
    }
}
