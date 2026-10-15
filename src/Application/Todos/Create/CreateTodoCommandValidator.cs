using FluentValidation;
using SharedKernel;

namespace Application.Todos.Create;

public class CreateTodoCommandValidator : AbstractValidator<CreateTodoCommand>
{
    public CreateTodoCommandValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(c => c.UserId).NotEmpty();
        RuleFor(c => c.Priority).IsInEnum();
        RuleFor(c => c.Description).NotEmpty().MaximumLength(255);
        RuleFor(c => c.DueDate)
            .Must(dueDate => dueDate >= dateTimeProvider.UtcNow.Date)
            .WithMessage("'Due Date' must not be in the past.")
            .When(c => c.DueDate.HasValue);
    }
}
