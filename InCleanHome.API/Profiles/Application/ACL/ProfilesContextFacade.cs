using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Services;

namespace InCleanHome.API.Profiles.Application.ACL;

public class ProfilesContextFacade(
    IClientProfileQueryService clientQueryService,
    IWorkerProfileQueryService workerQueryService,
    IWorkerProfileCommandService workerCommandService,
    IUserRepository userRepository) : Profiles.Interfaces.ACL.IProfilesContextFacade
{
    public async Task<string> FetchUserNameByUserId(int userId)
    {
        var worker = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(userId));
        if (worker != null) return worker.Name;
        var client = await clientQueryService.Handle(new GetClientProfileByUserIdQuery(userId));
        return client?.Name ?? string.Empty;
    }

    public async Task<string?> FetchUserEmailByUserId(int userId)
    {
        // El email vive en el aggregate User (IAM), no en los perfiles. Lo
        // resolvemos por el repositorio de usuarios. Retornamos null si no
        // existe (el caller puede usar un default).
        var user = await userRepository.FindByIdAsync(userId);
        return user?.Email;
    }

    public async Task<decimal> FetchWorkerHourlyRateByUserId(int userId)
    {
        var worker = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(userId));
        return worker?.HourlyRate ?? 0m;
    }

    public async Task RegisterWorkerCompletedService(int workerUserId, int rating)
        => await workerCommandService.Handle(new RegisterWorkerCompletedServiceCommand(workerUserId, rating));

    public async Task<string?> FetchWorkerPhotoByUserId(int userId)
    {
        var worker = await workerQueryService.Handle(new GetWorkerProfileByUserIdQuery(userId));
        return worker?.PhotoUrl;
    }

    public async Task<string?> FetchClientPhotoByUserId(int userId)
    {
        var client = await clientQueryService.Handle(new GetClientProfileByUserIdQuery(userId));
        return client?.PhotoUrl;
    }
}
