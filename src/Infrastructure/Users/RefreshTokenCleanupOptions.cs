using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Users;

internal sealed class RefreshTokenCleanupOptions
{
    public const string SectionName = "RefreshTokenCleanup";

    [Range(1, 1440)]
    public int IntervalInMinutes { get; set; } = 60;

    // Expired tokens are kept this long after expiry as an audit trail, then deleted.
    [Range(0, 3650)]
    public int RetentionInDays { get; set; } = 30;
}
