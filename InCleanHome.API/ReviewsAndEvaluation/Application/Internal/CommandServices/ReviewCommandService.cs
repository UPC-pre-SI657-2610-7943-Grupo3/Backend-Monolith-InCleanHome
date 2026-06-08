using InCleanHome.API.Profiles.Interfaces.ACL;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Repositories;
using InCleanHome.API.ReviewsAndEvaluation.Domain.Services;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.ReviewsAndEvaluation.Application.Internal.CommandServices;

public class ReviewCommandService(
    IReviewRepository repository,
    IProfilesContextFacade profilesFacade,
    IUnitOfWork unitOfWork) : IReviewCommandService
{
    public async Task<Review> Handle(CreateReviewCommand c)
    {
        var existing = await repository.FindByBookingIdAsync(c.BookingId);
        if (existing is not null)
            throw new Exception("This booking has already been reviewed.");

        var review = new Review(c.BookingId, c.ClientId, c.WorkerId, c.Rating, c.Comment);
        await repository.AddAsync(review);
        await unitOfWork.CompleteAsync();

        // Cross-BC side-effect: refresh worker stats. Done via ACL facade so this BC never
        // touches Profiles aggregates directly.
        await profilesFacade.RegisterWorkerCompletedService(c.WorkerId, c.Rating);

        return review;
    }
}
