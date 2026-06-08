using InCleanHome.API.Messaging.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Messaging.Domain.Repositories;

public interface IMessageRepository : IBaseRepository<Message>
{
    Task<IEnumerable<Message>> FindBetweenAsync(int userAId, int userBId);
    Task<IEnumerable<Message>> FindByUserAsync(int userId);
    Task MarkAsReadAsync(int userId, int otherUserId);
}
