using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace IntegrationTests.Observability;

public class OtlpLogExportTests
{
    [Fact]
    public async Task Logs_Should_BeExportedOverOtlp_AndCarryTraceAndSpanIds_WhenEndpointIsConfigured()
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

        // The sink batches; disposing the host flushes it. Poll until the batch that carries
        // the request logs arrives, absorbing scheduling jitter under parallel test load.
        var records = new List<OtlpLogRecord>();

        for (int attempt = 0; attempt < 300; attempt++)
        {
            records = received.SelectMany(OtlpLogPayload.Parse).ToList();

            if (records.Any(record => record.HasTraceAndSpanIds))
            {
                break;
            }

            await Task.Delay(100);
        }

        // At least one record (the request completion log) is correlated with its trace:
        // the OTLP record carries the ids of the active activity.
        records.ShouldContain(record => record.HasTraceAndSpanIds);
    }
}
