namespace InCleanHome.API.SearchAndCatalog.Domain.Model.Commands;

public record SlotInput(int DayOfWeek, string StartTime, string EndTime, bool IsAvailable);

public record ReplaceWorkerAvailabilityCommand(int WorkerUserId, List<SlotInput> Slots);
