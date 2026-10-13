namespace Infrastructure.Authorization;

internal sealed class PermissionProvider
{
    // Stub: In a real application, this would query a database or an external service to get the permissions for the user.
    private static readonly string[] DefaultPermissions = ["users:access"];

    public Task<HashSet<string>> GetForUserIdAsync(Guid userId) =>
        Task.FromResult(new HashSet<string>(DefaultPermissions, StringComparer.Ordinal));
}
