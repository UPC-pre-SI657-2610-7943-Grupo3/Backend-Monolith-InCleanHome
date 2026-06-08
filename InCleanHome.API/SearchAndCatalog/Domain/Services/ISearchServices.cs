using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Commands;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Queries;

namespace InCleanHome.API.SearchAndCatalog.Domain.Services;

public interface IAvailabilitySlotCommandService
{
    Task<IEnumerable<AvailabilitySlot>> Handle(ReplaceWorkerAvailabilityCommand command);
}

public interface IAvailabilitySlotQueryService
{
    Task<IEnumerable<AvailabilitySlot>> Handle(GetAvailabilityByWorkerUserIdQuery query);
}
