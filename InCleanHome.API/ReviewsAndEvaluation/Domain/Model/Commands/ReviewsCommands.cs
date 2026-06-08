namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Commands;

public record CreateReviewCommand(int BookingId, int ClientId, int WorkerId, int Rating, string? Comment);
