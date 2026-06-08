using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.Reports.Domain.Model.Queries;
using InCleanHome.API.Reports.Domain.Repositories;
using InCleanHome.API.Reports.Domain.Services;

namespace InCleanHome.API.Reports.Application.Internal.QueryServices;

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

