using InCleanHome.API.Payments.Domain.Model.Aggregates;
using InCleanHome.API.Shared.Domain.Repositories;

namespace InCleanHome.API.Payments.Domain.Repositories;

public interface IPaymentMethodRepository : IBaseRepository<PaymentMethod>
{
    Task<IEnumerable<PaymentMethod>> FindByUserIdAsync(int userId);
    Task<PaymentMethod?> FindDefaultByUserIdAsync(int userId);
}
