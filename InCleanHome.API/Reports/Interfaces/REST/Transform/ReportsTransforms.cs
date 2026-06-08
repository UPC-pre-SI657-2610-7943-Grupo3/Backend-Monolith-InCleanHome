using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.Reports.Interfaces.REST.Resources;

namespace InCleanHome.API.Reports.Interfaces.REST.Transform;

public static class ReportResourceFromEntityAssembler
{
    public static ReportResource ToResourceFromEntity(Report r)
        => new(r.Id, r.ReporterUserId, r.ReportedUserId, r.ReportedRole, r.Reason, r.Details, r.Status, r.CreatedDate, r.ConfirmedByAdminUserId, r.ConfirmedAt, r.AdminNotes);
}
