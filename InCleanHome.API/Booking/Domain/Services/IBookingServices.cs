using InCleanHome.API.Booking.Domain.Model.Aggregates;
using InCleanHome.API.Booking.Domain.Model.Commands;
using InCleanHome.API.Booking.Domain.Model.Queries;

namespace InCleanHome.API.Booking.Domain.Services;

public interface IBookingRequestCommandService
{
    Task<BookingRequest> Handle(CreateBookingCommand command);
    Task<BookingRequest?> Handle(UpdateBookingStatusCommand command);
    Task<BookingRequest?> Handle(RescheduleBookingCommand command);
}

public interface IBookingRequestQueryService
{
    Task<BookingRequest?> Handle(GetBookingByIdQuery query);
    Task<IEnumerable<BookingRequest>> Handle(GetBookingsByClientUserIdQuery query);
    Task<IEnumerable<BookingRequest>> Handle(GetBookingsByWorkerUserIdQuery query);
}
