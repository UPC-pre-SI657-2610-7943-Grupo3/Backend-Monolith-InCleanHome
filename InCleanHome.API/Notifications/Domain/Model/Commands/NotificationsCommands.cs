namespace InCleanHome.API.Notifications.Domain.Model.Commands;

public record CreateNotificationCommand(int UserId, string Type, string Title, string Body, string? Link);
public record MarkNotificationReadCommand(int NotificationId, int UserId);
public record MarkAllNotificationsReadCommand(int UserId);
public record DeleteNotificationCommand(int NotificationId, int UserId);
