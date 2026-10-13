using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Core;
using Shouldly;

namespace IntegrationTests.Logging;

[Collection("console-out")]
public sealed class ProductionLoggingTests
{
    [Fact]
    public void BaseConfiguration_WritesStructuredLogsToConsole()
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile(FindUp("src/Web.Api/appsettings.json"), optional: false)
            .Build();

        TextWriter original = Console.Out;
        using var captured = new StringWriter();
        Console.SetOut(captured);

        try
        {
            using Logger logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();

            logger.Information("production-log-probe {Probe}", 42);
        }
        finally
        {
            Console.SetOut(original);
        }

        string output = captured.ToString();

        output.ShouldContain("production-log-probe");
        output.ShouldContain("\"@t\"");
    }

    private static string FindUp(string relativePath)
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);

        while (directory is not null)
        {
            string candidate = Path.Combine(directory.FullName, relativePath);

            if (File.Exists(candidate))
            {
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not locate '{relativePath}' above '{AppContext.BaseDirectory}'.");
    }
}
