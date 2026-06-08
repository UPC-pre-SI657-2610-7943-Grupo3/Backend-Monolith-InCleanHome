using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.Messaging.Domain.Model.Aggregates;

/// <summary>
///     Message aggregate root — a single direct message between two users.
/// </summary>
/// <remarks>
///     The conversation is implicit: any pair of <c>SenderId</c>/<c>RecipientId</c> messages,
///     ordered by <c>CreatedDate</c>, makes a conversation thread. <c>ReadAt</c> is set once
///     the recipient opens the chat.
/// </remarks>
public class Message : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int SenderId { get; private set; }
    public int RecipientId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public DateTimeOffset? ReadAt { get; private set; }

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public Message() { }

    public Message(int senderId, int recipientId, string content)
    {
        SenderId    = senderId;
        RecipientId = recipientId;
        Content     = content ?? string.Empty;
        ReadAt      = null;
    }

    public Message MarkAsRead()
    {
        if (ReadAt is null) ReadAt = DateTimeOffset.UtcNow;
        return this;
    }
}
