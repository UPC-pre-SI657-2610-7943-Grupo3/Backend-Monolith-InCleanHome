using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.ReviewsAndEvaluation.Infrastructure.Persistence.EFC.Repositories;

public class ReviewRepository(AppDbContext context)
    : BaseRepository<Review>(context), IReviewRepository
{
    public async Task<IEnumerable<Review>> FindByWorkerIdAsync(int workerId)
        => await Context.Set<Review>()
            .Where(r => r.WorkerId == workerId)
            .OrderByDescending(r => r.CreatedDate)
            .ToListAsync();

    public async Task<Review?> FindByBookingIdAsync(int bookingId)
        => await Context.Set<Review>().FirstOrDefaultAsync(r => r.BookingId == bookingId);
}
