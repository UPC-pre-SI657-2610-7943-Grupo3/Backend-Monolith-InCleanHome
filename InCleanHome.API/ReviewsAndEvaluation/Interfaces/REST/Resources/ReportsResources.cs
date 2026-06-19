namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;

public record CreateReportResource(int ReportedUserId, string ReportedRole, string Reason, string? Details);
public record ModerateReportResource(string? AdminNotes);

public record ReportResource(
    int Id,
    int ReporterUserId,
    int ReportedUserId,
    string ReportedRole,
    string Reason,
    string Details,
    string Status,
    DateTimeOffset? CreatedAt,
    int? ConfirmedByAdminUserId,
    DateTimeOffset? ConfirmedAt,
    string AdminNotes);
