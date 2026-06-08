using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Notifications.Infrastructure.Persistence.EFC.Repositories;

public class NotificationRepository(AppDbContext context)
    : BaseRepository<Notification>(context), INotificationRepository
{
    public async Task<IEnumerable<Notification>> FindByUserIdAsync(int userId)
        => await Context.Set<Notification>()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedDate)
            .ToListAsync();

    public async Task<int> CountUnreadByUserIdAsync(int userId)
        => await Context.Set<Notification>()
            .CountAsync(n => n.UserId == userId && !n.Read);

    public async Task<IEnumerable<Notification>> FindUnreadByUserIdAsync(int userId)
        => await Context.Set<Notification>()
            .Where(n => n.UserId == userId && !n.Read)
            .ToListAsync();
}
