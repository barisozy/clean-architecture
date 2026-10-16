using Application.Abstractions.Authentication;
using Application.Users.Register;
using Application.UnitTests.Abstractions;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SharedKernel;

namespace Application.UnitTests.Users;

public sealed class RegisterUserCommandHandlerTests : BaseHandlerTest
{
    private static RegisterUserCommand Command =>
        new("test@example.com", "Test", "User", "Password123");

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenEmailIsNotUnique()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();
        context.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            Email = Command.Email,
            FirstName = "Existing",
            LastName = "User",
            PasswordHash = "hash"
        });
        await context.SaveChangesAsync();

        var handler = new RegisterUserCommandHandler(context, Substitute.For<IPasswordHasher>());

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.EmailNotUnique);
    }

    [Fact]
    public async Task Handle_Should_CreateUserWithHashedPasswordAndRaiseDomainEvent_WhenValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash(Command.Password).Returns("hashed-password");

        var handler = new RegisterUserCommandHandler(context, passwordHasher);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        User user = await context.Users.SingleAsync(u => u.Id == result.Value);
        user.Email.ShouldBe(Command.Email);
        user.PasswordHash.ShouldBe("hashed-password");
        user.DomainEvents.ShouldContain(domainEvent => domainEvent is UserRegisteredDomainEvent);
    }

    [Fact]
    public async Task Handle_Should_GrantDefaultPermissions_WhenValid()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash(Command.Password).Returns("hashed-password");

        var handler = new RegisterUserCommandHandler(context, passwordHasher);

        // Act
        Result<Guid> result = await handler.Handle(Command, CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        string[] permissions = await context.UserPermissions
            .Where(p => p.UserId == result.Value)
            .Select(p => p.Permission)
            .ToArrayAsync();

        permissions.ShouldBe(Permissions.DefaultForNewUsers, ignoreOrder: true);
    }

    [Fact]
    public async Task Handle_Should_StoreEmailInLowerCase_WhenRegistered()
    {
        // Arrange
        await using TestDbContext context = CreateDbContext();

        IPasswordHasher passwordHasher = Substitute.For<IPasswordHasher>();
        passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        var handler = new RegisterUserCommandHandler(context, passwordHasher);

        // Act
        Result<Guid> result = await handler.Handle(
            new RegisterUserCommand("Test.User@Example.COM", "Test", "User", "Password123"),
            CancellationToken.None);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        User user = await context.Users.SingleAsync(u => u.Id == result.Value);
        user.Email.ShouldBe("test.user@example.com");
    }

    [Fact]
    public async Task Handle_Should_ReturnConflict_WhenConcurrentRegistrationWinsTheRace()
    {
        // Arrange
        await using RacingDbContext context = CreateRacingDbContext(conflictEmail: "race@example.com");
        var handler = new RegisterUserCommandHandler(context, Substitute.For<IPasswordHasher>());

        // Act
        Result<Guid> result = await handler.Handle(
            new RegisterUserCommand("race@example.com", "Test", "User", "Password123"),
            CancellationToken.None);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.EmailNotUnique);
    }

    [Fact]
    public async Task Handle_Should_Rethrow_WhenSaveFailsForAnotherReason()
    {
        // Arrange
        await using RacingDbContext context = CreateRacingDbContext(conflictEmail: null);
        var handler = new RegisterUserCommandHandler(context, Substitute.For<IPasswordHasher>());

        // Act & Assert
        await Should.ThrowAsync<DbUpdateException>(() => handler.Handle(
            new RegisterUserCommand("race@example.com", "Test", "User", "Password123"),
            CancellationToken.None));
    }

    private static RacingDbContext CreateRacingDbContext(string? conflictEmail)
    {
        DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase($"clean-architecture-{Guid.NewGuid()}")
            .Options;

        return new RacingDbContext(options, conflictEmail);
    }

    /// <summary>
    /// Simulates the losing side of a concurrent registration: the competing request
    /// commits first and our <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>
    /// fails on the unique email index.
    /// </summary>
    private sealed class RacingDbContext(DbContextOptions<TestDbContext> options, string? conflictEmail)
        : TestDbContext(options)
    {
        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (EntityEntry<User> entry in ChangeTracker.Entries<User>().ToList())
            {
                entry.State = EntityState.Detached;
            }

            if (conflictEmail is not null)
            {
                Users.Add(new User
                {
                    Id = Guid.NewGuid(),
                    Email = conflictEmail,
                    FirstName = "Race",
                    LastName = "Winner",
                    PasswordHash = "hash"
                });

                await base.SaveChangesAsync(cancellationToken);
            }

            throw new DbUpdateException("duplicate key value violates unique constraint \"ix_users_email\"");
        }
    }
}
