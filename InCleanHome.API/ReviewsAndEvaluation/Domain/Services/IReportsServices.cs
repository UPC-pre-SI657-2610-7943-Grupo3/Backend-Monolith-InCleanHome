using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Services;

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
