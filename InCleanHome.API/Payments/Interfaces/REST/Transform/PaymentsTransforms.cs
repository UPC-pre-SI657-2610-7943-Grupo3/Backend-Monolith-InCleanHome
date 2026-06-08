using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Interfaces.REST.Resources;

namespace InCleanHome.API.Payments.Interfaces.REST.Transform;

public static class PaymentMethodResourceFromEntityAssembler
{
    public static PaymentMethodResource ToResourceFromEntity(PaymentMethod p)
        => new(p.Id, p.Type, p.Label, p.Details, p.IsDefault);
}
