using Application.Abstractions.Authentication;
using Application.Users;
using Application.Users.Login;
using Application.UnitTests.Abstractions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class LoginUserCommandHandlerTests : BaseHandlerTest
{
    private const string Email = "test@example.com";
    private const string Password = "Password123";

    [Fact]
    public async Task Handle_Should_StillVerifyAPassword_WhenEmailIsUnknown()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        var handler = new LoginUserCommandHandler(
            context,
            passwordHasher,
            Substitute.For<ITokenProvider>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginUserCommand("unknown@example.com", "Whatever1!"),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFoundByEmail);

        // Same work as the wrong-password path: response time must not reveal whether the user exists.
        passwordHasher.Received(1).Verify(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task Handle_Should_VerifyOnce_WhenPasswordIsWrong()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        await SeedUserAsync(context);

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(false);

        var handler = new LoginUserCommandHandler(
            context,
            passwordHasher,
            Substitute.For<ITokenProvider>(),
            Substitute.For<IDateTimeProvider>());

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginUserCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.NotFoundByEmail);
        passwordHasher.Received(1).Verify(Password, "hash");
    }

    [Fact]
    public async Task Handle_Should_ReturnTokensAndPersistRefreshToken_WhenCredentialsAreValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        await SeedUserAsync(context);

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        ITokenProvider tokenProvider = Substitute.For<ITokenProvider>();
        tokenProvider.Create(Arg.Any<User>()).Returns("access-token");
        tokenProvider.GenerateRefreshToken().Returns("refresh-token");

        IDateTimeProvider dateTimeProvider = Substitute.For<IDateTimeProvider>();
        dateTimeProvider.UtcNow.Returns(DateTime.UtcNow);

        var handler = new LoginUserCommandHandler(context, passwordHasher, tokenProvider, dateTimeProvider);

        // Act
        Result<AccessTokensResponse> result = await handler.Handle(
            new LoginUserCommand(Email, Password),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");

        RefreshToken refreshToken = await context.RefreshTokens.SingleAsync();
        refreshToken.TokenHash.ShouldBe(RefreshTokenHasher.Hash("refresh-token"));
        refreshToken.FamilyId.ShouldNotBe(Guid.Empty);
        refreshToken.CreatedOnUtc.ShouldBe(dateTimeProvider.UtcNow);
        refreshToken.ExpiresOnUtc.ShouldBeGreaterThan(dateTimeProvider.UtcNow);
    }

    private static async Task SeedUserAsync(TestDbContext context)
    {
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = Email,
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash"
        });

        await context.SaveChangesAsync();
    }
}
