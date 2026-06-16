using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace GhostBind.App;

// Downloads the latest HidHide MSI from the official Nefarius GitHub release
// and runs it elevated. Used on first launch when HidHide isn't detected.
public static class HidHideInstaller
{
    public sealed record InstallResult(bool Succeeded, string Detail);

    public static InstallResult Install()
    {
        try
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("GhostBind/1.0");
            http.Timeout = TimeSpan.FromSeconds(60);

            // Find the MSI asset on the latest release.
            var json = http.GetStringAsync("https://api.github.com/repos/nefarius/HidHide/releases/latest").Result;
            var doc = JsonDocument.Parse(json);
            string? msiUrl = null;
            string? msiName = null;
            foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString();
                if (name != null && name.EndsWith(".msi", StringComparison.OrdinalIgnoreCase))
                {
                    msiUrl = asset.GetProperty("browser_download_url").GetString();
                    msiName = name;
                    break;
                }
            }
            if (msiUrl == null) return new InstallResult(false, "No .msi asset found on the latest HidHide release.");

            // Download.
            var bytes = http.GetByteArrayAsync(msiUrl).Result;
            var tempPath = Path.Combine(Path.GetTempPath(), msiName ?? "HidHide_install.msi");
            File.WriteAllBytes(tempPath, bytes);

            // Run the installer elevated. msiexec /i <path> /qb /norestart shows
            // a basic progress UI and avoids restarting the machine.
            var psi = new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                Arguments = $"/i \"{tempPath}\" /qb /norestart",
                UseShellExecute = true,
                Verb = "runas", // triggers UAC
            };
            var proc = Process.Start(psi);
            if (proc == null) return new InstallResult(false, "Could not launch msiexec.exe.");

            proc.WaitForExit();
            // msiexec exit code 0 = success, 3010 = success but reboot required.
            return proc.ExitCode is 0 or 3010
                ? new InstallResult(true, $"Installed (exit {proc.ExitCode}).")
                : new InstallResult(false, $"msiexec exit code {proc.ExitCode}.");
        }
        catch (Exception ex)
        {
            return new InstallResult(false, ex.Message);
        }
    }
}
