using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.SearchAndCatalog.Domain.Repositories;

public interface IAvailabilitySlotRepository : IBaseRepository<AvailabilitySlot>
{
    Task<IEnumerable<AvailabilitySlot>> FindByWorkerUserIdAsync(int workerUserId);
    Task RemoveByWorkerUserIdAsync(int workerUserId);
}
