using System.Net;
using System.Net.Http.Json;
using Application.Users;
using Domain.Users;
using Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTests.Users;

/// <summary>
/// The reuse grace window is disabled by default; this suite runs against a dedicated host with
/// <c>RefreshTokenReuse:ReuseGraceSeconds</c> configured, so the strict revocation behavior
/// covered by <see cref="RefreshTokenTests"/> stays intact elsewhere.
/// </summary>
public sealed class RefreshTokenReuseGraceTests
{
    [Fact]
    public async Task ReplayedToken_Should_NotRevokeTheFamily_InsideTheGraceWindow()
    {
        // Arrange
        var factory = new IntegrationTestWebAppFactory
        {
            ExtraSettings = new Dictionary<string, string>
            {
                ["RefreshTokenReuse:ReuseGraceSeconds"] = "30"
            }
        };

        await factory.InitializeAsync();

        try
        {
            HttpClient client = factory.CreateClient();
            string email = $"{Guid.NewGuid():N}@example.com";

            HttpResponseMessage register = await client.PostAsJsonAsync(
                "users/register",
                new { email, firstName = "Test", lastName = "User", password = "Password123" });
            register.EnsureSuccessStatusCode();

            HttpResponseMessage login = await client.PostAsJsonAsync(
                "users/login",
                new { email, password = "Password123" });
            login.EnsureSuccessStatusCode();
            AccessTokensResponse first = (await login.Content.ReadFromJsonAsync<AccessTokensResponse>())!;

            HttpResponseMessage rotated = await client.PostAsJsonAsync(
                "users/refresh-token",
                new { refreshToken = first.RefreshToken });
            rotated.EnsureSuccessStatusCode();
            AccessTokensResponse second = (await rotated.Content.ReadFromJsonAsync<AccessTokensResponse>())!;

            // Act: replay the already-used token while inside the grace window.
            HttpResponseMessage replay = await client.PostAsJsonAsync(
                "users/refresh-token",
                new { refreshToken = first.RefreshToken });

            // Assert: the replay is rejected, but the family survives.
            replay.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

            HttpResponseMessage next = await client.PostAsJsonAsync(
                "users/refresh-token",
                new { refreshToken = second.RefreshToken });
            next.StatusCode.ShouldBe(HttpStatusCode.OK);

            using IServiceScope scope = factory.Services.CreateScope();
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            List<RefreshToken> rows = await db.RefreshTokens.AsNoTracking().ToListAsync();

            rows.ShouldAllBe(r => r.RevokedOnUtc == null);
        }
        finally
        {
            await factory.DisposeAsync();
        }
    }
}
