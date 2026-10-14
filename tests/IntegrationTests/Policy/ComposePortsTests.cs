using Shouldly;

namespace IntegrationTests.Policy;

public class ComposePortsTests
{
    private const string Loopback = "127.0.0.1:";

    [Theory]
    [InlineData("docker-compose.yml")]
    [InlineData("docker-compose.override.yml")]
    public void PublishedPorts_Should_BindToLoopbackOnly(string composeFile)
    {
        List<string> offenders = [];
        int? portsIndent = null;

        foreach (string raw in File.ReadAllLines(FindUp(composeFile)))
        {
            string line = raw.TrimEnd();
            string trimmed = line.TrimStart();

            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
            {
                continue;
            }

            int indent = line.Length - trimmed.Length;

            if (trimmed == "ports:")
            {
                portsIndent = indent;
                continue;
            }

            if (portsIndent is int blockIndent)
            {
                if (indent > blockIndent && trimmed.StartsWith("- ", StringComparison.Ordinal))
                {
                    string entry = trimmed[2..];
                    int comment = entry.IndexOf(" #", StringComparison.Ordinal);

                    if (comment >= 0)
                    {
                        entry = entry[..comment];
                    }

                    entry = entry.Trim().Trim('"', '\'');

                    if (!entry.StartsWith(Loopback, StringComparison.Ordinal))
                    {
                        offenders.Add(entry);
                    }

                    continue;
                }

                portsIndent = null;
            }
        }

        offenders.ShouldBeEmpty($"Ports not bound to {Loopback}: {string.Join(", ", offenders)}");
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
