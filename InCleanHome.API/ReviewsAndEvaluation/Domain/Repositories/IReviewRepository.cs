using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;

public interface IReviewRepository : IBaseRepository<Review>
{
    Task<IEnumerable<Review>> FindByWorkerIdAsync(int workerId);
    Task<Review?> FindByBookingIdAsync(int bookingId);
}
