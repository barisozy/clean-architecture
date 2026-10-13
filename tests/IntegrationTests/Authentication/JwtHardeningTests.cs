using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Web.Api;

namespace IntegrationTests.Authentication;

[Collection(nameof(IntegrationTestCollection))]
public sealed class JwtHardeningTests(IntegrationTestWebAppFactory factory)
{
    [Fact]
    public void Startup_WithShortSecret_Fails()
    {
        using WebApplicationFactory<Program> invalidFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:Secret"] = "too-short" })));

        Exception exception = Should.Throw<Exception>(() => invalidFactory.CreateClient());

        exception.ToString().ShouldContain("Jwt:Secret");
    }

    [Fact]
    public void Startup_InProduction_WithDevelopmentSecret_Fails()
    {
        using WebApplicationFactory<Program> invalidFactory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "dev-only-this-secret-is-long-enough-but-not-allowed",
                    ["Jwt:Issuer"] = "issuer",
                    ["Jwt:Audience"] = "audience",
                    ["Jwt:ExpirationInMinutes"] = "60"
                }));
        });

        Exception exception = Should.Throw<Exception>(() => invalidFactory.CreateClient());

        exception.ToString().ShouldContain("development-only");
    }

    [Fact]
    public void Startup_WithPlaceholderSecret_Fails()
    {
        using WebApplicationFactory<Program> invalidFactory = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "set-in-user-secrets-or-environment"
                })));

        Exception exception = Should.Throw<Exception>(() => invalidFactory.CreateClient());

        exception.ToString().ShouldContain("Jwt:Secret");
        exception.ToString().ShouldNotContain("set-in-user-secrets-or-environment");
    }

    [Fact]
    public async Task Token_SignedWithUnexpectedAlgorithm_IsRejected()
    {
        IConfiguration configuration = factory.Services.GetRequiredService<IConfiguration>();
        string otherUser = Guid.NewGuid().ToString();

        using HttpResponseMessage hs256 = await Get(factory, CreateToken(configuration, SecurityAlgorithms.HmacSha256), otherUser);
        hs256.StatusCode.ShouldBe(HttpStatusCode.Forbidden, await hs256.Content.ReadAsStringAsync());

        using HttpResponseMessage hs512 = await Get(factory, CreateToken(configuration, SecurityAlgorithms.HmacSha512), otherUser);
        hs512.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    private static async Task<HttpResponseMessage> Get(
        IntegrationTestWebAppFactory factory,
        string token,
        string userId)
    {
        using HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await client.GetAsync($"users/{userId}");
    }

    private static string CreateToken(IConfiguration configuration, string algorithm)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [new Claim(JwtRegisteredClaimNames.Sub, Guid.NewGuid().ToString())]),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Secret"]!)),
                algorithm)
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }
}
