using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Logout;

internal sealed class LogoutUserCommandHandler(
    IApplicationDbContext context,
    IUserContext userContext,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<LogoutUserCommand>
{
    public async Task<Result> Handle(LogoutUserCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = RefreshTokenHasher.Hash(command.RefreshToken);
        DateTime now = dateTimeProvider.UtcNow;

        RefreshToken? token = await context.RefreshTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        // Unknown tokens and tokens that belong to other users are ignored without
        // revealing whether they exist; logging out is idempotent.
        if (token is null || token.UserId != userContext.UserId)
        {
            return Result.Success();
        }

        await context.RefreshTokens
            .Where(rt => rt.FamilyId == token.FamilyId && rt.RevokedOnUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedOnUtc, (DateTime?)now),
                cancellationToken);

        return Result.Success();
    }
}
