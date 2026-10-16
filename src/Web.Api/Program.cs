using System.Reflection;
using System.Security.Claims;
using Application;
using Infrastructure;
using Scalar.AspNetCore;
using Serilog;
using Web.Api;
using Web.Api.Extensions;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// preserveStaticLogger keeps the static Log.Logger untouched: each host owns and disposes only
// its own logger, which matters when several hosts run in one process (integration tests).
builder.Host.UseSerilog(
    (context, loggerConfig) => loggerConfig
        .ReadFrom.Configuration(context.Configuration)
        .WriteToOtlpIfConfigured(context.Configuration, context.HostingEnvironment),
    preserveStaticLogger: true);

builder.Services.AddOpenApiWithAuth();

builder.Services
    .AddApplication()
    .AddPresentation()
    .AddInfrastructure(builder.Configuration);

builder.Services.AddForwardedHeadersInternal(builder.Configuration);

builder.Services.AddRateLimitingInternal(builder.Configuration);

builder.Services.AddEndpoints(Assembly.GetExecutingAssembly());

WebApplication app = builder.Build();

// Must run first: client address and scheme are resolved before logging, rate limiting and auth.
// It is a no-op unless ForwardedHeaders:Enabled is configured.
app.UseForwardedHeaders();

app.MapEndpoints();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup", false))
{
    app.ApplyMigrations();
}

app.MapHealthEndpoints();

app.UseRequestContextLogging();

app.UseSerilogRequestLogging(options =>
{
    // The default is the static Log.Logger, which is shared between hosts in the same process
    // (for example, integration tests). Pin it to this host's own logger instance instead.
    options.Logger = app.Services.GetRequiredService<Serilog.ILogger>();

    // Request completion logs carry the authenticated actor, so they can be tied to a user.
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        string? userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!string.IsNullOrEmpty(userId))
        {
            diagnosticContext.Set("UserId", userId);
        }
    };
});

app.UseExceptionHandler();

app.UseAuthentication();

app.UseRateLimiter();

app.UseAuthorization();

// REMARK: If you want to use Controllers, you'll need this.
app.MapControllers();

await app.RunAsync();

// REMARK: Required for functional and integration tests to work.
namespace Web.Api
{
    public partial class Program;
}
