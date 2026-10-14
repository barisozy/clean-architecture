using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Shouldly;
using Web.Api;

namespace IntegrationTests.Health;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task Live_Should_ReturnHealthy_WithoutTouchingTheDatabase()
    {
        await using var factory = new ApiFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Healthy");
    }

    [Fact]
    public async Task Ready_Should_ExposeStatusOnly_WhenDatabaseIsUnreachable()
    {
        await using var factory = new ApiFactory();
        HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("health/ready");

        response.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).ShouldBe("Unhealthy");
    }

    [Fact]
    public async Task Production_Should_NotExposeTheDetailedHealthReport()
    {
        await using ApiFactory baseFactory = new();
        await using WebApplicationFactory<Program> factory = baseFactory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("Jwt:Secret", "production-like-secret-0123456789-0123456789");
            builder.UseSetting("Jwt:Issuer", "issuer");
            builder.UseSetting("Jwt:Audience", "audience");
            builder.UseSetting("Jwt:ExpirationInMinutes", "60");
        });
        HttpClient client = factory.CreateClient();

        (await client.GetAsync("health")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await client.GetAsync("health/live")).StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}
