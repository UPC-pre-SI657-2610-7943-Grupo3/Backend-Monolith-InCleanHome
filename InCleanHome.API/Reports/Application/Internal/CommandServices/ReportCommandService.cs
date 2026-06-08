using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.Reports.Domain.Model.Commands;
using InCleanHome.API.Reports.Domain.Repositories;
using InCleanHome.API.Reports.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Reports.Application.Internal.CommandServices;

public class ReportCommandService(
    IReportRepository repository,
    INotificationsContextFacade notificationsFacade,
    IUnitOfWork unitOfWork) : IReportCommandService
{
    public async Task<Report> Handle(CreateReportCommand c)
    {
        var report = new Report(c.ReporterUserId, c.ReportedUserId, c.ReportedRole, c.Reason, c.Details);
        await repository.AddAsync(report);
        await unitOfWork.CompleteAsync();
        // Notify the reported user that they have a new report
        try {
            await notificationsFacade.CreateNotification(c.ReportedUserId, "report",
                "Cuenta reportada",
                "Tu cuenta ha recibido un reporte. El equipo de administración lo revisará pronto.",
                "/");
        } catch { }
        return report;
    }

    public async Task<Report> Handle(ConfirmReportCommand c)
    {
        var report = await repository.FindByIdAsync(c.ReportId)
                     ?? throw new Exception($"Report {c.ReportId} not found");
        report.Confirm(c.AdminUserId, c.AdminNotes);
        repository.Update(report);
        await unitOfWork.CompleteAsync();
        return report;
    }

    public async Task<Report> Handle(DismissReportCommand c)
    {
        var report = await repository.FindByIdAsync(c.ReportId)
                     ?? throw new Exception($"Report {c.ReportId} not found");
        report.Dismiss(c.AdminUserId, c.AdminNotes);
        repository.Update(report);
        await unitOfWork.CompleteAsync();
        return report;
    }
}

