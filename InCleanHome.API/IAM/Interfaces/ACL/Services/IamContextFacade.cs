using InCleanHome.API.IAM.Domain.Model.Commands;
using InCleanHome.API.IAM.Domain.Model.Queries;
using InCleanHome.API.IAM.Domain.Services;

namespace InCleanHome.API.IAM.Interfaces.ACL.Services;

public class IamContextFacade(
    IUserQueryService userQueryService,
    IUserCommandService userCommandService) : IIamContextFacade
{
    public async Task<int> FetchUserIdByEmail(string email)
    {
        var user = await userQueryService.Handle(new GetUserByEmailQuery(email));
        return user?.Id ?? 0;
    }

    public async Task<string> FetchEmailByUserId(int userId)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        return user?.Email ?? string.Empty;
    }

    public async Task<string> FetchRoleByUserId(int userId)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        return user?.Role ?? string.Empty;
    }

    public async Task<bool> IsUserSuspended(int userId)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        return user?.IsCurrentlySuspended() ?? false;
    }

    public async Task<bool> IsWorkerApproved(int userId)
    {
        var user = await userQueryService.Handle(new GetUserByIdQuery(userId));
        return user?.DocumentsVerified ?? false;
    }

    public async Task SuspendUser(int userId, TimeSpan duration, string reason)
        => await userCommandService.Handle(new SuspendUserCommand(userId, duration, reason));
}
