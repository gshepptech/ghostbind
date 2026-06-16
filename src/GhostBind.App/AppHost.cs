using System.IO;
using GhostBind.Core;
using GhostBind.Core.Profiles;

namespace GhostBind.App;

// Service-locator style singleton — keeps page/viewmodel ctors parameterless so WPF-UI's
// page provider can instantiate them. A real DI container is a v1.1 thing.
public static class AppHost
{
    public static ControllerService Controller { get; } = new();
    public static ProfileStore ProfileStore { get; } = new();
    public static GameProfileSwitcher AutoSwitcher { get; } = new();
    public static string? ActiveProfileName { get; set; }

    public static HidHideManager.Status HidHideStatus { get; private set; }
    public static string? HidHideDetail { get; private set; }

    public static void Start()
    {
        // Carry forward profiles + autoswitch config from the old SheppSense folder
        // for users upgrading from the placeholder name. One-shot, idempotent.
        MigrateLegacyAppData();

        // Whitelist ourselves with HidHide before starting the controller loop —
        // without this, HidHide hides the DualSense from us and `DualSenseReader.Open`
        // returns "no controller found" even with the pad plugged in.
        var (status, detail) = HidHideManager.EnsureWhitelisted();

        // Bundled installer: if HidHide isn't on the system, prompt the user and
        // download + install it from the official Nefarius release. One-time first-
        // run setup; subsequent launches see status=Whitelisted and skip this.
        if (status == HidHideManager.Status.NotInstalled)
        {
            var msg = "HidHide isn't installed. GhostBind uses it to hide the physical " +
                      "DualSense from games so they only see the virtual Xbox controller.\n\n" +
                      "Install HidHide now? (~15 MB download from the official Nefarius GitHub release. " +
                      "You'll see a UAC prompt and a brief installer.)";
            var result = System.Windows.MessageBox.Show(
                msg, "GhostBind first-run setup",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                var install = HidHideInstaller.Install();
                detail = install.Detail;
                if (install.Succeeded)
                {
                    // Re-check now that the driver is installed.
                    (status, detail) = HidHideManager.EnsureWhitelisted();
                }
                else
                {
                    System.Windows.MessageBox.Show(
                        "HidHide install failed: " + install.Detail +
                        "\n\nGhostBind will still work, but games may see both your DualSense and the virtual Xbox pad.",
                        "GhostBind",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                }
            }
        }

        HidHideStatus = status;
        HidHideDetail = detail;

        var names = ProfileStore.ListNames();
        if (names.Count == 0)
        {
            ProfileStore.Save(Controller.CurrentProfile);
            ActiveProfileName = Controller.CurrentProfile.Name;
        }
        else
        {
            ActiveProfileName = names[0];
            Controller.CurrentProfile = ProfileStore.Load(ActiveProfileName);
        }

        Controller.Start();
        AutoSwitcher.Start();
    }

    private static void MigrateLegacyAppData()
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var oldDir = Path.Combine(appData, "SheppSense");
            var newDir = Path.Combine(appData, "GhostBind");

            if (Directory.Exists(oldDir) && !Directory.Exists(newDir))
            {
                Directory.Move(oldDir, newDir);
            }
        }
        catch
        {
            // Migration is best-effort. If it fails (file in use, perms, whatever)
            // we just proceed with a fresh GhostBind directory — the user can
            // re-create their profiles manually.
        }
    }

    public static void Stop()
    {
        AutoSwitcher.Dispose();
        if (ActiveProfileName != null)
        {
            try { ProfileStore.Save(Controller.CurrentProfile); } catch { }
        }
        Controller.Dispose();
    }
}
