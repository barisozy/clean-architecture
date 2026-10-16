using System.Xml.Linq;
using Shouldly;

namespace ArchitectureTests;

public class ProjectReferenceTests
{
    [Fact]
    public void SharedKernel_Should_HaveNoReferences() =>
        AssertReferences("src/SharedKernel/SharedKernel.csproj", projects: [], packages: []);

    [Fact]
    public void Domain_Should_ReferenceOnlySharedKernel_AndNoPackages() =>
        AssertReferences("src/Domain/Domain.csproj", projects: ["SharedKernel"], packages: []);

    [Fact]
    public void Application_Should_UseOnlyApprovedReferences() =>
        AssertReferences(
            "src/Application/Application.csproj",
            projects: ["Domain", "SharedKernel"],
            packages:
            [
                "FluentValidation.DependencyInjectionExtensions",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions.Caching.Hybrid",
                "Microsoft.Extensions.Logging.Abstractions",
                "Microsoft.Extensions.Options",
                "Scrutor",
                "Serilog"
            ]);

    [Fact]
    public void Infrastructure_Should_UseOnlyApprovedReferences() =>
        AssertReferences(
            "src/Infrastructure/Infrastructure.csproj",
            projects: ["Application"],
            packages:
            [
                "AspNetCore.HealthChecks.NpgSql",
                "EFCore.NamingConventions",
                "Microsoft.AspNetCore.Authentication.JwtBearer",
                "Microsoft.EntityFrameworkCore.Relational",
                "Microsoft.Extensions.Diagnostics.HealthChecks",
                "Npgsql.EntityFrameworkCore.PostgreSQL"
            ]);

    [Fact]
    public void WebApi_Should_UseOnlyApprovedReferences() =>
        AssertReferences(
            "src/Web.Api/Web.Api.csproj",
            projects: ["Aspire.ServiceDefaults", "Infrastructure"],
            packages:
            [
                "AspNetCore.HealthChecks.UI.Client",
                "Microsoft.AspNetCore.OpenApi",
                "Microsoft.EntityFrameworkCore.Tools",
                "Microsoft.VisualStudio.Azure.Containers.Tools.Targets",
                "Scalar.AspNetCore",
                "Serilog.AspNetCore",
                "Serilog.Sinks.OpenTelemetry",
                "Serilog.Sinks.Seq"
            ]);

    [Fact]
    public void ServiceDefaults_Should_UseOnlyApprovedReferences() =>
        AssertReferences(
            "src/Aspire.ServiceDefaults/Aspire.ServiceDefaults.csproj",
            projects: [],
            packages:
            [
                "Microsoft.Extensions.Http.Resilience",
                "Microsoft.Extensions.ServiceDiscovery",
                "Npgsql.OpenTelemetry",
                "OpenTelemetry.Exporter.OpenTelemetryProtocol",
                "OpenTelemetry.Extensions.Hosting",
                "OpenTelemetry.Instrumentation.AspNetCore",
                "OpenTelemetry.Instrumentation.EntityFrameworkCore",
                "OpenTelemetry.Instrumentation.Http",
                "OpenTelemetry.Instrumentation.Runtime"
            ]);

    private static void AssertReferences(string projectPath, string[] projects, string[] packages)
    {
        XDocument document = XDocument.Load(RepoFiles.Find(projectPath));

        var actualProjects = document
            .Descendants("ProjectReference")
            .Select(e => Path.GetFileNameWithoutExtension(((string)e.Attribute("Include")!).Replace('\\', '/')))
            .Order(StringComparer.Ordinal)
            .ToArray();

        var actualPackages = document
            .Descendants("PackageReference")
            .Select(e => (string)e.Attribute("Include")!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        actualProjects.ShouldBe(
            projects.Order(StringComparer.Ordinal).ToArray(),
            $"{projectPath}: project references changed. Update this allowlist only as a deliberate architecture decision.");

        actualPackages.ShouldBe(
            packages.Order(StringComparer.Ordinal).ToArray(),
            $"{projectPath}: package references changed. Review the new dependency (license, maintenance, security), then update this allowlist.");
    }
}
