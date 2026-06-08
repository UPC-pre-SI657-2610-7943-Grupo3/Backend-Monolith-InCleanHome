namespace InCleanHome.API.Reports.Domain.Model.Commands;

public record CreateReportCommand(int ReporterUserId, int ReportedUserId, string ReportedRole, string Reason, string? Details);
public record ConfirmReportCommand(int ReportId, int AdminUserId, string? AdminNotes);
public record DismissReportCommand(int ReportId, int AdminUserId, string? AdminNotes);
