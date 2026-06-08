using InCleanHome.API.IAM.Application.Internal.OutboundServices;
using InCleanHome.API.IAM.Domain.Model.Queries;
using InCleanHome.API.IAM.Domain.Services;
using InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Attributes;

namespace InCleanHome.API.IAM.Infrastructure.Pipeline.Middleware.Components;

/// <summary>
///     Resolves the authenticated user from the JWT and attaches it to HttpContext.Items["User"].
///     Endpoints decorated with <see cref="AllowAnonymousAttribute"/> bypass this check.
/// </summary>
public class RequestAuthorizationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext context,
        IUserQueryService userQueryService,
        ITokenService tokenService)
    {
        var endpoint = context.Request.HttpContext.GetEndpoint();
        var allowAnonymous = endpoint?.Metadata
            .Any(m => m.GetType() == typeof(AllowAnonymousAttribute)) ?? false;

        if (allowAnonymous)
        {
            await next(context);
            return;
        }

        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        if (string.IsNullOrEmpty(token))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Missing or invalid token" });
            return;
        }

        var userId = await tokenService.ValidateToken(token);
        if (userId == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
            return;
        }

        var user = await userQueryService.Handle(new GetUserByIdQuery(userId.Value));
        if (user == null)
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "User not found" });
            return;
        }

        context.Items["User"] = user;
        await next(context);
    }
}
