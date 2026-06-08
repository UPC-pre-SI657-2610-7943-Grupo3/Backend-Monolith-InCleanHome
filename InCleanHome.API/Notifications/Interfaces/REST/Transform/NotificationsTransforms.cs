using InCleanHome.API.Notifications.Domain.Model.Aggregates;
using InCleanHome.API.Notifications.Interfaces.REST.Resources;

namespace InCleanHome.API.Notifications.Interfaces.REST.Transform;

public static class NotificationResourceFromEntityAssembler
{
    public static NotificationResource ToResourceFromEntity(Notification n)
        => new(n.Id, n.Type, n.Title, n.Body, n.Link, n.Read, n.CreatedDate);
}
