using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;

namespace Web.Api.Extensions;

internal static class HealthEndpointsExtensions
{
    internal static WebApplication MapHealthEndpoints(this WebApplication app)
    {
        // Liveness: the process is responsive. Runs only checks tagged "live" and never touches dependencies,
        // so a database outage does not make the orchestrator restart a healthy process.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live")
            })
            .DisableRateLimiting();

        // Readiness: dependencies are reachable. The default writer prints the status only,
        // with no check names or exception details.
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready")
            })
            .DisableRateLimiting();

        if (app.Environment.IsDevelopment())
        {
            // Detailed report (check names, durations, exception messages) for local diagnostics only.
            app.MapHealthChecks("/health", new HealthCheckOptions
                {
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                })
                .DisableRateLimiting();
        }

        return app;
    }
}
