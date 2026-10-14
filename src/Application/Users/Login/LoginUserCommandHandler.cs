using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Login;

internal sealed class LoginUserCommandHandler(
    IApplicationDbContext context,
    IPasswordHasher passwordHasher,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LoginUserCommand, AccessTokensResponse>
{
    // Well-formed hash (32-byte hash, 16-byte salt) that matches no password.
    // Verifying against it keeps response time independent of whether the user exists.
    private static readonly string DummyPasswordHash = $"{new string('0', 64)}-{new string('0', 32)}";

    public async Task<Result<AccessTokensResponse>> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        User? user = await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(u => u.Email == command.Email, cancellationToken);

        if (user is null)
        {
            _ = passwordHasher.Verify(command.Password, DummyPasswordHash);

            return Result.Failure<AccessTokensResponse>(UserErrors.NotFoundByEmail);
        }

        bool verified = passwordHasher.Verify(command.Password, user.PasswordHash);

        if (!verified)
        {
            return Result.Failure<AccessTokensResponse>(UserErrors.NotFoundByEmail);
        }

        string accessToken = tokenProvider.Create(user);
        string refreshToken = tokenProvider.GenerateRefreshToken();

        context.RefreshTokens.Add(RefreshToken.Create(
            user.Id,
            Guid.NewGuid(),
            RefreshTokenHasher.Hash(refreshToken),
            dateTimeProvider.UtcNow));

        await context.SaveChangesAsync(cancellationToken);

        return new AccessTokensResponse(accessToken, refreshToken);
    }
}
