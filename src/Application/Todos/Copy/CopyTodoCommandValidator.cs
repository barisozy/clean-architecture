using FluentValidation;

namespace Application.Todos.Copy;

internal sealed class CopyTodoCommandValidator : AbstractValidator<CopyTodoCommand>
{
    public CopyTodoCommandValidator()
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.TodoId).NotEmpty();
    }
}
