using InCleanHome.API.Profiles.Domain.Model.Aggregates;
using InCleanHome.API.Profiles.Domain.Model.Queries;
using InCleanHome.API.Profiles.Domain.Repositories;
using InCleanHome.API.Profiles.Domain.Services;

namespace InCleanHome.API.Profiles.Application.Internal.QueryServices;

public class ClientProfileQueryService(IClientProfileRepository repository) : IClientProfileQueryService
{
    public async Task<ClientProfile?> Handle(GetClientProfileByUserIdQuery query)
        => await repository.FindByUserIdAsync(query.UserId);
}

public class WorkerProfileQueryService(IWorkerProfileRepository repository) : IWorkerProfileQueryService
{
    public async Task<WorkerProfile?> Handle(GetWorkerProfileByUserIdQuery query)
        => await repository.FindByUserIdAsync(query.UserId);

    public async Task<WorkerProfile?> Handle(GetWorkerProfileByIdQuery query)
        => await repository.FindByIdAsync(query.Id);

    public async Task<IEnumerable<WorkerProfile>> Handle(GetAllWorkerProfilesQuery query)
        => await repository.ListAsync();

    public async Task<IEnumerable<WorkerProfile>> Handle(SearchWorkersQuery query)
        => await repository.SearchAsync(query);
}
