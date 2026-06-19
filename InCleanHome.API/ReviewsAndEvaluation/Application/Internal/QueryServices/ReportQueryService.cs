using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;

namespace InCleanHome.API.ReviewsAndEvaluation.Application.Internal.QueryServices;

public class ReportQueryService(IReportRepository repository) : IReportQueryService
{
    public async Task<IEnumerable<Report>> Handle(GetAllReportsQuery query)
        => await repository.FindAllOrderedAsync();

    public async Task<IEnumerable<Report>> Handle(GetReportsByReportedUserIdQuery query)
        => await repository.FindByReportedUserIdAsync(query.ReportedUserId);

    public async Task<IEnumerable<Report>> Handle(GetConfirmedReportsByReportedUserIdQuery query)
        => await repository.FindConfirmedByReportedUserIdAsync(query.ReportedUserId);

    public async Task<int> Handle(CountConfirmedReportsByReportedUserIdQuery query)
        => await repository.CountConfirmedByReportedUserIdAsync(query.ReportedUserId);
}

