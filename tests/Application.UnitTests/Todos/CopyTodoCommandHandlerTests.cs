using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.UnitTests.Abstractions;
using Application.Todos.Copy;
using Domain.Todos;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Todos;

public sealed class CopyTodoCommandHandlerTests : BaseHandlerTest
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Handle_Should_RaiseCreatedEventWithIdOfCopiedItem_WhenValid()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var todoId = Guid.NewGuid();
        await using TestDbContext context = CreateDbContext();
        context.Users.Add(new User
        {
            Id = userId,
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });
        context.TodoItems.Add(new TodoItem
        {
            Id = todoId,
            UserId = userId,
            Description = "Original",
            Priority = Priority.High,
            Labels = ["work"],
            IsCompleted = true,
            CreatedAt = Now.AddDays(-1)
        });
        await context.SaveChangesAsync();

        CopyTodoCommandHandler handler = CreateHandler(context, userId);

        // Act
        Result<Guid> result = await handler.Handle(
            new CopyTodoCommand { UserId = userId, TodoId = todoId },
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);
        result.Value.ShouldNotBe(todoId);

        TodoItem copy = await context.TodoItems.SingleAsync(t => t.Id == result.Value);
        copy.IsCompleted.ShouldBeFalse();

        TodoItemCreatedDomainEvent raised = copy.DomainEvents
            .OfType<TodoItemCreatedDomainEvent>()
            .Single();
        raised.TodoItemId.ShouldBe(copy.Id);
    }

    [Fact]
    public async Task Handle_Should_ReturnForbidden_WhenCallerIsNotTheOwner()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        CopyTodoCommandHandler handler = CreateHandler(context, Guid.NewGuid());

        // Act
        Result<Guid> result = await handler.Handle(
            new CopyTodoCommand { UserId = Guid.NewGuid(), TodoId = Guid.NewGuid() },
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.Forbidden());
    }

    private static CopyTodoCommandHandler CreateHandler(IApplicationDbContext context, Guid callerId)
    {
        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(callerId);

        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(Now);

        return new CopyTodoCommandHandler(context, dateTimeProvider, userContext);
    }
}
