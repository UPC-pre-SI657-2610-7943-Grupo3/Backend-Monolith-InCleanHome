using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.Reports.Domain.Model.Aggregates;

/// <summary>
///     Report aggregate root — a user reports another profile (worker or client)
///     for review by the platform moderation team.
/// </summary>
public class Report : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int ReporterUserId { get; private set; }
    public int ReportedUserId { get; private set; }
    public string ReportedRole { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string Status { get; private set; } = "open";
    public int? ConfirmedByAdminUserId { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }
    public string AdminNotes { get; private set; } = string.Empty;

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public Report() { }

    public Report(int reporterUserId, int reportedUserId, string reportedRole, string reason, string? details)
    {
        if (reporterUserId == reportedUserId)
            throw new ArgumentException("You cannot report yourself.");
        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("A reason is required.");

        ReporterUserId = reporterUserId;
        ReportedUserId = reportedUserId;
        ReportedRole   = reportedRole;
        Reason         = reason;
        Details        = details ?? string.Empty;
        Status         = "open";
    }

    public Report Confirm(int adminUserId, string? adminNotes)
    {
        Status = "confirmed";
        ConfirmedByAdminUserId = adminUserId;
        ConfirmedAt = DateTimeOffset.UtcNow;
        AdminNotes = adminNotes ?? string.Empty;
        return this;
    }

    public Report Dismiss(int adminUserId, string? adminNotes)
    {
        Status = "dismissed";
        ConfirmedByAdminUserId = adminUserId;
        ConfirmedAt = DateTimeOffset.UtcNow;
        AdminNotes = adminNotes ?? string.Empty;
        return this;
    }
}

