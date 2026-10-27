using System.Text.RegularExpressions;
using DurLineScanner.Controls;

namespace DurLineScanner.Tests;

public class HelpTests
{
    [Theory]
    [InlineData("9. Registerkarte Ebenen & Pegel", "9-registerkarte-ebenen--pegel")]
    [InlineData("4.2 Receiver / Senderliste", "42-receiver--senderliste")]
    [InlineData("6. Einen Scan durchführen", "6-einen-scan-durchführen")]
    public void Anchor_FollowsGitHubRules(string heading, string expected) =>
        Assert.Equal(expected, MarkdownDocument.Anchor(heading));

    [Theory]
    [InlineData("BENUTZERHANDBUCH.md")]
    [InlineData("USER_GUIDE.md")]
    public void Guide_AllSectionLinksResolve(string file)
    {
        var text = File.ReadAllText(Path.Combine(RepoRoot(), "docs", file));
        var anchors = Regex.Matches(text, @"^#{1,6}\s+(.*)$", RegexOptions.Multiline)
            .Select(m => MarkdownDocument.Anchor(m.Groups[1].Value.Trim())).ToHashSet();
        var links = Regex.Matches(text, @"\]\(#([^)]+)\)").Select(m => m.Groups[1].Value).ToList();

        Assert.NotEmpty(links);
        Assert.All(links, link => Assert.Contains(link, anchors));
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "DurLineScanner.slnx"))) dir = dir.Parent;
        return dir?.FullName ?? throw new DirectoryNotFoundException("Projektordner nicht gefunden");
    }
}
