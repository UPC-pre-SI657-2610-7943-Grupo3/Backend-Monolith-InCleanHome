using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Booking.Domain.Model.Commands;
using InCleanHome.API.Booking.Domain.Model.ValueObjects;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.Booking.Domain.Services;
using InCleanHome.API.IAM.Domain.Model.ValueObjects;
using InCleanHome.API.IAM.Interfaces.ACL;
using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.ACL;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Domain.Services;

namespace InCleanHome.API.Booking.Application.Internal.CommandServices;

public class BookingRequestCommandService(
    IBookingRequestRepository repository,
    IWorkerProfileQueryService workerQueryService,
    IProfilesContextFacade profilesFacade,
    INotificationsContextFacade notificationsFacade,
    IIamContextFacade iamFacade,
    ICommissionRateProvider commissionProvider,
    IUnitOfWork unitOfWork) : IBookingRequestCommandService
{
    public async Task<BookingRequest> Handle(CreateBookingCommand c)
    {
        // Block if the client (the booker) is currently suspended.
        if (await iamFacade.IsUserSuspended(c.ClientId))
            throw new InvalidOperationException(
                "Tu cuenta está temporalmente suspendida por una cancelación tardía. No puedes reservar hasta que termine la sanción.");

        // Block if the worker is not yet approved by admin.
        if (!await iamFacade.IsWorkerApproved(c.WorkerId))
            throw new InvalidOperationException(
                "Trabajador(a) aún no ha sido aprobada por administración.");

        // Block if the worker is currently suspended.
        if (await iamFacade.IsUserSuspended(c.WorkerId))
            throw new InvalidOperationException(
                "Trabajador(a) se encuentra temporalmente suspendida. Inténtalo más tarde.");

        var worker = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(c.WorkerId))
            ?? throw new Exception("Worker not found");

        // Block if the worker already has an active booking overlapping this time slot.
        var overlapping = await repository.FindWorkerOverlappingAsync(c.WorkerId, c.Date, c.StartTime, c.EndTime);
        if (overlapping.Any())
            throw new InvalidOperationException(
                "No puedes reservar a esta hora, ya que otro cliente ya reservó a esta hora con este/esta trabajador(a). Por favor elige otro horario o fecha.");

        // Selecciona la tarifa aplicable según el día de la semana. Si el booking
        // cae domingo, usamos la tarifa especial declarada por la trabajadora.
        // Esta decisión se toma server-side para que el cliente no pueda pasar
        // un monto manipulado: el backend es la única fuente de verdad.
        var rateForBooking = c.Date.DayOfWeek == DayOfWeek.Sunday
            ? (worker.HourlyRateSunday > 0 ? worker.HourlyRateSunday : worker.HourlyRate)
            : worker.HourlyRate;

        // Lee la comisión actual desde PlatformSettings (con caché in-memory).
        // El valor queda "snapshot" dentro del booking — no se recalcula si
        // luego admin cambia la tasa.
        var commissionRate = await commissionProvider.GetCurrentRateAsync();

        var booking = new BookingRequest(
            c.ClientId, c.WorkerId, c.ServiceTypes ?? new List<string>(), c.Date,
            c.StartTime, c.EndTime, c.Hours, c.PaymentMethodId,
            c.Address, c.Notes ?? string.Empty, rateForBooking, commissionRate);

        await repository.AddAsync(booking);
        await unitOfWork.CompleteAsync();

        // Notify the worker of the new request.
        var clientName = await profilesFacade.FetchUserNameByUserId(c.ClientId);
        await notificationsFacade.CreateNotification(
            c.WorkerId, "pending", "Nueva solicitud",
            $"{clientName} solicitó un servicio para el {c.Date:yyyy-MM-dd}. Revísalo en tus solicitudes.", "/worker/requests");

        return booking;
    }

    public async Task<BookingRequest?> Handle(UpdateBookingStatusCommand c)
    {
        var booking = await repository.FindByIdAsync(c.BookingId);
        if (booking is null) return null;

        // Authorization rules per role
        var isClient = c.RequesterRole == UserRole.Client && booking.ClientId == c.RequesterUserId;
        var isWorker = c.RequesterRole == UserRole.Worker && booking.WorkerId == c.RequesterUserId;
        var isAdmin  = c.RequesterRole == UserRole.Admin;
        if (!isClient && !isWorker && !isAdmin)
            throw new UnauthorizedAccessException("Not allowed to change this booking");

        switch (c.NewStatus)
        {
            case BookingStatus.Accepted:
                if (!isWorker && !isAdmin) throw new UnauthorizedAccessException("Only workers can accept bookings");
                booking.Accept();
                break;
            case BookingStatus.Rejected:
                if (!isWorker && !isAdmin) throw new UnauthorizedAccessException("Only workers can reject bookings");
                booking.Reject();
                break;
            case BookingStatus.Cancelled:
                // Both client and worker may cancel a pending/accepted booking.
                // If they cancel late (inside the penalty window), apply the suspension.
                if (isWorker)
                {
                    var late = booking.IsLateCancellation(byWorker: true);
                    booking.CancelByWorker();
                    if (late)
                        await iamFacade.SuspendUser(booking.WorkerId, TimeSpan.FromDays(7),
                            "Cancelación tardía (menos de 7 días hábiles antes del servicio).");
                }
                else
                {
                    var late = booking.IsLateCancellation(byWorker: false);
                    booking.CancelByClient();
                    if (late)
                        await iamFacade.SuspendUser(booking.ClientId, TimeSpan.FromHours(48),
                            "Cancelación tardía (menos de 3 días hábiles antes del servicio).");
                }
                break;
            case BookingStatus.Completed:
                if (!isWorker && !isAdmin) throw new UnauthorizedAccessException("Only workers can complete bookings");
                booking.Complete();
                break;
            default:
                throw new InvalidOperationException($"Unsupported status transition '{c.NewStatus}'");
        }

        repository.Update(booking);
        await unitOfWork.CompleteAsync();

        // Notify the counterpart about the status change.
        await NotifyStatusChange(booking, c.NewStatus, isWorker);

        return booking;
    }

    // Sends an in-app notification to the affected party after a status transition.
    private async Task NotifyStatusChange(BookingRequest booking, string status, bool changedByWorker)
    {
        var workerName = await profilesFacade.FetchUserNameByUserId(booking.WorkerId);
        var clientName = await profilesFacade.FetchUserNameByUserId(booking.ClientId);

        switch (status)
        {
            case BookingStatus.Accepted:
                await notificationsFacade.CreateNotification(booking.ClientId, "accepted",
                    "Reserva aceptada", $"Trabajador(a) {workerName} aceptó tu reserva del {booking.Date:yyyy-MM-dd}.", "/client/bookings");
                break;
            case BookingStatus.Rejected:
                await notificationsFacade.CreateNotification(booking.ClientId, "rejected",
                    "Reserva rechazada", $"Trabajador(a) {workerName} no pudo aceptar tu reserva del {booking.Date:yyyy-MM-dd}.", "/client/bookings");
                break;
            case BookingStatus.Completed:
                await notificationsFacade.CreateNotification(booking.ClientId, "completed",
                    "Servicio completado", $"Tu servicio con el/la trabajador(a) {workerName} fue completado.", "/client/bookings");
                break;
            case BookingStatus.Cancelled:
                if (changedByWorker)
                    await notificationsFacade.CreateNotification(booking.ClientId, "cancelled",
                        "Reserva cancelada", $"Trabajador(a) {workerName} canceló la reserva del {booking.Date:yyyy-MM-dd}.", "/client/bookings");
                else
                    await notificationsFacade.CreateNotification(booking.WorkerId, "cancelled",
                        "Reserva cancelada", $"{clientName} canceló la reserva del {booking.Date:yyyy-MM-dd}.", "/worker/requests");
                break;
        }
    }

    public async Task<BookingRequest?> Handle(RescheduleBookingCommand c)
    {
        var booking = await repository.FindByIdAsync(c.BookingId);
        if (booking is null) return null;

        // Solo el cliente o la trabajadora de la reserva pueden reprogramarla.
        var isClient = booking.ClientId == c.RequesterUserId;
        var isWorker = booking.WorkerId == c.RequesterUserId;
        if (!isClient && !isWorker)
            throw new UnauthorizedAccessException("No tienes permiso para reprogramar esta reserva.");

        var worker = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(booking.WorkerId))
            ?? throw new Exception("Worker not found");

        // Misma regla que al crear: si la nueva fecha cae domingo, usa la tarifa
        // de domingo de la trabajadora. La regla NO depende de la fecha original.
        var rateForReschedule = c.NewDate.DayOfWeek == DayOfWeek.Sunday
            ? (worker.HourlyRateSunday > 0 ? worker.HourlyRateSunday : worker.HourlyRate)
            : worker.HourlyRate;

        // En reschedule también leemos la comisión actual: si el booking se
        // creó hace un mes con tasa antigua, el nuevo monto usa la tasa de hoy.
        var rescheduleCommissionRate = await commissionProvider.GetCurrentRateAsync();
        booking.Reschedule(c.NewDate, c.NewStartTime, c.NewEndTime, c.NewHours,
                           rateForReschedule, rescheduleCommissionRate);
        repository.Update(booking);
        await unitOfWork.CompleteAsync();

        // Notifica a la contraparte del cambio (requiere su confirmación porque la
        // reserva volvió a "pending").
        var workerName = await profilesFacade.FetchUserNameByUserId(booking.WorkerId);
        var clientName = await profilesFacade.FetchUserNameByUserId(booking.ClientId);
        if (isClient)
            await notificationsFacade.CreateNotification(booking.WorkerId, "pending",
                "Solicitud reprogramada",
                $"{clientName} reprogramó la reserva al {booking.Date:yyyy-MM-dd} ({booking.StartTime}–{booking.EndTime}). Necesita tu confirmación.",
                "/worker/requests");
        else
            await notificationsFacade.CreateNotification(booking.ClientId, "pending",
                "Reserva reprogramada",
                $"La trabajador(a) {workerName} reprogramó tu reserva al {booking.Date:yyyy-MM-dd} ({booking.StartTime}–{booking.EndTime}).",
                "/client/bookings");

        return booking;
    }
}
