using Application.Abstractions.Messaging;
using Domain.Todos;

namespace Application.Todos.Create;

public sealed record CreateTodoCommand : ICommand<Guid>
{
    public Guid UserId { get; init; }
    public string Description { get; init; }
    public DateTime? DueDate { get; init; }
    public List<string> Labels { get; init; } = [];
    public Priority Priority { get; init; }
}
