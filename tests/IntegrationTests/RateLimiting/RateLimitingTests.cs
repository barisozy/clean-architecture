using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Shouldly;
using Web.Api;

namespace IntegrationTests.RateLimiting;

public sealed class RateLimitingTests(RateLimitingTests.RateLimitedFactory factory)
    : IClassFixture<RateLimitingTests.RateLimitedFactory>
{
    private const int Limit = 3;
    private const string RemoteIpHeader = "X-Test-Remote-Ip";

    [Fact]
    public async Task Authenticated_Users_Should_HaveSeparateBuckets_EvenFromTheSameIp()
    {
        HttpClient userA = CreateAuthenticatedClient(Guid.NewGuid());
        HttpClient userB = CreateAuthenticatedClient(Guid.NewGuid());
        string path = $"users/{Guid.NewGuid()}"; // 403 for non-owners, no database access.

        for (int i = 0; i < Limit; i++)
        {
            (await SendAsync(userA, path, "127.0.0.1")).ShouldNotBe(HttpStatusCode.TooManyRequests);
        }

        (await SendAsync(userA, path, "127.0.0.1")).ShouldBe(HttpStatusCode.TooManyRequests);
        (await SendAsync(userB, path, "127.0.0.1")).ShouldNotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task TrustedProxy_Should_PartitionAnonymousClientsByForwardedFor()
    {
        HttpClient client = factory.CreateClient();

        for (int i = 0; i < Limit; i++)
        {
            (await SendAsync(client, "openapi/v1.json", "127.0.0.1", "203.0.113.10"))
                .ShouldNotBe(HttpStatusCode.TooManyRequests);
        }

        (await SendAsync(client, "openapi/v1.json", "127.0.0.1", "203.0.113.10"))
            .ShouldBe(HttpStatusCode.TooManyRequests);

        (await SendAsync(client, "openapi/v1.json", "127.0.0.1", "203.0.113.20"))
            .ShouldNotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task HealthProbes_Should_NeverBeRateLimited()
    {
        HttpClient client = factory.CreateClient();

        for (int i = 0; i < Limit * 5; i++)
        {
            (await SendAsync(client, "health/live", "127.0.0.1", "203.0.113.50"))
                .ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task UntrustedPeer_Should_NotBeAbleToSpoofForwardedFor()
    {
        HttpClient client = factory.CreateClient();

        for (int i = 0; i < Limit; i++)
        {
            (await SendAsync(client, "openapi/v1.json", "198.51.100.7", "203.0.113.30"))
                .ShouldNotBe(HttpStatusCode.TooManyRequests);
        }

        (await SendAsync(client, "openapi/v1.json", "198.51.100.7", "203.0.113.30"))
            .ShouldBe(HttpStatusCode.TooManyRequests);

        // A different spoofed address must not open a fresh bucket.
        (await SendAsync(client, "openapi/v1.json", "198.51.100.7", "203.0.113.31"))
            .ShouldBe(HttpStatusCode.TooManyRequests);
    }

    private static async Task<HttpStatusCode> SendAsync(
        HttpClient client,
        string path,
        string remoteIp,
        string? forwardedFor = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add(RemoteIpHeader, remoteIp);

        if (forwardedFor is not null)
        {
            request.Headers.Add("X-Forwarded-For", forwardedFor);
        }

        using HttpResponseMessage response = await client.SendAsync(request);

        return response.StatusCode;
    }

    private HttpClient CreateAuthenticatedClient(Guid userId)
    {
        IConfiguration config = factory.Services.GetRequiredService<IConfiguration>();

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[] { new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()) }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            Issuer = config["Jwt:Issuer"],
            Audience = config["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!)),
                SecurityAlgorithms.HmacSha256)
        };

        HttpClient client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", new JsonWebTokenHandler().CreateToken(descriptor));

        return client;
    }

    public sealed class RateLimitedFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseSetting("ConnectionStrings:Database", "Host=localhost;Database=clean-architecture;Username=postgres;Password=postgres");
            builder.UseSetting("RateLimiting:Global:PermitLimit", Limit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.UseSetting("RateLimiting:Global:WindowInSeconds", "600");
            builder.UseSetting("ForwardedHeaders:Enabled", "true");
            builder.UseSetting("ForwardedHeaders:KnownProxies:0", "127.0.0.1");

            builder.ConfigureTestServices(services =>
                services.AddSingleton<IStartupFilter, RemoteIpStartupFilter>());
        }

        private sealed class RemoteIpStartupFilter : IStartupFilter
        {
            public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
                app =>
                {
                    app.Use(async (context, nextMiddleware) =>
                    {
                        if (context.Request.Headers.TryGetValue(RemoteIpHeader, out StringValues value) &&
                            IPAddress.TryParse(value.ToString(), out IPAddress? address))
                        {
                            context.Connection.RemoteIpAddress = address;
                        }

                        await nextMiddleware();
                    });

                    next(app);
                };
        }
    }
}
