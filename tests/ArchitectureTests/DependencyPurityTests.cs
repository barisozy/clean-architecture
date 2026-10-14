using NetArchTest.Rules;

namespace ArchitectureTests;

public class DependencyPurityTests : BaseTest
{
    private static readonly string[] Frameworks =
    [
        "Microsoft.EntityFrameworkCore",
        "Microsoft.AspNetCore",
        "Microsoft.Extensions",
        "Serilog",
        "Npgsql",
        "Newtonsoft"
    ];

    [Fact]
    public void SharedKernel_Should_NotDependOnAnyOtherLayer()
    {
        TestResult result = Types.InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(
                DomainAssembly.GetName().Name!,
                ApplicationAssembly.GetName().Name!,
                InfrastructureAssembly.GetName().Name!,
                PresentationAssembly.GetName().Name!)
            .GetResult();

        ShouldPass(result);
    }

    [Fact]
    public void SharedKernel_Should_NotDependOnFrameworksOrLibraries()
    {
        TestResult result = Types.InAssembly(SharedKernelAssembly)
            .Should()
            .NotHaveDependencyOnAny(Frameworks)
            .GetResult();

        ShouldPass(result);
    }

    [Fact]
    public void Domain_Should_NotDependOnFrameworksOrLibraries()
    {
        TestResult result = Types.InAssembly(DomainAssembly)
            .Should()
            .NotHaveDependencyOnAny(Frameworks)
            .GetResult();

        ShouldPass(result);
    }

    [Fact]
    public void Application_Should_NotDependOnWebOrDatabaseDrivers()
    {
        TestResult result = Types.InAssembly(ApplicationAssembly)
            .Should()
            .NotHaveDependencyOnAny("Microsoft.AspNetCore", "Npgsql", "Newtonsoft")
            .GetResult();

        ShouldPass(result);
    }
}
