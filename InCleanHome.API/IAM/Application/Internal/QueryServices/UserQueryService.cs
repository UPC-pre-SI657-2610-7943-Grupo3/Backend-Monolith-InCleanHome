using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Queries;
using InCleanHome.API.IAM.Domain.Repositories;
using InCleanHome.API.IAM.Domain.Services;

namespace InCleanHome.API.IAM.Application.Internal.QueryServices;

public class UserQueryService(
    IUserRepository userRepository,
    IWorkerDocumentRepository workerDocumentRepository) : IUserQueryService
{
    public async Task<User?> Handle(GetUserByIdQuery query)        => await userRepository.FindByIdAsync(query.Id);
    public async Task<User?> Handle(GetUserByEmailQuery query)     => await userRepository.FindByEmailAsync(query.Email);
    public async Task<IEnumerable<User>> Handle(GetAllUsersQuery query) => await userRepository.ListAsync();

    public async Task<IEnumerable<WorkerDocument>> Handle(GetWorkerDocumentsByUserIdQuery query)
        => await workerDocumentRepository.FindByUserIdAsync(query.UserId);
}
