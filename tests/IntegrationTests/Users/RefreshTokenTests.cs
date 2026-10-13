using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Users;

public sealed class RefreshTokenTests : BaseIntegrationTest
{
    private readonly IntegrationTestWebAppFactory _factory;

    public RefreshTokenTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Should_StoreOnlyTheHashOfTheRefreshToken()
    {
        // Arrange
        (Guid UserId, AccessTokens Tokens) registration = await RegisterAndLoginAsync();

        // Act
        List<RefreshToken> rows = await LoadRefreshTokensAsync();

        // Assert
        rows.ShouldContain(r => r.TokenHash == Hash(registration.Tokens.RefreshToken));
        rows.ShouldNotContain(r => r.TokenHash == registration.Tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_Should_RotateWithinSameFamily_AndMarkOldTokenUsed()
    {
        // Arrange
        (Guid UserId, AccessTokens Tokens) registration = await RegisterAndLoginAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync(registration.Tokens.RefreshToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AccessTokens second = (await response.Content.ReadFromJsonAsync<AccessTokens>())!;
        second.RefreshToken.ShouldNotBe(registration.Tokens.RefreshToken);

        List<RefreshToken> rows = await LoadRefreshTokensAsync();
        RefreshToken old = rows.Single(r => r.TokenHash == Hash(registration.Tokens.RefreshToken));
        RefreshToken current = rows.Single(r => r.TokenHash == Hash(second.RefreshToken));

        current.FamilyId.ShouldBe(old.FamilyId);
        old.UsedOnUtc.ShouldNotBeNull();
        current.UsedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Refresh_Should_RejectAlreadyUsedToken()
    {
        // Arrange
        (Guid UserId, AccessTokens Tokens) registration = await RegisterAndLoginAsync();
        (await RefreshAsync(registration.Tokens.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        HttpResponseMessage replay = await RefreshAsync(registration.Tokens.RefreshToken);

        // Assert
        replay.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private Task<HttpResponseMessage> RefreshAsync(string refreshToken) =>
        _factory.CreateClient().PostAsJsonAsync("users/refresh-token", new { refreshToken });

    private async Task<List<RefreshToken>> LoadRefreshTokensAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.RefreshTokens.AsNoTracking().ToListAsync();
    }

    private static string Hash(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
