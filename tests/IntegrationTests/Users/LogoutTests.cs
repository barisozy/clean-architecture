using System.Net;
using System.Net.Http.Json;

namespace IntegrationTests.Users;

public sealed class LogoutTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Logout_Should_RevokeTheRefreshTokenFamily()
    {
        // Arrange
        (Guid _, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage logout = await HttpClient.PostAsJsonAsync(
            "users/logout",
            new { refreshToken = tokens.RefreshToken });

        // Assert
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        HttpResponseMessage refresh = await HttpClient.PostAsJsonAsync(
            "users/refresh-token",
            new { refreshToken = tokens.RefreshToken });

        refresh.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Logout_Should_NotAffectOtherSessions()
    {
        // Arrange
        string email = UniqueEmail();
        await RegisterUserAsync(email);
        AccessTokens deviceA = await LoginAsync(email);
        AccessTokens deviceB = await LoginAsync(email);
        Authenticate(deviceA.AccessToken);

        // Act
        HttpResponseMessage logout = await HttpClient.PostAsJsonAsync(
            "users/logout",
            new { refreshToken = deviceA.RefreshToken });

        // Assert
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        HttpResponseMessage refresh = await HttpClient.PostAsJsonAsync(
            "users/refresh-token",
            new { refreshToken = deviceB.RefreshToken });

        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Should_NotRevokeTokensOfAnotherUser()
    {
        // Arrange
        (Guid _, AccessTokens victim) = await RegisterAndLoginAsync();
        (Guid _, AccessTokens attacker) = await RegisterAndLoginAsync();
        Authenticate(attacker.AccessToken);

        // Act
        HttpResponseMessage logout = await HttpClient.PostAsJsonAsync(
            "users/logout",
            new { refreshToken = victim.RefreshToken });

        // Assert
        logout.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        HttpResponseMessage refresh = await HttpClient.PostAsJsonAsync(
            "users/refresh-token",
            new { refreshToken = victim.RefreshToken });

        refresh.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Should_BeIdempotent_ForUnknownToken()
    {
        // Arrange
        (Guid _, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "users/logout",
            new { refreshToken = "this-token-does-not-exist" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_Should_RequireAuthentication()
    {
        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "users/logout",
            new { refreshToken = "this-token-does-not-exist" });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Should_ReturnProblem_WhenRefreshTokenIsMissing()
    {
        // Arrange
        (Guid _, AccessTokens tokens) = await RegisterAndLoginAsync();
        Authenticate(tokens.AccessToken);

        // Act
        HttpResponseMessage response = await HttpClient.PostAsJsonAsync(
            "users/logout",
            new { refreshToken = string.Empty });

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
