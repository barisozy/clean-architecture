using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace IntegrationTests.Observability;

public class OtlpLogExportTests
{
    [Fact]
    public async Task Logs_Should_BeExportedOverOtlp_WhenEndpointIsConfigured()
    {
        var received = new ConcurrentBag<string>();

        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://127.0.0.1:0");

        await using WebApplication receiver = builder.Build();
        receiver.MapPost("/v1/{signal}", (string signal) =>
        {
            received.Add(signal);

            return Results.Ok();
        });
        await receiver.StartAsync();

        string endpoint = receiver.Urls.First();

        await using (var factory = new ApiFactory())
        {
            using HttpClient client = factory
                .WithWebHostBuilder(host =>
                {
                    host.UseSetting("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);
                    host.UseSetting("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");
                })
                .CreateClient();

            (await client.GetAsync("health/live")).EnsureSuccessStatusCode();
        }

        // The sink batches; disposing the host flushes it. Poll to absorb scheduling jitter.
        for (int i = 0; i < 50 && !received.Contains("logs"); i++)
        {
            await Task.Delay(100);
        }

        received.ShouldContain("logs");
    }
}
