using System.Net;
using System.Security.Claims;
using Domain.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Users;

[Collection(nameof(IntegrationTestCollection))]
public sealed class AuthorizationTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public AuthorizationTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Anonymous_GetUser_Returns401()
    {
        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Authenticated_GetOwnUser_Returns200()
    {
        // Arrange
        (Guid userId, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"users/{userId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Authenticated_GetOtherUser_Returns403()
    {
        // Arrange
        (_, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.GetAsync($"users/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_WithoutGrant_IsDenied()
    {
        // Arrange
        (Guid userId, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);
        await _factory.RevokePermissionsAsync(userId);

        // Act: own user, so only the missing users:access grant can produce the 403.
        HttpResponseMessage response = await HttpClient.GetAsync($"users/{userId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Authenticated_UnknownUser_IsDenied()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorizationService authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // Act
        AuthorizationResult result = await authz.AuthorizeAsync(
            Authenticated(Guid.NewGuid()),
            null,
            Permissions.UsersAccess);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Authenticated_UnknownPermission_IsDenied()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorizationService authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // Act
        AuthorizationResult result = await authz.AuthorizeAsync(Authenticated(Guid.NewGuid()), null, "orders:delete");

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Authenticated_KnownPermission_IsAllowed()
    {
        // Arrange
        (Guid userId, _) = await RegisterAndLoginAsync();
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorizationService authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // Act
        AuthorizationResult result = await authz.AuthorizeAsync(Authenticated(userId), null, Permissions.UsersAccess);

        // Assert
        result.Succeeded.ShouldBeTrue();
    }

    [Fact]
    public async Task Anonymous_AnyPermission_IsDeniedWithoutThrowing()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorizationService authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        AuthorizationResult result = await authz.AuthorizeAsync(anonymous, null, Permissions.UsersAccess);

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    private static ClaimsPrincipal Authenticated(Guid userId) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, userId.ToString())],
            authenticationType: "test"));
}
