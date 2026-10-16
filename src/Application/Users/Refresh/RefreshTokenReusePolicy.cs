namespace Application.Users.Refresh;

internal static class RefreshTokenReusePolicy
{
    public static bool ShouldRevokeFamily(DateTime usedOnUtc, DateTime now, int graceSeconds) =>
        graceSeconds <= 0 || now - usedOnUtc > TimeSpan.FromSeconds(graceSeconds);
}
