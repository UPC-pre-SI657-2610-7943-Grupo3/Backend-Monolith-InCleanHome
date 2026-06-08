using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Queries;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;

namespace InCleanHome.API.ReviewsAndEvaluation.Application.Internal.QueryServices;

public class ReviewQueryService(IReviewRepository repository) : IReviewQueryService
{
    public async Task<IEnumerable<Review>> Handle(GetReviewsByWorkerIdQuery query)
        => await repository.FindByWorkerIdAsync(query.WorkerId);

    public async Task<Review?> Handle(GetReviewByBookingIdQuery query)
        => await repository.FindByBookingIdAsync(query.BookingId);
}
