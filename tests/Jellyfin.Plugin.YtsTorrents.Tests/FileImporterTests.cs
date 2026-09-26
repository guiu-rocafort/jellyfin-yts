using System;
using System.IO;
using Jellyfin.Plugin.YtsTorrents.Configuration;
using Jellyfin.Plugin.YtsTorrents.Downloads;
using Microsoft.Extensions.Logging.Abstractions;

namespace Jellyfin.Plugin.YtsTorrents.Tests;

public sealed class FileImporterTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "yts-file-importer-" + Guid.NewGuid().ToString("N"));
    private readonly string _source;
    private readonly string _destination;

    public FileImporterTests()
    {
        Directory.CreateDirectory(Path.Combine(_root, "staging"));
        Directory.CreateDirectory(Path.Combine(_root, "library"));
        _source = Path.Combine(_root, "staging", "Sintel.mkv");
        _destination = Path.Combine(_root, "library", "Sintel.mkv");
        File.WriteAllText(_source, "full video");
    }

    public void Dispose() => Directory.Delete(_root, true);

    [Theory]
    [InlineData(ImportMode.Hardlink)]
    [InlineData(ImportMode.Copy)]
    [InlineData(ImportMode.Move)]
    public void Place_LeavesOnlyTheFinalFile(ImportMode mode)
    {
        FileImporter.Place(_source, _destination, mode, NullLogger.Instance);

        Assert.Equal("full video", File.ReadAllText(_destination));
        Assert.False(File.Exists(_destination + FileImporter.PartialSuffix));
        Assert.Equal(mode != ImportMode.Move, File.Exists(_source));
    }

    [Theory]
    [InlineData(ImportMode.Hardlink)]
    [InlineData(ImportMode.Copy)]
    [InlineData(ImportMode.Move)]
    public void Place_RetryAfterInterruptedAttempt_ReplacesLeftovers(ImportMode mode)
    {
        // What an import interrupted mid-copy leaves behind (and, before .partial staging existed, a
        // truncated file under the real name).
        File.WriteAllText(_destination + FileImporter.PartialSuffix, "trunc");
        File.WriteAllText(_destination, "trunc");

        FileImporter.Place(_source, _destination, mode, NullLogger.Instance);

        Assert.Equal("full video", File.ReadAllText(_destination));
        Assert.False(File.Exists(_destination + FileImporter.PartialSuffix));
    }
}
