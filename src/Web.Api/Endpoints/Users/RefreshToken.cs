using Application.Abstractions.Messaging;
using Application.Users;
using Application.Users.Refresh;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class RefreshToken : IEndpoint
{
    public sealed record Request(string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/refresh-token", async (
            Request request,
            ICommandHandler<RefreshTokenCommand, AccessTokensResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new RefreshTokenCommand(request.RefreshToken);

            Result<AccessTokensResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .WithSummary("Refresh access tokens")
        .WithDescription("Exchanges a valid refresh token for a new access token and refresh token.")
        .Produces<AccessTokensResponse>(StatusCodes.Status200OK)
        .RequireRateLimiting(RateLimitingPolicies.Authentication);
    }
}
