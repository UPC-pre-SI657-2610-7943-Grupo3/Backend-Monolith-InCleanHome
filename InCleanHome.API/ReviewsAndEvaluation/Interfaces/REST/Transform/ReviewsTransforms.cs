using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;

namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Transform;

public static class ReviewResourceFromEntityAssembler
{
    public static ReviewResource ToResourceFromEntity(Review r, string clientName)
        => new(r.Id, r.BookingId, r.ClientId, r.WorkerId, clientName, r.Rating, r.Comment, r.CreatedDate);
}
