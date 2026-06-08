using InCleanHome.API.IAM.Domain.Model.Aggregates;

namespace InCleanHome.API.IAM.Application.Internal.OutboundServices;

public interface ITokenService
{
    string GenerateToken(User user);
    Task<int?> ValidateToken(string token);
}
