using Application.Abstractions.Messaging;
using Application.Todos.Get;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Todos;

internal sealed class Get : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("todos", async (
            Guid userId,
            IQueryHandler<GetTodosQuery, List<TodoResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var query = new GetTodosQuery(userId);

            Result<List<TodoResponse>> result = await handler.Handle(query, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Todos)
        .WithSummary("List todos for a user")
        .WithDescription("Returns the todos belonging to the user ID supplied in the query string.")
        .Produces<List<TodoResponse>>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
