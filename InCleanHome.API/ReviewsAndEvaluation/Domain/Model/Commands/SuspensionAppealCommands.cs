namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;

/// <summary>Usuario suspendido envía su reclamo.</summary>
public record SubmitSuspensionAppealCommand(int UserId, string Reason);

/// <summary>Admin acepta el reclamo (levanta la suspensión).</summary>
public record AcceptSuspensionAppealCommand(int AppealId, int AdminUserId, string Response);

/// <summary>Admin rechaza el reclamo (mantiene la suspensión).</summary>
public record RejectSuspensionAppealCommand(int AppealId, int AdminUserId, string Response);
