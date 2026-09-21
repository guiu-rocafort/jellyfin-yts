using System;
using System.IO;
using Jellyfin.Plugin.YtsTorrents.Downloads;

namespace Jellyfin.Plugin.YtsTorrents.Tests;

public class MovieFolderNamerTests
{
    [Theory]
    [InlineData("Se7en", "Se7en")]
    [InlineData("The Matrix: Reloaded", "The Matrix  Reloaded")]
    [InlineData("Rated: R*/?", "Rated  R  ")]
    public void SanitizeTitle_StripsInvalidCharacters(string input, string expectedPrefix)
    {
        var sanitized = MovieFolderNamer.SanitizeTitle(input);

        Assert.Equal(expectedPrefix.Trim(), sanitized.Trim());
    }

    [Fact]
    public void BuildTargetDir_ProducesTitleYearFolderUnderRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "yts-tests-root");

        var target = MovieFolderNamer.BuildTargetDir(root, "Sintel", 2010);

        Assert.Equal(Path.GetFullPath(Path.Combine(root, "Sintel (2010)")), target);
    }

    [Theory]
    [InlineData("../../etc")]
    [InlineData("..\\..\\Windows")]
    [InlineData("../")]
    public void BuildTargetDir_RejectsPathTraversalAttempts(string maliciousTitle)
    {
        var root = Path.Combine(Path.GetTempPath(), "yts-tests-root");

        // Slashes are sanitized to spaces before the path is built, so traversal segments can never
        // survive into a real ".." path component -- this asserts the result always stays under root.
        var target = MovieFolderNamer.BuildTargetDir(root, maliciousTitle, 1999);

        var rootWithSeparator = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        Assert.StartsWith(rootWithSeparator, target, StringComparison.Ordinal);
    }
}
