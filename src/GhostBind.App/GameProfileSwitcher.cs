using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace GhostBind.App;

// Polls the foreground window every couple seconds, looks up the focused exe in
// AutoSwitchConfig, and switches the active profile when the user changes games.
public sealed class GameProfileSwitcher : IDisposable
{
    public AutoSwitchConfig Config { get; private set; } = AutoSwitchConfig.Load();

    public event Action<string>? ProfileSwitched;

    private DispatcherTimer? _timer;
    private string? _lastExe;

    public void Start()
    {
        if (_timer != null) return;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _timer.Tick += (_, _) => Tick();
        _timer.Start();
    }

    public void Stop()
    {
        _timer?.Stop();
        _timer = null;
    }

    public void Reload() => Config = AutoSwitchConfig.Load();

    public void Save() => Config.Save();

    private void Tick()
    {
        if (!Config.Enabled) return;

        var exe = ForegroundExeName();
        if (exe == null || exe == _lastExe) return;
        _lastExe = exe;

        if (!Config.ExeToProfile.TryGetValue(exe.ToLowerInvariant(), out var profileName)) return;

        try
        {
            var profile = AppHost.ProfileStore.Load(profileName);
            AppHost.Controller.CurrentProfile = profile;
            AppHost.ActiveProfileName = profileName;
            ProfileSwitched?.Invoke(profileName);
        }
        catch { /* missing profile, just ignore */ }
    }

    private static string? ForegroundExeName()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return null;
            var p = Process.GetProcessById((int)pid);
            return p.ProcessName; // already without .exe
        }
        catch { return null; }
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public void Dispose() => Stop();
}
