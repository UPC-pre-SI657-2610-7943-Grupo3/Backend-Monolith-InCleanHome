using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Services;

public interface ISuspensionAppealCommandService
{
    Task<SuspensionAppeal> Handle(SubmitSuspensionAppealCommand command);
    Task<SuspensionAppeal?> Handle(AcceptSuspensionAppealCommand command);
    Task<SuspensionAppeal?> Handle(RejectSuspensionAppealCommand command);
}

public interface ISuspensionAppealQueryService
{
    Task<SuspensionAppeal?> Handle(GetSuspensionAppealByIdQuery query);
    Task<SuspensionAppeal?> Handle(GetActiveSuspensionAppealByUserIdQuery query);
    Task<IEnumerable<SuspensionAppeal>> Handle(GetSuspensionAppealsByUserIdQuery query);
    Task<IEnumerable<SuspensionAppeal>> Handle(GetPendingSuspensionAppealsQuery query);
}
