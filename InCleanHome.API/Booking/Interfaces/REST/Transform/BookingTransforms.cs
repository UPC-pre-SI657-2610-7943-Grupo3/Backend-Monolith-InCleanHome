using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Booking.Interfaces.REST.Resources;

namespace InCleanHome.API.Booking.Interfaces.REST.Transform;

public static class BookingResourceFromEntityAssembler
{
    public static BookingResource ToResourceFromEntity(BookingRequest b, string clientName, string workerName,
        string? workerPhotoUrl = null, string? clientPhotoUrl = null, bool isPaid = false)
    {
        var services = b.ServiceTypesList.ToList();
        // Backward compat: ServiceType expone el primer servicio (o "" si no hay).
        var primary = services.FirstOrDefault() ?? string.Empty;
        return new BookingResource(
            b.Id,
            b.ClientId,
            b.WorkerId,
            clientName,
            workerName,
            workerPhotoUrl,
            clientPhotoUrl,
            primary,
            services,
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
}
