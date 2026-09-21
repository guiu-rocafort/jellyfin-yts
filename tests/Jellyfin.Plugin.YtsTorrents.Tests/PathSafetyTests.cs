using System;
using System.IO;
using Jellyfin.Plugin.YtsTorrents.Downloads;

namespace Jellyfin.Plugin.YtsTorrents.Tests;

/// <summary>
/// Adversarial fuzz of MovieFolderNamer.BuildTargetDir -- the single place a YTS-search-result title
/// (attacker-influenced if the API is ever compromised or spoofed) turns into a real filesystem path.
/// None of these inputs should ever resolve outside the configured library root.
/// </summary>
public class PathSafetyTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "yts-path-safety-root");

    [Theory]
    [InlineData("../../../etc/passwd")]
    [InlineData("..")]
    [InlineData("../..")]
    [InlineData("/etc/passwd")]
    [InlineData("C:\\Windows\\System32")]
    [InlineData("....//....//etc")]
    [InlineData("Title/With/Slashes")]
    [InlineData("Title\\With\\Backslashes")]
    public void BuildTargetDir_NeverEscapesLibraryRoot(string maliciousTitle)
    {
        var target = MovieFolderNamer.BuildTargetDir(Root, maliciousTitle, 2000);

        var rootWithSeparator = Path.GetFullPath(Root) + Path.DirectorySeparatorChar;
        Assert.StartsWith(rootWithSeparator, target, StringComparison.Ordinal);
        Assert.NotEqual(Path.GetFullPath(Root), target);
    }

    [Fact]
    public void BuildTargetDir_RejectsEmptyLibraryRoot()
    {
        Assert.Throws<ArgumentException>(() => MovieFolderNamer.BuildTargetDir(string.Empty, "Sintel", 2010));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildTargetDir_RejectsBlankTitle(string blankTitle)
    {
        Assert.Throws<ArgumentException>(() => MovieFolderNamer.BuildTargetDir(Root, blankTitle, 2010));
    }
}
