namespace InCleanHome.API.Messaging.Interfaces.REST.Resources;

// ── Legacy resources (kept for backwards compatibility) ───────────────────────

public record SendMessageResource(string Content);

public record MessageResource(
    int Id,
    int SenderId,
    int RecipientId,
    string Content,
    DateTimeOffset? CreatedAt,
    DateTimeOffset? ReadAt);

public record ConversationResource(
    int UserId,
    string UserName,
    string? UserPhotoUrl,
    string LastMessage,
    DateTimeOffset? LastMessageAt,
    int UnreadCount);

// ── Twilio Conversations resources ────────────────────────────────────────────

/// <summary>
///     Returned by GET /api/messages/token.
///     The frontend uses <c>Token</c> to initialize the Twilio Conversations SDK
///     and <c>Identity</c> to know the current user's Twilio identity string.
/// </summary>
public record TwilioTokenResource(string Token, string Identity);

/// <summary>
///     Returned by POST /api/messages/conversation/{userId}.
///     The frontend uses <c>ConversationSid</c> to join or create the chat channel.
/// </summary>
public record ConversationSidResource(string ConversationSid);
