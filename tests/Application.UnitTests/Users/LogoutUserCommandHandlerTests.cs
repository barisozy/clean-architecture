using Application.Abstractions.Authentication;
using Application.UnitTests.Abstractions;
using Application.Users;
using Application.Users.Logout;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class LogoutUserCommandHandlerTests : BaseHandlerTest
{
    [Fact]
    public async Task Handle_Should_Succeed_WhenTokenIsUnknown()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var handler = new LogoutUserCommandHandler(
            context,
            Substitute.For<IUserContext>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result result = await handler.Handle(new LogoutUserCommand("unknown-token"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_Should_NotRevokeAnything_WhenTokenBelongsToAnotherUser()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        var token = RefreshToken.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            RefreshTokenHasher.Hash("token"),
            DateTime.UtcNow);
        context.RefreshTokens.Add(token);
        await context.SaveChangesAsync();

        IUserContext userContext = Substitute.For<IUserContext>();
        userContext.UserId.Returns(Guid.NewGuid());

        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new LogoutUserCommandHandler(context, userContext, dateTimeProvider);

        // Act
        Result result = await handler.Handle(new LogoutUserCommand("token"), CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        RefreshToken stored = await context.RefreshTokens.SingleAsync(rt => rt.Id == token.Id);
        stored.RevokedOnUtc.ShouldBeNull();
    }
}
