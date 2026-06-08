using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Queries;
using InCleanHome.API.SearchAndCatalog.Domain.Repositories;
using InCleanHome.API.SearchAndCatalog.Domain.Services;

namespace InCleanHome.API.SearchAndCatalog.Application.Internal.QueryServices;

public class AvailabilitySlotQueryService(IAvailabilitySlotRepository repository) : IAvailabilitySlotQueryService
{
    public async Task<IEnumerable<AvailabilitySlot>> Handle(GetAvailabilityByWorkerUserIdQuery query)
        => await repository.FindByWorkerUserIdAsync(query.WorkerUserId);
}
