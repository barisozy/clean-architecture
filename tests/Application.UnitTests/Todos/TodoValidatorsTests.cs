/// <summary>
/// BOM (Byte Order Mark) is intentionally omitted.
/// </summary>
using Application.Todos.Create;
using Application.Todos.Update;
using Domain.Todos;
using FluentValidation.TestHelper;
using NSubstitute;
using SharedKernel;

namespace Application.UnitTests.Todos;

public sealed class TodoValidatorsTests
{
    private static readonly DateTime Now = new(2030, 1, 15, 10, 30, 0, DateTimeKind.Utc);

    private readonly CreateTodoCommandValidator _createValidator;
    private readonly UpdateTodoCommandValidator _updateValidator = new();

    public TodoValidatorsTests()
    {
        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(Now);

        _createValidator = new CreateTodoCommandValidator(dateTimeProvider);
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenDescriptionIsEmpty()
    {
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = string.Empty,
            Priority = Priority.Low
        };

        TestValidationResult<CreateTodoCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void CreateValidator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new CreateTodoCommand
        {
            UserId = Guid.NewGuid(),
            Description = "Buy groceries",
            Priority = Priority.Medium,
            Labels = ["home"]
        };

        TestValidationResult<CreateTodoCommand> result = _createValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CreateValidator_Should_HaveError_WhenDueDateIsBeforeToday()
    {
        CreateTodoCommand command = ValidCommand(Now.Date.AddTicks(-1));

        TestValidationResult<CreateTodoCommand> result = _createValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.DueDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void CreateValidator_Should_NotHaveError_WhenDueDateIsTodayOrLater(int daysFromToday)
    {
        CreateTodoCommand command = ValidCommand(Now.Date.AddDays(daysFromToday));

        TestValidationResult<CreateTodoCommand> result = _createValidator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.DueDate);
    }

    [Fact]
    public void UpdateValidator_Should_HaveError_WhenDescriptionExceedsMaxLength()
    {
        var command = new UpdateTodoCommand(Guid.NewGuid(), new string('a', 501));

        TestValidationResult<UpdateTodoCommand> result = _updateValidator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Description);
    }

    [Fact]
    public void UpdateValidator_Should_NotHaveErrors_WhenCommandIsValid()
    {
        var command = new UpdateTodoCommand(Guid.NewGuid(), "Updated description");

        TestValidationResult<UpdateTodoCommand> result = _updateValidator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static CreateTodoCommand ValidCommand(DateTime dueDate) => new()
    {
        UserId = Guid.NewGuid(),
        Description = "Buy groceries",
        Priority = Priority.Medium,
        DueDate = dueDate
    };
}
