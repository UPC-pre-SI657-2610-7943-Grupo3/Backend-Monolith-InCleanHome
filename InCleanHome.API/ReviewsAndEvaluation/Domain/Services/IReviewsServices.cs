using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Services;

public interface IReviewCommandService
{
    Task<Review> Handle(CreateReviewCommand command);
}

public interface IReviewQueryService
{
    Task<IEnumerable<Review>> Handle(GetReviewsByWorkerIdQuery query);
    Task<Review?> Handle(GetReviewByBookingIdQuery query);
}
