namespace InCleanHome.API.SearchAndCatalog.Interfaces.REST.Resources;

public record AvailabilitySlotResource(int Id, int DayOfWeek, string StartTime, string EndTime, bool IsAvailable);

public record SlotInputResource(int DayOfWeek, string StartTime, string EndTime, bool IsAvailable);

public record ReplaceAvailabilityResource(List<SlotInputResource> Slots);
