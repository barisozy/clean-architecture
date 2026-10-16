using Application.Abstractions.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;

namespace Infrastructure.Authorization;

internal sealed class PermissionProvider(IApplicationDbContext context, HybridCache cache)
{
    // Serving permissions from the cache keeps the authorization path off the database, at the
    // cost of a revoked permission staying effective for up to five minutes per application
    // instance. Any command that mutates user permissions must invalidate the affected entry.
    private static readonly HybridCacheEntryOptions CacheEntryOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5)
    };

    public async Task<HashSet<string>> GetForUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
#pragma warning disable EXTEXP0018 // HybridCache is released; the API is stable in .NET 10.
        string[] permissions = await cache.GetOrCreateAsync(
            PermissionCacheKeys.ForUser(userId),
            async token => await context.UserPermissions
                .Where(permission => permission.UserId == userId)
                .Select(permission => permission.Permission)
                .ToArrayAsync(token),
            CacheEntryOptions,
            cancellationToken: cancellationToken);
#pragma warning restore EXTEXP0018

        return new HashSet<string>(permissions, StringComparer.Ordinal);
    }
}
