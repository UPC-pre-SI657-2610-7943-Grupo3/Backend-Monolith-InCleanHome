namespace InCleanHome.API.Notifications.Domain.Model.Queries;

public record GetNotificationsByUserIdQuery(int UserId);
public record GetUnreadCountByUserIdQuery(int UserId);
