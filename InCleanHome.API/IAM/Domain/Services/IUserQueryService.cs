using InCleanHome.API.IAM.Domain.Model.Aggregates;
using InCleanHome.API.IAM.Domain.Model.Queries;

namespace InCleanHome.API.IAM.Domain.Services;

public interface IUserQueryService
{
    Task<User?> Handle(GetUserByIdQuery query);
    Task<User?> Handle(GetUserByEmailQuery query);
    Task<IEnumerable<User>> Handle(GetAllUsersQuery query);
    Task<IEnumerable<WorkerDocument>> Handle(GetWorkerDocumentsByUserIdQuery query);
}
