using Serilog;
using Serilog.Sinks.OpenTelemetry;

namespace Web.Api.Extensions;

internal static class SerilogOtlpExtensions
{
    private const string EndpointKey = "OTEL_EXPORTER_OTLP_ENDPOINT";
    private const string ProtocolKey = "OTEL_EXPORTER_OTLP_PROTOCOL";
    private const string ServiceNameKey = "OTEL_SERVICE_NAME";
    private const string ResourceAttributesKey = "OTEL_RESOURCE_ATTRIBUTES";
    private const string LogsPath = "/v1/logs";

    // Logs go through Serilog (the only logging provider), so the OTLP export of logs lives here.
    // Traces and metrics are exported by the OpenTelemetry SDK configured in Aspire.ServiceDefaults.
    internal static LoggerConfiguration WriteToOtlpIfConfigured(
        this LoggerConfiguration loggerConfiguration,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        string? endpoint = configuration[EndpointKey];

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return loggerConfiguration;
        }

        bool useHttp = string.Equals(
            configuration[ProtocolKey],
            "http/protobuf",
            StringComparison.OrdinalIgnoreCase);

        return loggerConfiguration.WriteTo.OpenTelemetry(options =>
        {
            options.Protocol = useHttp ? OtlpProtocol.HttpProtobuf : OtlpProtocol.Grpc;
            options.Endpoint = useHttp ? WithLogsPath(endpoint) : endpoint;
            options.ResourceAttributes = BuildResourceAttributes(configuration, environment);
        });
    }

    private static string WithLogsPath(string endpoint)
    {
        string trimmed = endpoint.TrimEnd('/');

        return trimmed.EndsWith(LogsPath, StringComparison.Ordinal) ? trimmed : trimmed + LogsPath;
    }

    private static Dictionary<string, object> BuildResourceAttributes(
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var attributes = new Dictionary<string, object>
        {
            ["service.name"] = configuration[ServiceNameKey] ?? environment.ApplicationName,
            ["deployment.environment.name"] = environment.EnvironmentName
        };

        string? raw = configuration[ResourceAttributesKey];

        if (!string.IsNullOrWhiteSpace(raw))
        {
            foreach (string pair in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                int separator = pair.IndexOf('=');

                if (separator > 0)
                {
                    attributes[pair[..separator].Trim()] = pair[(separator + 1)..].Trim();
                }
            }
        }

        return attributes;
    }
}
