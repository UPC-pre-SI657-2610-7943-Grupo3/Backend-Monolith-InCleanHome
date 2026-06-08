using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Domain.Model.Queries;
using InCleanHome.API.Notifications.Domain.Repositories;
using InCleanHome.API.Notifications.Domain.Services;

namespace InCleanHome.API.Notifications.Application.Internal.QueryServices;

public class NotificationQueryService(INotificationRepository repository) : INotificationQueryService
{
    public async Task<IEnumerable<Notification>> Handle(GetNotificationsByUserIdQuery query)
        => await repository.FindByUserIdAsync(query.UserId);

    public async Task<int> Handle(GetUnreadCountByUserIdQuery query)
        => await repository.CountUnreadByUserIdAsync(query.UserId);
}
