using Application.Abstractions.Messaging;

namespace Application.Todos.Copy;

public sealed record CopyTodoCommand : ICommand<Guid>
{
    public Guid UserId { get; init; }
    public Guid TodoId { get; init; }
}
