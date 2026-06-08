namespace InCleanHome.API.Messaging.Domain.Model.Queries;

public record GetConversationsForUserQuery(int UserId);
public record GetMessagesBetweenQuery(int UserAId, int UserBId);

/// <summary>Read model returned by the query layer (not an aggregate).</summary>
public record ConversationView(
    int UserId,
    string UserName,
    string? UserPhotoUrl,
    string LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);
