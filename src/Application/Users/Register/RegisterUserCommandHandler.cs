using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Register;

internal sealed class RegisterUserCommandHandler(IApplicationDbContext context, IPasswordHasher passwordHasher)
    : ICommandHandler<RegisterUserCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        // Emails are stored normalized so lookups are case-insensitive.
        string email = EmailNormalizer.Normalize(command.Email);

        if (await context.Users.AnyAsync(u => u.Email == email, cancellationToken))
        {
            return Result.Failure<Guid>(UserErrors.EmailNotUnique);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FirstName = command.FirstName,
            LastName = command.LastName,
            PasswordHash = passwordHasher.Hash(command.Password)
        };

        user.Raise(new UserRegisteredDomainEvent(user.Id));

        context.Users.Add(user);

        foreach (string permission in Permissions.DefaultForNewUsers)
        {
            context.UserPermissions.Add(UserPermission.Create(user.Id, permission));
        }

        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Two registrations can pass the pre-check concurrently; the unique index decides.
            if (await context.Users.AnyAsync(u => u.Email == email, cancellationToken))
            {
                return Result.Failure<Guid>(UserErrors.EmailNotUnique);
            }

            throw;
        }

        return user.Id;
    }
}
