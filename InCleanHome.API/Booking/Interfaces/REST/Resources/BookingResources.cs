namespace InCleanHome.API.Booking.Interfaces.REST.Resources;

public record CreateBookingResource(
    int WorkerId,
    // ServiceTypes: la nueva lista enviada desde el frontend. Si llega vacía o null,
    // se usa ServiceType (campo legacy) como fallback de un único servicio, para
    // no romper clientes viejos que aún manden el campo singular.
    List<string>? ServiceTypes,
    string? ServiceType,
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
    // ServiceType: legacy. Conserva el primer servicio para mantener compatibilidad
    // con código viejo que aún lo lea. Para nuevo código, usar ServiceTypes.
    string ServiceType,
    List<string> ServiceTypes,
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
