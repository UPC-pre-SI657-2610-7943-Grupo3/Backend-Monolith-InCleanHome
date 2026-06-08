using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Commands;
using InCleanHome.API.Profiles.Domain.Model.Queries;

namespace InCleanHome.API.Profiles.Domain.Services;

public interface IClientProfileCommandService
{
    Task<ClientProfile> Handle(CreateClientProfileCommand command);
    Task<ClientProfile?> Handle(UpdateClientProfileCommand command);
    Task<ClientProfile?> Handle(UpdateClientPhotoCommand command);
}

public interface IClientProfileQueryService
{
    Task<ClientProfile?> Handle(GetClientProfileByUserIdQuery query);
}

public interface IWorkerProfileCommandService
{
    Task<WorkerProfile> Handle(CreateWorkerProfileCommand command);
    Task<WorkerProfile?> Handle(UpdateWorkerProfileCommand command);
    Task<WorkerProfile?> Handle(RegisterWorkerCompletedServiceCommand command);
    Task<WorkerProfile?> Handle(UpdateWorkerPhotoCommand command);
}

public interface IWorkerProfileQueryService
{
    Task<WorkerProfile?> Handle(GetWorkerProfileByUserIdQuery query);
    Task<WorkerProfile?> Handle(GetWorkerProfileByIdQuery query);
    Task<IEnumerable<WorkerProfile>> Handle(GetAllWorkerProfilesQuery query);
    Task<IEnumerable<WorkerProfile>> Handle(SearchWorkersQuery query);
}
