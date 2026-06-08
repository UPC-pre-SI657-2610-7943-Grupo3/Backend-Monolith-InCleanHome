using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Profiles.Application.Internal.CommandServices;

public class ClientProfileCommandService(
    IClientProfileRepository repository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork) : IClientProfileCommandService
{
    public async Task<ClientProfile> Handle(CreateClientProfileCommand command)
    {
        var profile = new ClientProfile(command.UserId, command.Name, command.Phone);
        await repository.AddAsync(profile);
        await unitOfWork.CompleteAsync();
        return profile;
    }

    public async Task<ClientProfile?> Handle(UpdateClientProfileCommand command)
    {
        var profile = await repository.FindByUserIdAsync(command.UserId);
        if (profile == null) return null;

        // Update ClientProfile with name and phone
        profile.Update(command.Name, command.Phone);
        repository.Update(profile);

        await unitOfWork.CompleteAsync();
        return profile;
    }

    public async Task<ClientProfile?> Handle(UpdateClientPhotoCommand command)
    {
        var profile = await repository.FindByUserIdAsync(command.UserId);
        if (profile == null) return null;
        profile.SetPhoto(command.PhotoUrl);
        repository.Update(profile);
        await unitOfWork.CompleteAsync();
        return profile;
    }
}
