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

namespace InCleanHome.API.Payments.Application.Internal.CommandServices;

public class ServicePaymentCommandService(
    IServicePaymentRepository repository,
    IBookingRequestRepository bookingRepository,
    IUnitOfWork unitOfWork,
    INotificationsContextFacade notificationsFacade,
    IProfilesContextFacade profilesFacade) : IServicePaymentCommandService
{
    /// <summary>
    /// Notifica al trabajador que un servicio suyo acaba de ser pagado por el cliente.
    /// Se llama después de crear la fila ServicePayment, envuelto en try/catch para que
    /// si la notificación falla NO se revierta el pago ya guardado.
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
            Console.WriteLine($"[Payments] Could not create payment notification: {ex.Message}");
        }
    }

    public async Task<ServicePayment> Handle(PayBookingCommand c)
    {
        if (!PaymentChannel.IsValid(c.Channel))
            throw new Exception("Invalid payment channel");

        // El cliente no debería poder usar este endpoint para pagar con tarjeta o
        // PayPal — esos canales van por sus respectivos controllers de pasarela.
        if (c.Channel == PaymentChannel.IzipayCard)
            throw new Exception("Use Izipay flow for card payments");
        if (c.Channel == PaymentChannel.PayPal)
            throw new Exception("Use PayPal flow for PayPal payments");

        var booking = await bookingRepository.FindByIdAsync(c.BookingId)
            ?? throw new Exception("Booking not found");

        if (booking.ClientId != c.ClientId)
            throw new Exception("This booking does not belong to you");

        if (booking.Status != BookingStatus.Completed)
            throw new Exception("Booking must be completed before payment");

        var existing = await repository.FindByBookingIdAsync(c.BookingId);
        if (existing is not null)
            throw new Exception("This booking has already been paid");

        var payment = new ServicePayment(
            booking.Id, booking.ClientId, booking.WorkerId,
            booking.TotalAmount, c.Channel);

        await repository.AddAsync(payment);
        await unitOfWork.CompleteAsync();

        await NotifyWorkerOfPaymentAsync(payment);
        return payment;
    }

    public async Task<ServicePayment> Handle(ConfirmIzipayPaymentCommand c)
    {
        var booking = await bookingRepository.FindByIdAsync(c.BookingId)
            ?? throw new Exception("Booking not found");

        if (booking.ClientId != c.ClientId)
            throw new Exception("This booking does not belong to you");

        if (booking.Status != BookingStatus.Completed)
            throw new Exception("Booking must be completed before payment");

        var existing = await repository.FindByBookingIdAsync(c.BookingId);
        if (existing is not null)
            throw new Exception("This booking has already been paid");

        var payment = new ServicePayment(
            booking.Id, booking.ClientId, booking.WorkerId,
            booking.TotalAmount, PaymentChannel.IzipayCard,
            izipayOrderId: c.IzipayOrderId, izipayTransactionId: c.IzipayTransactionId);

        await repository.AddAsync(payment);
        await unitOfWork.CompleteAsync();

        await NotifyWorkerOfPaymentAsync(payment);
        return payment;
    }

    public async Task<ServicePayment> Handle(ConfirmPayPalPaymentCommand c)
    {
        var booking = await bookingRepository.FindByIdAsync(c.BookingId)
            ?? throw new Exception("Booking not found");

        if (booking.ClientId != c.ClientId)
            throw new Exception("This booking does not belong to you");

        if (booking.Status != BookingStatus.Completed)
            throw new Exception("Booking must be completed before payment");

        var existing = await repository.FindByBookingIdAsync(c.BookingId);
        if (existing is not null)
            throw new Exception("This booking has already been paid");

        var payment = new ServicePayment(
            booking.Id, booking.ClientId, booking.WorkerId,
            booking.TotalAmount, PaymentChannel.PayPal,
            paypalOrderId: c.PayPalOrderId, paypalCaptureId: c.PayPalCaptureId);

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
