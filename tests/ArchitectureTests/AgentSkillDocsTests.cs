using Shouldly;

namespace ArchitectureTests;

public class AgentSkillDocsTests
{
    // Snippets in the agent skills must use APIs that actually exist. Both were taught
    // after they were renamed (UserErrors.Unauthorized) or banned (DateTime.Today).
    private static readonly string[] BannedSnippets =
    [
        "UserErrors.Unauthorized",
        "DateTime.Today"
    ];

    [Fact]
    public void SkillDocs_Should_NotTeachRemovedOrBannedApis()
    {
        string readme = RepoFiles.Find(".agents/skills/README.md");
        string agentsRoot = Path.GetDirectoryName(Path.GetDirectoryName(readme)!)!;
        List<string> offenders = [];

        foreach (string file in Directory.EnumerateFiles(agentsRoot, "*.md", SearchOption.AllDirectories))
        {
            string content = File.ReadAllText(file);
            string relative = Path.GetRelativePath(agentsRoot, file);

            foreach (string snippet in BannedSnippets)
            {
                if (content.Contains(snippet, StringComparison.Ordinal))
                {
                    offenders.Add($"{relative}: {snippet}");
                }
            }
        }

        offenders.ShouldBeEmpty($"Skill docs reference APIs that were removed or banned: {string.Join("; ", offenders)}");
    }
}
