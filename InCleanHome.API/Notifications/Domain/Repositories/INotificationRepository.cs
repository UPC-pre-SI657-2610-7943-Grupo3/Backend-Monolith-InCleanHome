using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Notifications.Domain.Repositories;

public interface INotificationRepository : IBaseRepository<Notification>
{
    Task<IEnumerable<Notification>> FindByUserIdAsync(int userId);
    Task<int> CountUnreadByUserIdAsync(int userId);
    Task<IEnumerable<Notification>> FindUnreadByUserIdAsync(int userId);
}
