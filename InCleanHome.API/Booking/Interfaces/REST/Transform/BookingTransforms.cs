using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Booking.Interfaces.REST.Resources;

namespace InCleanHome.API.Booking.Interfaces.REST.Transform;

public static class BookingResourceFromEntityAssembler
{
    public static BookingResource ToResourceFromEntity(BookingRequest b, string clientName, string workerName,
        string? workerPhotoUrl = null, string? clientPhotoUrl = null, bool isPaid = false)
        => new(
            b.Id,
            b.ClientId,
            b.WorkerId,
            clientName,
            workerName,
            workerPhotoUrl,
            clientPhotoUrl,
            b.ServiceType,
            b.Date.ToString("yyyy-MM-dd"),
            b.StartTime,
            b.EndTime,
            b.Hours,
            b.PaymentMethodId,
            b.Address,
            b.Notes,
            b.HourlyRate,
            b.TotalAmount,
            b.PlatformFee,
            b.WorkerEarning,
            b.Status,
            isPaid,
            b.CreatedDate);
}
