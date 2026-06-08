using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.Reports.Domain.Model.Commands;
using InCleanHome.API.Reports.Domain.Model.Queries;

namespace InCleanHome.API.Reports.Domain.Services;

public interface IReportCommandService
{
    Task<Report> Handle(CreateReportCommand command);
    Task<Report> Handle(ConfirmReportCommand command);
    Task<Report> Handle(DismissReportCommand command);
}

public interface IReportQueryService
{
    Task<IEnumerable<Report>> Handle(GetAllReportsQuery query);
    Task<IEnumerable<Report>> Handle(GetReportsByReportedUserIdQuery query);
    Task<IEnumerable<Report>> Handle(GetConfirmedReportsByReportedUserIdQuery query);
    Task<int> Handle(CountConfirmedReportsByReportedUserIdQuery query);
}
