using Domain.Todos;
using FluentValidation;
using SharedKernel;

namespace Application.Todos.Create;

internal sealed class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Priority).IsInEnum();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(TodoItem.MaxDescriptionLength);
        RuleFor(c => c.DueDate)
            .Must(dueDate => dueDate >= dateTimeProvider.UtcNow.Date)
            .WithMessage("'Due Date' must not be in the past.")
            .When(c => c.DueDate.HasValue);
    }
}
