namespace InCleanHome.API.SearchAndCatalog.Domain.Model.Aggregates;

/// <summary>
///     A weekly availability slot for a worker (used to filter and display schedule).
/// </summary>
/// <remarks>
///     <c>DayOfWeek</c> follows the JS convention: 0=Sunday, 1=Monday, ..., 6=Saturday.
///     Times are stored as strings in <c>HH:mm</c> format for simple JSON round-tripping
///     with the Vue calendar picker.
/// </remarks>
public class AvailabilitySlot
{
    public int Id { get; private set; }
    public int WorkerUserId { get; private set; }
    public int DayOfWeek { get; private set; }
    public string StartTime { get; private set; } = "08:00";
    public string EndTime { get; private set; }   = "18:00";
    public bool IsAvailable { get; private set; } = true;

    public AvailabilitySlot() { }

    public AvailabilitySlot(int workerUserId, int dayOfWeek, string startTime, string endTime, bool isAvailable)
    {
        WorkerUserId = workerUserId;
        DayOfWeek    = dayOfWeek;
        StartTime    = startTime;
        EndTime      = endTime;
        IsAvailable  = isAvailable;
    }

    public AvailabilitySlot Update(string startTime, string endTime, bool isAvailable)
    {
        StartTime   = startTime;
        EndTime     = endTime;
        IsAvailable = isAvailable;
        return this;
    }
}
