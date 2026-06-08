namespace InCleanHome.API.ReviewsAndEvaluation.Interfaces.REST.Resources;

public record CreateReviewResource(int BookingId, int WorkerId, int Rating, string? Comment);

public record ReviewResource(
    int Id,
    int BookingId,
    int ClientId,
    int WorkerId,
    string ClientName,
    int Rating,
    string Comment,
    DateTimeOffset? CreatedAt);
