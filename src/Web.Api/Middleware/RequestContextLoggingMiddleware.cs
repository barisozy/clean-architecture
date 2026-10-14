using System.Diagnostics.CodeAnalysis;
using Serilog.Context;

namespace Web.Api.Middleware;

public class RequestContextLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationIdHeaderName = "Correlation-Id";
    private const int MaxCorrelationIdLength = 64;

    public async Task Invoke(HttpContext context)
    {
        string correlationId = GetCorrelationId(context);

        // Echo the value so a client report can be matched to the log lines of that request.
        context.Response.Headers[CorrelationIdHeaderName] = correlationId;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }

    private static string GetCorrelationId(HttpContext context)
    {
        string? supplied = context.Request.Headers[CorrelationIdHeaderName].FirstOrDefault();

        return IsValid(supplied) ? supplied : context.TraceIdentifier;
    }

    // Untrusted input: bounded length and a restricted alphabet prevent log flooding and log injection.
    private static bool IsValid([NotNullWhen(true)] string? value) =>
        !string.IsNullOrEmpty(value) &&
        value.Length <= MaxCorrelationIdLength &&
        value.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_' or '.' or ':');
}
