using InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;
using InCleanHome.API.SearchAndCatalog.Interfaces.REST.Resources;

namespace InCleanHome.API.SearchAndCatalog.Interfaces.REST.Transform;

public static class AvailabilitySlotResourceFromEntityAssembler
{
    public static AvailabilitySlotResource ToResourceFromEntity(AvailabilitySlot s)
        => new(s.Id, s.DayOfWeek, s.StartTime, s.EndTime, s.IsAvailable);
}
