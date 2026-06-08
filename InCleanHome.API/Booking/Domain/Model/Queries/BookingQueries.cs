namespace InCleanHome.API.Booking.Domain.Model.Queries;

public record GetBookingByIdQuery(int Id);
public record GetBookingsByClientUserIdQuery(int ClientUserId);
public record GetBookingsByWorkerUserIdQuery(int WorkerUserId);
