using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Domain.Model.Commands;
using InCleanHome.API.Notifications.Domain.Model.Queries;

namespace InCleanHome.API.Notifications.Domain.Services;

public interface INotificationCommandService
{
    Task<Notification> Handle(CreateNotificationCommand command);
    Task<bool> Handle(MarkNotificationReadCommand command);
    Task Handle(MarkAllNotificationsReadCommand command);
    Task<bool> Handle(DeleteNotificationCommand command);
}

public interface INotificationQueryService
{
    Task<IEnumerable<Notification>> Handle(GetNotificationsByUserIdQuery query);
    Task<int> Handle(GetUnreadCountByUserIdQuery query);
}

public interface IFirebaseMessagingService
{
    /// <summary>
    /// Envía una notificación push directa a un dispositivo usando Firebase Cloud Messaging.
    /// </summary>
    /// <param name="deviceToken">El token único del navegador o celular del usuario.</param>
    /// <param name="title">Título de la alerta (ej. "Reserva Aceptada 🧹")</param>
    /// <param name="body">Cuerpo del mensaje (ej. "Tu servicio ha sido programado con éxito.")</param>
    /// <param name="data">Diccionario con datos extra como el BookingId, Status, etc.</param>
    Task<string> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
}
