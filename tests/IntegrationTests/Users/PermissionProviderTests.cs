using Domain.Users;

namespace IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class PermissionProviderTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public PermissionProviderTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetForUserIdAsync_Should_ReturnGrantedPermissions()
    {
        // Arrange
        (Guid userId, _) = await RegisterAndLoginAsync();

        // Act
        HashSet<string> permissions = await _factory.GetPermissionsAsync(userId);

        // Assert
        permissions.ShouldBe(Permissions.DefaultForNewUsers, ignoreOrder: true);
    }

    [Fact]
    public async Task GetForUserIdAsync_Should_ReturnEmpty_WhenUserHasNoGrants()
    {
        // Act
        HashSet<string> permissions = await _factory.GetPermissionsAsync(Guid.NewGuid());

        // Assert
        permissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetForUserIdAsync_Should_ServeRepeatedReadsFromCache()
    {
        // Arrange
        (Guid userId, _) = await RegisterAndLoginAsync();
        (await _factory.GetPermissionsAsync(userId)).ShouldContain(Permissions.UsersAccess);

        // Act
        await _factory.RevokePermissionsAsync(userId);
        HashSet<string> permissions = await _factory.GetPermissionsAsync(userId);

        // Assert: the cached grant is served until the entry expires.
        permissions.ShouldContain(Permissions.UsersAccess);
    }
}
