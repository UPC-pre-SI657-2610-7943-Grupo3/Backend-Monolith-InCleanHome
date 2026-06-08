namespace InCleanHome.API.Booking.Domain.Model.ValueObjects;

/// <summary>
///     Booking status values matching the lowercase strings used by the Vue frontend.
/// </summary>
public static class BookingStatus
{
    public const string Pending   = "pending";
    public const string Accepted  = "accepted";
    public const string Rejected  = "rejected";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";

    public static readonly string[] All = { Pending, Accepted, Rejected, Cancelled, Completed };

    public static bool IsValid(string s) => All.Contains(s);
}
