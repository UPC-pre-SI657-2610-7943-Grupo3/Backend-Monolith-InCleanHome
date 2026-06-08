namespace InCleanHome.API.Booking.Interfaces.REST.Resources;

public record CreateBookingResource(
    int WorkerId,
    string ServiceType,
    string Date,           // "yyyy-MM-dd" from the frontend calendar
    string StartTime,      // "HH:mm"
    string EndTime,        // "HH:mm"
    decimal Hours,
    int PaymentMethodId,
    string Address,
    string? Notes);

public record UpdateBookingStatusResource(string Status);

public record RescheduleBookingResource(
    DateOnly Date,
    string StartTime,
    string EndTime,
    decimal Hours);

public record BookingResource(
    int Id,
    int ClientId,
    int WorkerId,
    string ClientName,
    string WorkerName,
    string? WorkerPhotoUrl,
    string? ClientPhotoUrl,
    string ServiceType,
    string Date,
    string StartTime,
    string EndTime,
    decimal Hours,
    int PaymentMethodId,
    string Address,
    string Notes,
    decimal HourlyRate,
    decimal TotalAmount,
    decimal PlatformFee,
    decimal WorkerEarning,
    string Status,
    bool IsPaid,
    DateTimeOffset? CreatedAt);
