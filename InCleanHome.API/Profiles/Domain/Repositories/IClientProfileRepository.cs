using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Profiles.Domain.Repositories;

public interface IClientProfileRepository : IBaseRepository<ClientProfile>
{
    Task<ClientProfile?> FindByUserIdAsync(int userId);
}
