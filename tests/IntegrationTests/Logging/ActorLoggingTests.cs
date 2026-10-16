using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using IntegrationTests.Observability;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace IntegrationTests.Logging;

/// <summary>
/// Verifies that request completion logs exported over OTLP carry the authenticated actor,
/// so an audit trail can be reconstructed from the log backend.
/// </summary>
public sealed class ActorLoggingTests
{
    private sealed record Tokens(string AccessToken, string RefreshToken);

    [Fact]
    public async Task AuthenticatedRequests_Should_BeLoggedWithTheUserId()
    {
        var received = new ConcurrentBag<byte[]>();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        await using WebApplication receiver = builder.Build();
        receiver.MapPost("/v1/{signal}", async (string signal, HttpRequest request) =>
        {
            if (signal == "logs")
            {
                using var body = new MemoryStream();
                await request.Body.CopyToAsync(body);
                received.Add(body.ToArray());
            }

            return Results.Ok();
        });
        await receiver.StartAsync();

        var factory = new IntegrationTestWebAppFactory
        {
            ExtraSettings = new Dictionary<string, string>
            {
                ["OTEL_EXPORTER_OTLP_ENDPOINT"] = receiver.Urls.First(),
                ["OTEL_EXPORTER_OTLP_PROTOCOL"] = "http/protobuf"
            }
        };

        await factory.InitializeAsync();

        Guid userId;

        try
        {
            HttpClient client = factory.CreateClient();
            string email = $"test-{Guid.NewGuid():N}@example.com";

            HttpResponseMessage register = await client.PostAsJsonAsync(
                "users/register",
                new { email, firstName = "Test", lastName = "User", password = "Password123" });
            register.EnsureSuccessStatusCode();
            userId = await register.Content.ReadFromJsonAsync<Guid>();

            HttpResponseMessage login = await client.PostAsJsonAsync(
                "users/login",
                new { email, password = "Password123" });
            login.EnsureSuccessStatusCode();
            Tokens tokens = (await login.Content.ReadFromJsonAsync<Tokens>())!;

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokens.AccessToken);

            (await client.GetAsync($"users/{userId}")).EnsureSuccessStatusCode();
        }
        finally
        {
            // The sink batches; disposing the host flushes it.
            await factory.DisposeAsync();
        }

        // The sink batches; poll until the batch that carries the authenticated request's
        // completion log arrives, absorbing scheduling jitter under parallel test load.
        var records = new List<OtlpLogRecord>();

        for (int attempt = 0; attempt < 300; attempt++)
        {
            records = received.SelectMany(OtlpLogPayload.Parse).ToList();

            if (records.Any(record => record.HasAttribute("UserId", userId.ToString())))
            {
                break;
            }

            await Task.Delay(100);
        }

        records.ShouldContain(record => record.HasAttribute("UserId", userId.ToString()));
    }
}
