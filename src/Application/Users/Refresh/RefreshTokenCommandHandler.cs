using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using SharedKernel;

namespace Application.Users.Refresh;

internal sealed class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RefreshTokenCommand, AccessTokensResponse>
{
    public async Task<Result<AccessTokensResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = RefreshTokenHasher.Hash(command.RefreshToken);
        DateTime now = dateTimeProvider.UtcNow;

        RefreshToken? current = await context.RefreshTokens
            .Include(rt => rt.User)
            .SingleOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (current is null ||
            current.UsedOnUtc is not null ||
            current.RevokedOnUtc is not null ||
            current.ExpiresOnUtc <= now)
        {
            return Result.Failure<AccessTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        string accessToken = tokenProvider.Create(current.User);
        string newRefreshToken = tokenProvider.GenerateRefreshToken();

        // Rotation: retain the used token as an audit record and continue its family in a new row.
        current.UsedOnUtc = now;

        context.RefreshTokens.Add(RefreshToken.Create(
            current.UserId,
            current.FamilyId,
            RefreshTokenHasher.Hash(newRefreshToken),
            now));

        await context.SaveChangesAsync(cancellationToken);

        return new AccessTokensResponse(accessToken, newRefreshToken);
    }
}
