using InCleanHome.API.Reports.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Reports.Domain.Repositories;

public interface IReportRepository : IBaseRepository<Report>
{
    Task<IEnumerable<Report>> FindAllOrderedAsync();
    Task<IEnumerable<Report>> FindByReportedUserIdAsync(int reportedUserId);
    Task<IEnumerable<Report>> FindConfirmedByReportedUserIdAsync(int reportedUserId);
    Task<int> CountConfirmedByReportedUserIdAsync(int reportedUserId);
}
