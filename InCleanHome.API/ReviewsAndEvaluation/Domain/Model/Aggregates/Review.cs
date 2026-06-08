using System.ComponentModel.DataAnnotations.Schema;
using EntityFrameworkCore.CreatedUpdatedDate.Contracts;

namespace InCleanHome.API.ReviewsAndEvaluation.Domain.Model.Aggregates;

/// <summary>
///     Review aggregate root — a 1-5 rating + comment posted by a client about a completed booking.
/// </summary>
public class Review : IEntityWithCreatedUpdatedDate
{
    public int Id { get; private set; }
    public int BookingId { get; private set; }
    public int ClientId { get; private set; }
    public int WorkerId { get; private set; }
    public int Rating { get; private set; }
    public string Comment { get; private set; } = string.Empty;

    [Column("CreatedAt")] public DateTimeOffset? CreatedDate { get; set; }
    [Column("UpdatedAt")] public DateTimeOffset? UpdatedDate { get; set; }

    public Review() { }

    public Review(int bookingId, int clientId, int workerId, int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        BookingId = bookingId;
        ClientId  = clientId;
        WorkerId  = workerId;
        Rating    = rating;
        Comment   = comment ?? string.Empty;
    }
}
