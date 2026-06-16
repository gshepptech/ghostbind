using System.Diagnostics;
using Nefarius.Drivers.HidHide;

namespace GhostBind.App;

public static class HidHideManager
{
    public enum Status
    {
        NotInstalled,
        WhitelistedAlready,
        WhitelistedNow,
        Failed,
    }

    public static (Status Result, string? Detail) EnsureWhitelisted()
    {
        HidHideControlService hh;
        try
        {
            hh = new HidHideControlService();
        }
        catch
        {
            // Driver not present — service constructor throws. Treat as not installed.
            return (Status.NotInstalled, null);
        }

        try
        {
            var path = Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(path))
                return (Status.Failed, "Could not determine own executable path.");

            foreach (var p in hh.ApplicationPaths)
            {
                if (string.Equals(p, path, StringComparison.OrdinalIgnoreCase))
                    return (Status.WhitelistedAlready, path);
            }

            hh.AddApplicationPath(path);
            return (Status.WhitelistedNow, path);
        }
        catch (Exception ex)
        {
            return (Status.Failed, ex.Message);
        }
    }
}
