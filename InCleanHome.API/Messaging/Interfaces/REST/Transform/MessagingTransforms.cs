using InCleanHome.API.Messaging.Domain.Model.Aggregates;
using InCleanHome.API.Messaging.Domain.Model.Queries;
using InCleanHome.API.Messaging.Interfaces.REST.Resources;

namespace InCleanHome.API.Messaging.Interfaces.REST.Transform;

public static class MessageResourceFromEntityAssembler
{
    public static MessageResource ToResourceFromEntity(Message m)
        => new(m.Id, m.SenderId, m.RecipientId, m.Content, m.CreatedDate, m.ReadAt);
}

public static class ConversationResourceFromViewAssembler
{
    public static ConversationResource ToResourceFromView(ConversationView v)
        => new(v.UserId, v.UserName, v.UserPhotoUrl, v.LastMessage, v.LastMessageAt, v.UnreadCount);
}
