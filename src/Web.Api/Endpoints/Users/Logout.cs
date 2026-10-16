using Application.Abstractions.Messaging;
using Application.Users.Logout;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Users;

internal sealed class Logout : IEndpoint
{
    public sealed record Request(string RefreshToken);

    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("users/logout", async (
            Request request,
            ICommandHandler<LogoutUserCommand> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new LogoutUserCommand(request.RefreshToken);

            Result result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.NoContent, CustomResults.Problem);
        })
        .WithTags(Tags.Users)
        .WithSummary("Log out")
        .WithDescription("Revokes the refresh token family of the supplied refresh token. Access tokens stay valid until they expire.")
        .Produces(StatusCodes.Status204NoContent)
        .RequireAuthorization()
        .RequireRateLimiting(RateLimitingPolicies.Authentication);
    }
}
