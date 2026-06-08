using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Commands;
using InCleanHome.API.SearchAndCatalog.Domain.Model.Queries;
using InCleanHome.API.SearchAndCatalog.Domain.Repositories;
using InCleanHome.API.SearchAndCatalog.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.SearchAndCatalog.Application.Internal.CommandServices;

public class AvailabilitySlotCommandService(
    IAvailabilitySlotRepository repository,
    IUnitOfWork unitOfWork) : IAvailabilitySlotCommandService
{
    public async Task<IEnumerable<AvailabilitySlot>> Handle(ReplaceWorkerAvailabilityCommand command)
    {
        // Replace-all semantics matches the frontend's PUT /workers/{id}/availability
        await repository.RemoveByWorkerUserIdAsync(command.WorkerUserId);

        var fresh = command.Slots
            .Select(s => new AvailabilitySlot(command.WorkerUserId, s.DayOfWeek, s.StartTime, s.EndTime, s.IsAvailable))
            .ToList();

        foreach (var slot in fresh) await repository.AddAsync(slot);
        await unitOfWork.CompleteAsync();
        return fresh;
    }
}
