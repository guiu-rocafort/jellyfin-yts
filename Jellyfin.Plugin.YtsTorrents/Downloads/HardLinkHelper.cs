using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.YtsTorrents.Downloads;

/// <summary>
/// .NET has no built-in cross-platform hardlink API. This creates one via the platform's native call and
/// falls back to a plain copy on failure (most commonly EXDEV -- source and destination on different
/// filesystems/volumes, the classic Docker gotcha when qBittorrent's download dir and Jellyfin's library
/// dir aren't bind-mounted from the same host path).
/// </summary>
public static class HardLinkHelper
{
    [DllImport("libc", SetLastError = true, EntryPoint = "link")]
    private static extern int LinuxLink(string oldPath, string newPath);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CreateHardLink(string newFileName, string existingFileName, IntPtr securityAttributes);

    public static void CreateOrCopy(string sourcePath, string destinationPath, ILogger logger)
    {
        if (TryCreateHardLink(sourcePath, destinationPath))
        {
            return;
        }

        logger.LogInformation(
            "Could not hardlink {Source} -> {Destination} (likely on different filesystems); falling back to copy.",
            sourcePath,
            destinationPath);
        File.Copy(sourcePath, destinationPath, overwrite: true);
    }

    private static bool TryCreateHardLink(string sourcePath, string destinationPath)
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return CreateHardLinkWindows(destinationPath, sourcePath);
            }

            return LinuxLink(sourcePath, destinationPath) == 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        catch (EntryPointNotFoundException)
        {
            return false;
        }
    }

    [SupportedOSPlatform("windows")]
    private static bool CreateHardLinkWindows(string destinationPath, string sourcePath) =>
        CreateHardLink(destinationPath, sourcePath, IntPtr.Zero);
}
