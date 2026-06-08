using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Payments.Domain.Model.Queries;
using InCleanHome.API.Payments.Domain.Repositories;
using InCleanHome.API.Payments.Domain.Services;

namespace InCleanHome.API.Payments.Application.Internal.QueryServices;

public class PaymentMethodQueryService(IPaymentMethodRepository repository) : IPaymentMethodQueryService
{
    public async Task<IEnumerable<PaymentMethod>> Handle(GetPaymentMethodsByUserIdQuery query)
        => await repository.FindByUserIdAsync(query.UserId);

    public async Task<PaymentMethod?> Handle(GetPaymentMethodByIdQuery query)
        => await repository.FindByIdAsync(query.Id);
}
