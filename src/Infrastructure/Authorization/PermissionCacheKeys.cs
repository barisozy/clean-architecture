namespace Infrastructure.Authorization;

internal static class PermissionCacheKeys
{
    public static string ForUser(Guid userId) => $"permissions:user:{userId}";
}
