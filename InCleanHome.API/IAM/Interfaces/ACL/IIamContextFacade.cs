namespace InCleanHome.API.IAM.Interfaces.ACL;

public interface IIamContextFacade
{
    Task<int> FetchUserIdByEmail(string email);
    Task<string> FetchEmailByUserId(int userId);
    Task<string> FetchRoleByUserId(int userId);
    Task<bool> IsUserSuspended(int userId);
    Task<bool> IsWorkerApproved(int userId);
    Task SuspendUser(int userId, TimeSpan duration, string reason);
}
