using Application.Abstractions.Messaging;
using Application.Todos.GetById;
using SharedKernel;
using Web.Api.Extensions;
using Web.Api.Infrastructure;

namespace Web.Api.Endpoints.Todos;

internal sealed class GetById : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("todos/{id:guid}", async (
            Guid id,
            IQueryHandler<GetTodoByIdQuery, TodoResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var command = new GetTodoByIdQuery(id);

            Result<TodoResponse> result = await handler.Handle(command, cancellationToken);

            return result.Match(Results.Ok, CustomResults.Problem);
        })
        .WithTags(Tags.Todos)
        .WithSummary("Get a todo by ID")
        .WithDescription("Returns the todo with the specified ID.")
        .Produces<TodoResponse>(StatusCodes.Status200OK)
        .RequireAuthorization();
    }
}
