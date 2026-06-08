using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Booking.Domain.Repositories;

public interface IBookingRequestRepository : IBaseRepository<BookingRequest>
{
    Task<IEnumerable<BookingRequest>> FindByClientUserIdAsync(int clientUserId);
    Task<IEnumerable<BookingRequest>> FindByWorkerUserIdAsync(int workerUserId);
    /// <summary>
    ///     Returns any active booking for the given worker on the given date whose
    ///     time range overlaps with [startTime, endTime]. Used to prevent double-booking.
    /// </summary>
    Task<IEnumerable<BookingRequest>> FindWorkerOverlappingAsync(int workerUserId, DateOnly date, string startTime, string endTime);
}
