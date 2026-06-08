using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Profiles.Domain.Repositories;

public interface IWorkerProfileRepository : IBaseRepository<WorkerProfile>
{
    Task<WorkerProfile?> FindByUserIdAsync(int userId);
    Task<IEnumerable<WorkerProfile>> SearchAsync(SearchWorkersQuery filters);
}
