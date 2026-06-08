namespace InCleanHome.API.Payments.Domain.Model.Queries;

public record GetPaymentMethodsByUserIdQuery(int UserId);
public record GetPaymentMethodByIdQuery(int Id);
