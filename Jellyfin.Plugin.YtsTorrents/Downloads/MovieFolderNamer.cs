using System;
using System.IO;
using System.Linq;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

/// <summary>
/// Pure helpers for turning a movie title/year into a filesystem path under a library root, matching
/// Jellyfin's standard "Title (Year)/" movie folder convention. Kept free of I/O side effects (other than
/// the final Path.GetFullPath normalization) so it's unit testable, including against path-traversal input.
/// </summary>
public static class MovieFolderNamer
{
    // A fixed, OS-independent set rather than Path.GetInvalidFileNameChars(): on Linux that set is just
    // { '\0', '/' }, which would leave Windows-illegal characters like ':' or '*' in folder names --
    // harmless on ext4 today, but not portable if the library volume is ever shared over SMB, and not
    // what an admin reading "Title (Year)" folder names would expect.
    private static readonly char[] InvalidChars = { '\\', '/', ':', '*', '?', '"', '<', '>', '|', '\0' };

    public static string SanitizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title must not be empty.", nameof(title));
        }

        var chars = title.Select(c => InvalidChars.Contains(c) || char.IsControl(c) ? ' ' : c).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "Untitled" : sanitized;
    }

    public static string BuildTargetDir(string libraryRoot, string title, int year)
    {
        if (string.IsNullOrWhiteSpace(libraryRoot))
        {
            throw new ArgumentException("Library root must not be empty.", nameof(libraryRoot));
        }

        var folderName = $"{SanitizeTitle(title)} ({year})";
        var normalizedRoot = Path.GetFullPath(libraryRoot);
        var fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, folderName));

        var rootWithSeparator = normalizedRoot.EndsWith(Path.DirectorySeparatorChar)
            ? normalizedRoot
            : normalizedRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Resolved import path escapes the configured library folder.");
        }

        return fullPath;
    }
}
