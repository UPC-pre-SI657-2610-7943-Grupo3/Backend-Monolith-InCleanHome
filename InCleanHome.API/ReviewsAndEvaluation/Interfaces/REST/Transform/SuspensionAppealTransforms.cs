using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;

namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Transform;

public static class SuspensionAppealResourceFromEntityAssembler
{
    public static SuspensionAppealResource ToResource(SuspensionAppeal a)
        => new(
            a.Id,
            a.UserId,
            a.Reason,
            a.Status,
            a.ReviewedByAdminUserId,
            a.ReviewedAt,
            a.AdminResponse,
            a.CreatedDate);
}
