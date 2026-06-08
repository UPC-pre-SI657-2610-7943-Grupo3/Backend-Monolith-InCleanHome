namespace InCleanHome.API.Notifications.Interfaces.REST.Resources;

public record NotificationResource(
    int Id,
    string Type,
    string Title,
    string Body,
    string? Link,
    bool Read,
    DateTimeOffset? CreatedAt);

public record UnreadCountResource(int Count);
