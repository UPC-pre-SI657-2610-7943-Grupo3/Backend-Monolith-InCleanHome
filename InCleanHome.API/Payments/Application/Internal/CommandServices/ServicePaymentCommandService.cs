using InCleanHome.API.Booking.Domain.Model.ValueObjects;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.Notifications.Interfaces.ACL;
using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Model.ValueObjects;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.Payments.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.ACL;
using InCleanHome.API.Shared.Domain.Repositories;
using InCleanHome.API.Shared.Domain.Services;

namespace InCleanHome.API.Payments.Application.Internal.CommandServices;

public class ServicePaymentCommandService(
    IServicePaymentRepository repository,
    IBookingRequestRepository bookingRepository,
    IUnitOfWork unitOfWork,
    INotificationsContextFacade notificationsFacade,
    IProfilesContextFacade profilesFacade,
    ICommissionRateProvider commissionProvider) : IServicePaymentCommandService
{
    /// <summary>
    ///     Notifica a la trabajadora que un servicio suyo acaba de ser pagado.
    ///     Best-effort: si la notificación falla NO se revierta el pago ya
    ///     guardado en BD.
    /// </summary>
    private async Task NotifyWorkerOfPaymentAsync(ServicePayment payment)
    {
        try
        {
            var clientName = await profilesFacade.FetchUserNameByUserId(payment.ClientId);
            if (string.IsNullOrWhiteSpace(clientName)) clientName = "El cliente";

            await notificationsFacade.CreateNotification(
                userId: payment.WorkerId,
                type:   "payment",
                title:  "Servicio pagado",
                body:   $"{clientName} pagó tu servicio. Revisa el detalle en tus solicitudes completadas.",
                link:   "/worker/requests");
        }
        catch (Exception ex)
        {
            // best-effort: log y seguimos. El pago ya está persistido.
            Console.WriteLine($"[Payments] Notificación de pago no enviada: {ex.Message}");
        }
    }

    public async Task<ServicePayment> Handle(PayBookingCommand c)
    {
        if (!PaymentChannel.IsValid(c.Channel))
            throw new InvalidOperationException("Invalid payment channel");

        // Los canales con pasarela van por su propio flujo (MercadoPago tiene
        // su confirmación dedicada). Este endpoint es solo para canales manuales.
        if (c.Channel == PaymentChannel.MercadoPago)
            throw new InvalidOperationException(
                "Use the Mercado Pago flow for gateway payments (POST /api/payments/mercadopago/...).");

        var booking = await bookingRepository.FindByIdAsync(c.BookingId)
            ?? throw new InvalidOperationException("Booking not found");

        if (booking.ClientId != c.ClientId)
            throw new InvalidOperationException("This booking does not belong to you");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException("Booking must be completed before payment");

        var existing = await repository.FindByBookingIdAsync(c.BookingId);
        if (existing is not null)
            throw new InvalidOperationException("This booking has already been paid");

        // Lee la comisión vigente al momento del pago (cacheada in-memory).
        // El valor queda implícito en PlatformFee / WorkerEarning del aggregate
        // y NO se recalcula si admin cambia la tasa después.
        var commissionRate = await commissionProvider.GetCurrentRateAsync();

        var payment = new ServicePayment(
            booking.Id, booking.ClientId, booking.WorkerId,
            booking.TotalAmount, c.Channel, commissionRate);

        await repository.AddAsync(payment);
        await unitOfWork.CompleteAsync();

        await NotifyWorkerOfPaymentAsync(payment);
        return payment;
    }

    public async Task<ServicePayment> Handle(ConfirmMercadoPagoPaymentCommand c)
    {
        var booking = await bookingRepository.FindByIdAsync(c.BookingId)
            ?? throw new InvalidOperationException("Booking not found");

        if (booking.ClientId != c.ClientId)
            throw new InvalidOperationException("This booking does not belong to you");

        if (booking.Status != BookingStatus.Completed)
            throw new InvalidOperationException("Booking must be completed before payment");

        var existing = await repository.FindByBookingIdAsync(c.BookingId);
        if (existing is not null)
            throw new InvalidOperationException("This booking has already been paid");

        var commissionRate = await commissionProvider.GetCurrentRateAsync();

        var payment = new ServicePayment(
            booking.Id, booking.ClientId, booking.WorkerId,
            booking.TotalAmount, PaymentChannel.MercadoPago, commissionRate,
            mercadoPagoPaymentId:    c.MercadoPagoPaymentId,
            mercadoPagoPreferenceId: c.MercadoPagoPreferenceId);

        await repository.AddAsync(payment);
        await unitOfWork.CompleteAsync();

        await NotifyWorkerOfPaymentAsync(payment);
        return payment;
    }

    public async Task<int> Handle(RequestPayoutCommand c)
    {
        var pending = (await repository.FindPendingPayoutsByWorkerIdAsync(c.WorkerId)).ToList();
        if (pending.Count == 0) return 0;

        foreach (var p in pending)
        {
            p.MarkPayoutRequested();
            repository.Update(p);
        }
        await unitOfWork.CompleteAsync();
        return pending.Count;
    }
}
