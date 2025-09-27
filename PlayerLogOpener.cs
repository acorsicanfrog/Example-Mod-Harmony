// ==================================================================================
// Utility: PlayerLogOpener (Cross-Platform)
//
// Simplified utility for opening Unity’s Player.log location, using 
// `Application.consoleLogPath` for path resolution.
//
// Key Features:
// - OpenPlayerLogLocation():
//      • Resolves Player.log path via `Application.consoleLogPath`.
//      • Opens the file (if present) or containing folder in the system’s file explorer:
//          ◦ Windows → explorer.exe (with /select support for highlighting the log file).
//          ◦ macOS   → open (with -R to reveal the file in Finder).
//          ◦ Linux   → xdg-open (opens the folder; file selection not widely supported).
//      • Falls back to user’s home directory if the expected folder does not exist.
//      • Logs warnings for unsupported platforms and errors on failure.
// - GetPlayerLogPath():
//      • Returns the runtime-resolved Player.log path from Unity directly.
// - GetHomeDirectory():
//      • Provides a cross-platform fallback to the user’s home directory.
//
// Usage:
// Call `PlayerLogOpener.OpenPlayerLogLocation()` to help users quickly access 
// their Player.log for troubleshooting and debugging.
// ==================================================================================


using System;
using System.IO;
using UnityEngine;
using System.Diagnostics;

public static class PlayerLogOpener
{
    /// <summary>
    /// Opens the folder containing the Player.log (and selects it when supported).
    /// If the exact file doesn't exist yet, opens the best-known directory.
    /// </summary>
    public static void OpenPlayerLogLocation()
    {
        string logPath = GetPlayerLogPath();

        string dir = Path.GetDirectoryName(logPath);

        // Ensure there's at least a directory to open.
        if (!Directory.Exists(dir))
        {
            // Fall back to user's home if the expected directory isn't there yet.
            dir = GetHomeDirectory();
        }

        try
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsEditor:
                    // Explorer can select a specific file if it exists.
                    if (File.Exists(logPath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = "/select,\"" + logPath.Replace('/', '\\') + "\"",
                            UseShellExecute = false
                        });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = "\"" + dir.Replace('/', '\\') + "\"",
                            UseShellExecute = false
                        });
                    }
                    break;

                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.OSXEditor:
                    // `open -R` reveals (selects) the file in Finder if it exists.
                    if (File.Exists(logPath))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "open",
                            Arguments = "-R \"" + logPath + "\"",
                            UseShellExecute = false
                        });
                    }
                    else
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "open",
                            Arguments = "\"" + dir + "\"",
                            UseShellExecute = false
                        });
                    }
                    break;

                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxEditor:
                    // Most distros support xdg-open; selecting a file isn't universally supported, so open the folder.
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "xdg-open",
                        Arguments = "\"" + dir + "\"",
                        UseShellExecute = false
                    });
                    break;

                default:
                    UnityEngine.Debug.LogWarning($"[MOD:{ExampleMod.modName}] {nameof(OpenPlayerLogLocation)}: Current platform not supported.");
                    break;
            }
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogError($"[MOD:{ExampleMod.modName}] {nameof(OpenPlayerLogLocation)} failed: " + ex.Message + "\nPath: " + logPath);
        }
    }

    public static string GetPlayerLogPath() => Application.consoleLogPath;

    static string GetHomeDirectory() => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
}