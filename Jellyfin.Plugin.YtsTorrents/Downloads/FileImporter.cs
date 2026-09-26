using System.IO;
using Jellyfin.Plugin.YtsTorrents.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

/// <summary>
/// Places one downloaded file into the library. Every mode writes to "&lt;dest&gt;.partial" first and
/// renames it into place only once complete, so an import interrupted mid-copy (e.g. Jellyfin shutting
/// down during a multi-GB copy) never leaves a truncated video under its real name for the library scan
/// to pick up -- Jellyfin ignores the ".partial" extension, and the retried import overwrites it.
/// </summary>
public static class FileImporter
{
    public const string PartialSuffix = ".partial";

    public static void Place(string sourcePath, string destinationPath, ImportMode mode, ILogger logger)
    {
        var partialPath = destinationPath + PartialSuffix;

        // A leftover from an earlier interrupted attempt would make link() fail with EEXIST.
        File.Delete(partialPath);

        switch (mode)
        {
            case ImportMode.Hardlink:
                HardLinkHelper.CreateOrCopy(sourcePath, partialPath, logger);
                break;
            case ImportMode.Move:
                // Across filesystems File.Move is copy-then-delete, so the source survives until the
                // copy to partialPath has completed.
                File.Move(sourcePath, partialPath, true);
                break;
            case ImportMode.Copy:
            default:
                File.Copy(sourcePath, partialPath, true);
                break;
        }

        // Same directory, so this rename is atomic.
        File.Move(partialPath, destinationPath, true);
    }
}
