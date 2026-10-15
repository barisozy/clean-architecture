using Shouldly;

namespace ArchitectureTests;

public class ClockUsageTests
{
    private static readonly string[] BannedClockMembers =
    [
        "DateTime.UtcNow",
        "DateTime.Now",
        "DateTime.Today",
        "DateTimeOffset.UtcNow",
        "DateTimeOffset.Now",
        "TimeProvider.System"
    ];

    private static readonly string[] ExcludedDirectories = ["bin", "obj", "Migrations"];

    private static readonly string AllowedFile = Path.Combine("Infrastructure", "Time", "DateTimeProvider.cs");

    [Fact]
    public void Source_Should_ReadTheClockOnlyThroughDateTimeProvider()
    {
        string sharedKernelProject = RepoFiles.Find("src/SharedKernel/SharedKernel.csproj");
        string source = Path.GetDirectoryName(Path.GetDirectoryName(sharedKernelProject))!;
        List<string> offenders = [];

        foreach (string file in Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(source, file);

            if (string.Equals(relative, AllowedFile, StringComparison.Ordinal) ||
                relative.Split(Path.DirectorySeparatorChar).Any(segment => ExcludedDirectories.Contains(segment)))
            {
                continue;
            }

            string content = File.ReadAllText(file);

            foreach (string member in BannedClockMembers)
            {
                if (content.Contains(member, StringComparison.Ordinal))
                {
                    offenders.Add($"{relative}: {member}");
                }
            }
        }

        offenders.ShouldBeEmpty($"Use IDateTimeProvider instead of: {string.Join("; ", offenders)}");
    }
}
