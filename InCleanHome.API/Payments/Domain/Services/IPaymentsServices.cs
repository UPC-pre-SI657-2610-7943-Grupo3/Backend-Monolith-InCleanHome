using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Commands;
using InCleanHome.API.Payments.Domain.Model.Queries;

namespace InCleanHome.API.Payments.Domain.Services;

public interface IPaymentMethodCommandService
{
    Task<PaymentMethod> Handle(RegisterPaymentMethodCommand command);
    Task<PaymentMethod?> Handle(SetDefaultPaymentMethodCommand command);
    Task<bool> Handle(DeletePaymentMethodCommand command);
}

public interface IPaymentMethodQueryService
{
    Task<IEnumerable<PaymentMethod>> Handle(GetPaymentMethodsByUserIdQuery query);
    Task<PaymentMethod?> Handle(GetPaymentMethodByIdQuery query);
}
