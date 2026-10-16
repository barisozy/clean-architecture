using Infrastructure.Authorization;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Users;

internal static class UserPermissionsTestExtensions
{
    internal static async Task<HashSet<string>> GetPermissionsAsync(this IntegrationTestWebAppFactory factory, Guid userId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        PermissionProvider provider = scope.ServiceProvider.GetRequiredService<PermissionProvider>();

        return await provider.GetForUserIdAsync(userId);
    }

    internal static async Task RevokePermissionsAsync(this IntegrationTestWebAppFactory factory, Guid userId)
    {
        using IServiceScope scope = factory.Services.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.UserPermissions
            .Where(permission => permission.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
