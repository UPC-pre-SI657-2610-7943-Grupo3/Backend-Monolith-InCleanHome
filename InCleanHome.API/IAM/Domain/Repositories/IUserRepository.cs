using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.IAM.Domain.Repositories;

public interface IUserRepository : IBaseRepository<User>
{
    Task<User?> FindByEmailAsync(string email);
    bool ExistsByEmail(string email);
    Task<User?> FindByResetTokenAsync(string token);
    /// <summary>
    ///     Returns just the Firebase device token for a given user without loading
    ///     the full aggregate. Returns null if the user has none registered.
    /// </summary>
    Task<string?> FindDeviceTokenByIdAsync(int userId);
}
