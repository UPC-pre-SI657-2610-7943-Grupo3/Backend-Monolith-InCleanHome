using InCleanHome.API.Messaging.Domain.Model.Aggregates;
using InCleanHome.API.Messaging.Domain.Model.Queries;
using InCleanHome.API.Messaging.Domain.Repositories;
using InCleanHome.API.Messaging.Domain.Services;
using InCleanHome.API.Profiles.Interfaces.ACL;

namespace InCleanHome.API.Messaging.Application.Internal.QueryServices;

public class MessageQueryService(
    IMessageRepository repository,
    IProfilesContextFacade profilesFacade) : IMessageQueryService
{
    public async Task<IEnumerable<Message>> Handle(GetMessagesBetweenQuery query)
        => await repository.FindBetweenAsync(query.UserAId, query.UserBId);

    /// <summary>
    ///     Aggregates all messages of the user into one ConversationView per peer.
    /// </summary>
    public async Task<IEnumerable<ConversationView>> Handle(GetConversationsForUserQuery query)
    {
        var all = (await repository.FindByUserAsync(query.UserId)).ToList();

        var conversations = all
            .GroupBy(m => m.SenderId == query.UserId ? m.RecipientId : m.SenderId)
            .Select(g =>
            {
                var ordered = g.OrderByDescending(m => m.CreatedDate).ToList();
                var last = ordered.First();
                var unread = ordered.Count(m => m.RecipientId == query.UserId && m.ReadAt == null);
                return new { OtherId = g.Key, Last = last, Unread = unread };
            })
            .OrderByDescending(c => c.Last.CreatedDate)
            .ToList();

        var result = new List<ConversationView>();
        foreach (var c in conversations)
        {
            var name = await profilesFacade.FetchUserNameByUserId(c.OtherId);
            if (string.IsNullOrEmpty(name)) name = $"User {c.OtherId}";
            
            var photo = await profilesFacade.FetchWorkerPhotoByUserId(c.OtherId)
                        ?? await profilesFacade.FetchClientPhotoByUserId(c.OtherId);
            result.Add(new ConversationView(c.OtherId, name, photo, c.Last.Content, c.Last.CreatedDate, c.Unread));
        }
        return result;
    }
}
