namespace InCleanHome.API.Payments.Interfaces.REST.Resources;

public record RegisterPaymentMethodResource(string Type, string Label, string? Details, bool IsDefault);

public record PaymentMethodResource(int Id, string Type, string Label, string Details, bool IsDefault);
