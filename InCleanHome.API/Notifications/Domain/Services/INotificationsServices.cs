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
