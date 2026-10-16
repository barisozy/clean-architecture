using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace ArchitectureTests;

public class DockerfileRestoreTests
{
    [Theory]
    [InlineData("Dockerfile")]
    [InlineData("Dockerfile (.NET 8)")]
    [InlineData("Dockerfile (.NET 9)")]
    public void Dockerfile_Should_CopyEverythingRestoreNeeds(string dockerfileName)
    {
        string webApiProject = RepoFiles.Find("src/Web.Api/Web.Api.csproj");
        string repoRoot = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(webApiProject)!))!;
        string dockerfilePath = Path.Combine(Path.GetDirectoryName(webApiProject)!, dockerfileName);

        File.Exists(dockerfilePath).ShouldBeTrue($"{dockerfileName} is missing.");

        List<string> copied = ParseCopiedFiles(File.ReadAllText(dockerfilePath));

        // Central Package Management needs both files on disk before restore runs.
        copied.ShouldContain("Directory.Packages.props");
        copied.ShouldContain("Directory.Build.props");

        foreach (string project in RestoreProjects(webApiProject, repoRoot))
        {
            copied.ShouldContain(
                project,
                $"{dockerfileName} must COPY {project} before running restore.");
        }
    }

    // COPY ["src/Web.Api/Web.Api.csproj", "src/Web.Api/"]
    private static List<string> ParseCopiedFiles(string dockerfile) =>
        Regex.Matches(dockerfile, @"COPY \[[""']([^""']+)[""']")
            .Select(match => match.Groups[1].Value.Replace('\\', '/'))
            .ToList();

    private static List<string> RestoreProjects(string webApiProject, string repoRoot)
    {
        var projects = new HashSet<string>(StringComparer.Ordinal)
        {
            Path.GetRelativePath(repoRoot, webApiProject).Replace('\\', '/')
        };
        var pending = new Queue<string>();
        pending.Enqueue(webApiProject);

        while (pending.Count > 0)
        {
            string current = pending.Dequeue();

            foreach (XElement reference in XDocument.Load(current).Descendants("ProjectReference"))
            {
                string include = ((string)reference.Attribute("Include")!).Replace('\\', '/');
                string referenced = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(current)!, include));

                if (projects.Add(Path.GetRelativePath(repoRoot, referenced).Replace('\\', '/')))
                {
                    pending.Enqueue(referenced);
                }
            }
        }

        return projects.Order(StringComparer.Ordinal).ToList();
    }
}
