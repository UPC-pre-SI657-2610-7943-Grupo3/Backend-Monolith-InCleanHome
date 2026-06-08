using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.Notifications.Domain.Model.Aggregates;

/// <summary>
///     Notification aggregate root — an in-app message addressed to a single user
///     (booking updates, reminders, payment dates, etc.).
/// </summary>
public class Notification : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int UserId { get; private set; }
    public string Type { get; private set; } = "info";
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Link { get; private set; }
    public bool Read { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public Notification() { }

    public Notification(int userId, string type, string title, string body, string? link)
    {
        UserId = userId;
        Type   = string.IsNullOrWhiteSpace(type) ? "info" : type;
        Title  = title;
        Body   = body;
        Link   = link;
        Read   = false;
    }

    public Notification MarkAsRead()
    {
        Read = true;
        return this;
    }
}
