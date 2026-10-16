using Application.Abstractions.Authentication;
using Application.Abstractions.Data;
using Application.Abstractions.Messaging;
using Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Application.Users.Refresh;

internal sealed partial class RefreshTokenCommandHandler(
    IApplicationDbContext context,
    ITokenProvider tokenProvider,
    IDateTimeProvider dateTimeProvider,
    IOptions<RefreshTokenReuseOptions> reuseOptions,
    ILogger<RefreshTokenCommandHandler> logger) : ICommandHandler<RefreshTokenCommand, AccessTokensResponse>
{
    public async Task<Result<AccessTokensResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        string tokenHash = RefreshTokenHasher.Hash(command.RefreshToken);
        DateTime now = dateTimeProvider.UtcNow;

        await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // One SQL statement claims the token. Only one concurrent request can change UsedOnUtc from NULL.
        int claimed = await context.RefreshTokens
            .Where(rt => rt.TokenHash == tokenHash &&
                         rt.UsedOnUtc == null &&
                         rt.RevokedOnUtc == null &&
                         rt.ExpiresOnUtc > now)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.UsedOnUtc, (DateTime?)now),
                cancellationToken);

        if (claimed == 0)
        {
            await RevokeFamilyIfTokenWasReusedAsync(tokenHash, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return Result.Failure<AccessTokensResponse>(UserErrors.InvalidRefreshToken);
        }

        RefreshToken current = await context.RefreshTokens
            .AsNoTracking()
            .Include(rt => rt.User)
            .SingleAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        string accessToken = tokenProvider.Create(current.User);
        string newRefreshToken = tokenProvider.GenerateRefreshToken();

        context.RefreshTokens.Add(RefreshToken.Create(
            current.UserId,
            current.FamilyId,
            RefreshTokenHasher.Hash(newRefreshToken),
            now));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new AccessTokensResponse(accessToken, newRefreshToken);
    }

    private async Task RevokeFamilyIfTokenWasReusedAsync(
        string tokenHash,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Unknown, expired, or revoked tokens do not trigger family revocation.
        var reused = await context.RefreshTokens
            .AsNoTracking()
            .Where(rt => rt.TokenHash == tokenHash && rt.UsedOnUtc != null)
            .Select(rt => new { rt.FamilyId, rt.UserId, rt.UsedOnUtc })
            .SingleOrDefaultAsync(cancellationToken);

        if (reused is null ||
            !RefreshTokenReusePolicy.ShouldRevokeFamily(
                reused.UsedOnUtc!.Value,
                now,
                reuseOptions.Value.ReuseGraceSeconds))
        {
            return;
        }

        int revoked = await context.RefreshTokens
            .Where(rt => rt.FamilyId == reused.FamilyId && rt.RevokedOnUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(rt => rt.RevokedOnUtc, (DateTime?)now),
                cancellationToken);

        LogRefreshTokenReuseDetected(logger, reused.FamilyId, reused.UserId, revoked);
    }

    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected. FamilyId={FamilyId} UserId={UserId} RevokedTokens={RevokedTokens}")]
    private static partial void LogRefreshTokenReuseDetected(
        ILogger logger,
        Guid familyId,
        Guid userId,
        int revokedTokens);
}
