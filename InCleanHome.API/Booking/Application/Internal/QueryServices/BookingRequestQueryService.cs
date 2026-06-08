using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Booking.Domain.Model.Queries;
using InCleanHome.API.Booking.Domain.Repositories;
using InCleanHome.API.Booking.Domain.Services;

namespace InCleanHome.API.Booking.Application.Internal.QueryServices;

public class BookingRequestQueryService(IBookingRequestRepository repository) : IBookingRequestQueryService
{
    public async Task<BookingRequest?> Handle(GetBookingByIdQuery query)
        => await repository.FindByIdAsync(query.Id);

    public async Task<IEnumerable<BookingRequest>> Handle(GetBookingsByClientUserIdQuery query)
        => await repository.FindByClientUserIdAsync(query.ClientUserId);

    public async Task<IEnumerable<BookingRequest>> Handle(GetBookingsByWorkerUserIdQuery query)
        => await repository.FindByWorkerUserIdAsync(query.WorkerUserId);
}
