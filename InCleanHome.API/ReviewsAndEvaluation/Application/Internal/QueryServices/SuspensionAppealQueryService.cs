using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;

namespace InCleanHome.API.ReviewsAndEvaluation.Application.Internal.QueryServices;

public class SuspensionAppealQueryService(ISuspensionAppealRepository repository)
    : ISuspensionAppealQueryService
{
    public async Task<SuspensionAppeal?> Handle(GetSuspensionAppealByIdQuery query)
        => await repository.FindByIdAsync(query.Id);

    public async Task<SuspensionAppeal?> Handle(GetActiveSuspensionAppealByUserIdQuery query)
        => await repository.FindActiveByUserIdAsync(query.UserId);

    public async Task<IEnumerable<SuspensionAppeal>> Handle(GetSuspensionAppealsByUserIdQuery query)
        => await repository.FindAllByUserIdAsync(query.UserId);

    public async Task<IEnumerable<SuspensionAppeal>> Handle(GetPendingSuspensionAppealsQuery query)
        => await repository.FindPendingAsync();
}
