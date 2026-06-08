using InCleanHome.API.Notifications.Domain.Model.Commands;
using InCleanHome.API.Notifications.Domain.Services;

namespace InCleanHome.API.Notifications.Application.ACL;

public class NotificationsContextFacade(INotificationCommandService commandService)
    : Notifications.Interfaces.ACL.INotificationsContextFacade
{
    public async Task CreateNotification(int userId, string type, string title, string body, string? link)
        => await commandService.Handle(new CreateNotificationCommand(userId, type, title, body, link));
}
