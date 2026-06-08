namespace InCleanHome.API.Messaging.Domain.Model.Commands;

public record SendMessageCommand(int SenderId, int RecipientId, string Content);
public record MarkConversationAsReadCommand(int UserId, int OtherUserId);
