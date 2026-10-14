using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SharedKernel;

namespace Infrastructure.Users;

internal sealed partial class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<RefreshTokenCleanupOptions> options,
    ILogger<RefreshTokenCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.Value.IntervalInMinutes));

        try
        {
            // First run happens after one interval so it never contends with startup and migrations.
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await TryRunOnceAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Host is shutting down.
        }
    }

    internal async Task<int> RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopeFactory.CreateScope();

        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        IDateTimeProvider dateTimeProvider = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        int retentionInDays = options.Value.RetentionInDays;
        DateTime threshold = dateTimeProvider.UtcNow.AddDays(-retentionInDays);

        int deleted = await context.RefreshTokens
            .Where(rt => rt.ExpiresOnUtc < threshold)
            .ExecuteDeleteAsync(cancellationToken);

        if (deleted > 0)
        {
            LogTokensDeleted(logger, deleted, retentionInDays);
        }

        return deleted;
    }

    private async Task TryRunOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RunOnceAsync(cancellationToken);
        }
#pragma warning disable CA1031 // A failing cleanup must not stop the host; it is retried on the next tick.
        catch (Exception exception) when (exception is not OperationCanceledException)
#pragma warning restore CA1031
        {
            LogCleanupFailed(logger, exception);
        }
    }

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Information,
        Message = "Deleted {DeletedTokens} refresh tokens expired more than {RetentionInDays} days ago")]
    private static partial void LogTokensDeleted(ILogger logger, int deletedTokens, int retentionInDays);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Error,
        Message = "Refresh token cleanup failed")]
    private static partial void LogCleanupFailed(ILogger logger, Exception exception);
}
