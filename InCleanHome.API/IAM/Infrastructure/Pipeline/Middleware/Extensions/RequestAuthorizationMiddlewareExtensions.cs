using InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Components;

namespace InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Extensions;

public static class RequestAuthorizationMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestAuthorization(this IApplicationBuilder builder)
        => builder.UseMiddleware<RequestAuthorizationMiddleware>();
}
