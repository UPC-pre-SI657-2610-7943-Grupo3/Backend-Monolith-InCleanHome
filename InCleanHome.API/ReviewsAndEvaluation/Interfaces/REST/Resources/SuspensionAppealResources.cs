namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;

/// <summary>Body para que el usuario suspendido envíe su reclamo.</summary>
public record SubmitSuspensionAppealResource(string Reason);

/// <summary>Body que el admin envía al aceptar o rechazar un reclamo.</summary>
public record ReviewSuspensionAppealResource(string Response);

/// <summary>Vista pública del reclamo (usuario y admin la usan).</summary>
public record SuspensionAppealResource(
    int Id,
    int UserId,
    string Reason,
    string Status,
    int? ReviewedByAdminUserId,
    DateTimeOffset? ReviewedAt,
    string AdminResponse,
    DateTimeOffset? CreatedAt);
