using System.Net;
using System.Security.Claims;
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
    public async Task Authenticated_UnknownPermission_IsDenied()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorizationService authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // Act
        AuthorizationResult result = await authz.AuthorizeAsync(Authenticated(), null, "orders:delete");

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    [Fact]
    public async Task Authenticated_KnownPermission_IsAllowed()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        IAuthorizationService authz = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // Act
        AuthorizationResult result = await authz.AuthorizeAsync(Authenticated(), null, "users:access");

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
        AuthorizationResult result = await authz.AuthorizeAsync(anonymous, null, "users:access");

        // Assert
        result.Succeeded.ShouldBeFalse();
    }

    private static ClaimsPrincipal Authenticated() =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())],
            authenticationType: "test"));
}
