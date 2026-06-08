namespace InCleanHome.API.Payments.Domain.Model.Commands;

public record RegisterPaymentMethodCommand(int UserId, string Type, string Label, string? Details, bool IsDefault);
public record SetDefaultPaymentMethodCommand(int UserId, int PaymentMethodId);
public record DeletePaymentMethodCommand(int UserId, int PaymentMethodId);
