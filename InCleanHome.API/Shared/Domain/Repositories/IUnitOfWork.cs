namespace InCleanHome.API.Shared.Domain.Repositories;

/// <summary>
///     Unit of Work — commits all pending changes across repositories in a single transaction.
/// </summary>
public interface IUnitOfWork
{
    Task CompleteAsync();
}
