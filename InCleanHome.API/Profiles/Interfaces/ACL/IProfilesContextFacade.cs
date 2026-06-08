namespace InCleanHome.API.Profiles.Interfaces.ACL;

/// <summary>
///     ACL facade exposing Profiles operations to other bounded contexts (read-only essentials).
/// </summary>
public interface IProfilesContextFacade
{
    Task<string> FetchUserNameByUserId(int userId);
    Task<decimal> FetchWorkerHourlyRateByUserId(int userId);
    Task RegisterWorkerCompletedService(int workerUserId, int rating);
    Task<string?> FetchWorkerPhotoByUserId(int userId);
    Task<string?> FetchClientPhotoByUserId(int userId);
}
