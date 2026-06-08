namespace InCleanHome.API.Notifications.Interfaces.ACL;

/// <summary>
///     ACL facade exposing notification creation to other bounded contexts
///     (e.g. Booking creates a notification when a booking status changes).
/// </summary>
public interface INotificationsContextFacade
{
    Task CreateNotification(int userId, string type, string title, string body, string? link);
}
