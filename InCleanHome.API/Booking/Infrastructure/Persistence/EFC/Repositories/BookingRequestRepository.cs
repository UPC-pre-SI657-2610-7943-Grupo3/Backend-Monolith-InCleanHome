using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Configuration;
using InCleanHome.API.Shared.Infrastructure.Persistence.EFC.Repositories;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.API.Booking.Infrastructure.Persistence.EFC.Repositories;

public class BookingRequestRepository(AppDbContext context)
    : BaseRepository<BookingRequest>(context), IBookingRequestRepository
{
    public async Task<IEnumerable<BookingRequest>> FindByClientUserIdAsync(int clientUserId)
        => await Context.Set<BookingRequest>()
            .Where(b => b.ClientId == clientUserId)
            .OrderByDescending(b => b.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<BookingRequest>> FindByWorkerUserIdAsync(int workerUserId)
        => await Context.Set<BookingRequest>()
            .Where(b => b.WorkerId == workerUserId)
            .OrderByDescending(b => b.CreatedDate)
            .ToListAsync();

    public async Task<IEnumerable<BookingRequest>> FindWorkerOverlappingAsync(
        int workerUserId, DateOnly date, string startTime, string endTime)
    {
        // Only active bookings (pending or accepted) can block a new slot.
        // We fetch all active bookings for this worker/date and filter in memory
        // using ordinal string comparison (times are stored as "HH:mm" so lexicographic == chronological).
        var activeStatuses = new[] { "pending", "accepted" };

        var dayBookings = await Context.Set<BookingRequest>()
            .Where(b =>
                b.WorkerId == workerUserId &&
                b.Date == date &&
                activeStatuses.Contains(b.Status))
            .ToListAsync();

        // Overlap condition: existing.start < new.end AND existing.end > new.start
        return dayBookings.Where(b =>
            string.Compare(b.StartTime, endTime,   StringComparison.Ordinal) < 0 &&
            string.Compare(b.EndTime,   startTime, StringComparison.Ordinal) > 0);
    }
}
