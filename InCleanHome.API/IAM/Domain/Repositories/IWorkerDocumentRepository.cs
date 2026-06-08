using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.IAM.Domain.Repositories;

public interface IWorkerDocumentRepository : IBaseRepository<WorkerDocument>
{
    Task<IEnumerable<WorkerDocument>> FindByUserIdAsync(int userId);
}
