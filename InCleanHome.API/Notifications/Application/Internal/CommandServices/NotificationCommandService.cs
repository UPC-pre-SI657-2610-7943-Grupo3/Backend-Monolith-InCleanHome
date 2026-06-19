using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Domain.Model.Commands;
using InCleanHome.API.Notifications.Domain.Repositories;
using InCleanHome.API.Notifications.Domain.Services;
using InCleanHome.API.Notifications.Domain.Services.External;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Notifications.Application.Internal.CommandServices;

public class NotificationCommandService(
    INotificationRepository repository,
    IUnitOfWork unitOfWork,
    IPushNotificationProvider pushProvider,
    IUserRepository userRepository) : INotificationCommandService // 🚀 Inyectamos Firebase + IUserRepository
{
    public async Task<Notification> Handle(CreateNotificationCommand c)
    {
        // 1. Guardamos localmente en PostgreSQL (Historial en la App Web)
        var notification = new Notification(c.UserId, c.Type, c.Title, c.Body, c.Link);
        await repository.AddAsync(notification);
        await unitOfWork.CompleteAsync();

        Console.WriteLine($"[Notifications] Created notification id={notification.Id} for userId={c.UserId} type={c.Type}");

        // 2. 🚀 Lógica de Envío Push en Segundo Plano con Firebase
        // Buscamos el DeviceToken asociado al usuario en la BD (registrado vía POST /auth/device-token desde el frontend).
        string? userDeviceToken = await GetUserDeviceTokenAsync(c.UserId);

        Console.WriteLine($"[Notifications] Token lookup for userId={c.UserId}: {(string.IsNullOrEmpty(userDeviceToken) ? "NULL/EMPTY ❌" : $"OK ({userDeviceToken.Length} chars)")}");

        if (!string.IsNullOrEmpty(userDeviceToken))
        {
            try
            {
                // Preparamos metadatos adicionales útiles para el Frontend (ej. redirecciones por clicks)
                var extraData = new Dictionary<string, string>
                {
                    { "type", c.Type },
                    { "link", c.Link ?? "" },
                    { "userId", c.UserId.ToString() },
                    { "notificationId", notification.Id.ToString() }
                };

                Console.WriteLine($"[Notifications] Calling Firebase SendNotificationAsync for userId={c.UserId}...");

                // Despachamos la notificación push directo a los servidores de Firebase
                await pushProvider.SendNotificationAsync(userDeviceToken, c.Title, c.Body, extraData);
            }
            catch (Exception ex)
            {
                // Usamos un bloque try-catch para que si Firebase falla por un token expirado,
                // la transacción principal de tu monolito NO se caiga ni afecte al flujo del usuario.
                Console.WriteLine($"[Firebase Error] No se pudo enviar la notificación push: {ex.Message}");
                Console.WriteLine($"[Firebase Error] Stack: {ex.StackTrace}");
            }
        }
        else
        {
            Console.WriteLine($"[Notifications] Skipping push: no device token for userId={c.UserId}");
        }

        return notification;
    }

    /// <summary>
    /// Recupera el token de Firebase Cloud Messaging del dispositivo/navegador del usuario.
    /// Devuelve null si el usuario no ha registrado todavía un token (p. ej. no concedió permiso).
    /// </summary>
    private async Task<string?> GetUserDeviceTokenAsync(int userId)
    {
        return await userRepository.FindDeviceTokenByIdAsync(userId);
    }

    public async Task<bool> Handle(MarkNotificationReadCommand c)
    {
        var notification = await repository.FindByIdAsync(c.NotificationId);
        // Only the owner can mark their own notification as read.
        if (notification is null || notification.UserId != c.UserId) return false;

        notification.MarkAsRead();
        repository.Update(notification);
        await unitOfWork.CompleteAsync();
        return true;
    }

    public async Task Handle(MarkAllNotificationsReadCommand c)
    {
        var unread = await repository.FindUnreadByUserIdAsync(c.UserId);
        foreach (var n in unread)
        {
            n.MarkAsRead();
            repository.Update(n);
        }
        await unitOfWork.CompleteAsync();
    }

    public async Task<bool> Handle(DeleteNotificationCommand c)
    {
        var notification = await repository.FindByIdAsync(c.NotificationId);
        // Only the owner can delete their own notification.
        if (notification is null || notification.UserId != c.UserId) return false;

        repository.Remove(notification);
        await unitOfWork.CompleteAsync();
        return true;
    }
}
