using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Application.Users;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Users;

public sealed class RefreshTokenTests : BaseIntegrationTest
{
    private const string Password = "Str0ng!Passw0rd#2026";
    private readonly IntegrationTestWebAppFactory _factory;

    public RefreshTokenTests(IntegrationTestWebAppFactory factory) : base(factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_Should_StoreOnlyTheHashOfTheRefreshToken()
    {
        // Arrange
        (string _, AccessTokensResponse tokens) = await RegisterAndLoginWithRefreshAsync();

        // Act
        List<RefreshToken> rows = await LoadRefreshTokensAsync();

        // Assert
        rows.ShouldContain(r => r.TokenHash == Hash(tokens.RefreshToken));
        rows.ShouldNotContain(r => r.TokenHash == tokens.RefreshToken);
    }

    [Fact]
    public async Task Refresh_Should_RotateWithinSameFamily_AndMarkOldTokenUsed()
    {
        // Arrange
        (string _, AccessTokensResponse first) = await RegisterAndLoginWithRefreshAsync();

        // Act
        HttpResponseMessage response = await RefreshAsync(first.RefreshToken);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        AccessTokensResponse second = (await response.Content.ReadFromJsonAsync<AccessTokensResponse>())!;
        second.RefreshToken.ShouldNotBe(first.RefreshToken);

        List<RefreshToken> rows = await LoadRefreshTokensAsync();
        RefreshToken old = rows.Single(r => r.TokenHash == Hash(first.RefreshToken));
        RefreshToken current = rows.Single(r => r.TokenHash == Hash(second.RefreshToken));

        current.FamilyId.ShouldBe(old.FamilyId);
        old.UsedOnUtc.ShouldNotBeNull();
        current.UsedOnUtc.ShouldBeNull();
    }

    [Fact]
    public async Task Refresh_Should_RejectAlreadyUsedToken()
    {
        // Arrange
        (string _, AccessTokensResponse first) = await RegisterAndLoginWithRefreshAsync();
        (await RefreshAsync(first.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        HttpResponseMessage replay = await RefreshAsync(first.RefreshToken);

        // Assert
        replay.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Refresh_Should_RevokeWholeFamily_WhenUsedTokenIsReplayed()
    {
        // Arrange
        (string _, AccessTokensResponse first) = await RegisterAndLoginWithRefreshAsync();
        HttpResponseMessage rotated = await RefreshAsync(first.RefreshToken);
        rotated.StatusCode.ShouldBe(HttpStatusCode.OK);
        AccessTokensResponse second = (await rotated.Content.ReadFromJsonAsync<AccessTokensResponse>())!;

        // Act
        (await RefreshAsync(first.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await RefreshAsync(second.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Assert
        List<RefreshToken> rows = await LoadRefreshTokensAsync();
        Guid familyId = rows.Single(r => r.TokenHash == Hash(first.RefreshToken)).FamilyId;
        rows.Where(r => r.FamilyId == familyId).ShouldAllBe(r => r.RevokedOnUtc != null);
    }

    [Fact]
    public async Task Refresh_Should_NotAffectOtherFamiliesOfTheSameUser()
    {
        // Arrange
        (string email, AccessTokensResponse deviceA) = await RegisterAndLoginWithRefreshAsync();
        AccessTokensResponse deviceB = await LoginWithRefreshAsync(email);

        // Act
        (await RefreshAsync(deviceA.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await RefreshAsync(deviceA.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Assert
        (await RefreshAsync(deviceB.RefreshToken)).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_Should_AllowExactlyOneWinner_WhenSameTokenIsUsedConcurrently()
    {
        // Arrange
        (string _, AccessTokensResponse tokens) = await RegisterAndLoginWithRefreshAsync();
        Task<HttpResponseMessage>[] requests = Enumerable
            .Range(0, 8)
            .Select(_ => RefreshAsync(tokens.RefreshToken))
            .ToArray();

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(requests);

        // Assert
        responses.Count(r => r.StatusCode == HttpStatusCode.OK).ShouldBe(1);
        responses.Count(r => r.StatusCode == HttpStatusCode.BadRequest).ShouldBe(7);

        List<RefreshToken> rows = await LoadRefreshTokensAsync();
        Guid familyId = rows.Single(r => r.TokenHash == Hash(tokens.RefreshToken)).FamilyId;
        rows.Where(r => r.FamilyId == familyId).ShouldAllBe(r => r.RevokedOnUtc != null);
    }

    private async Task<(string Email, AccessTokensResponse Tokens)> RegisterAndLoginWithRefreshAsync()
    {
        HttpClient client = _factory.CreateClient();
        string email = $"{Guid.NewGuid():N}@example.com";

        HttpResponseMessage register = await client.PostAsJsonAsync(
            "users/register",
            new { email, firstName = "Test", lastName = "User", password = Password });
        register.EnsureSuccessStatusCode();

        return (email, await LoginWithRefreshAsync(email));
    }

    private async Task<AccessTokensResponse> LoginWithRefreshAsync(string email)
    {
        HttpResponseMessage login = await _factory.CreateClient().PostAsJsonAsync(
            "users/login",
            new { email, password = Password });
        login.EnsureSuccessStatusCode();

        return (await login.Content.ReadFromJsonAsync<AccessTokensResponse>())!;
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
