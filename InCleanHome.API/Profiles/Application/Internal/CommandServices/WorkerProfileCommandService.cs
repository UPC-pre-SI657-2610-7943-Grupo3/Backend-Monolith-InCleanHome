using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Profiles.Application.Internal.CommandServices;

public class WorkerProfileCommandService(
    IWorkerProfileRepository repository,
    IUnitOfWork unitOfWork) : IWorkerProfileCommandService
{
    public async Task<WorkerProfile> Handle(CreateWorkerProfileCommand c)
    {
        var profile = new WorkerProfile(
            c.UserId, c.Name, c.Phone, c.Age, c.Gender,
            c.ServiceTypes, c.Zones, c.HourlyRate, c.HourlyRateSunday,
            c.ExperienceYears, c.Bio);
        await repository.AddAsync(profile);
        await unitOfWork.CompleteAsync();
        return profile;
    }

    public async Task<WorkerProfile?> Handle(UpdateWorkerProfileCommand c)
    {
        var profile = await repository.FindByUserIdAsync(c.UserId);
        if (profile == null) return null;
        profile.Update(c.Name, c.Phone, c.Age, c.ServiceTypes, c.Zones,
                        c.HourlyRate, c.HourlyRateSunday,
                        c.ExperienceYears, c.Bio);
        repository.Update(profile);
        await unitOfWork.CompleteAsync();
        return profile;
    }

    public async Task<WorkerProfile?> Handle(RegisterWorkerCompletedServiceCommand c)
    {
        var profile = await repository.FindByUserIdAsync(c.UserId);
        if (profile == null) return null;
        profile.RegisterCompletedService(c.Rating);
        repository.Update(profile);
        await unitOfWork.CompleteAsync();
        return profile;
    }

    public async Task<WorkerProfile?> Handle(UpdateWorkerPhotoCommand c)
    {
        var profile = await repository.FindByUserIdAsync(c.UserId);
        if (profile == null) return null;
        profile.SetPhoto(c.PhotoUrl);
        repository.Update(profile);
        await unitOfWork.CompleteAsync();
        return profile;
    }
}
