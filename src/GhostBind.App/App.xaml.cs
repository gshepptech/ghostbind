using System.Runtime.InteropServices;
using System.Windows;

namespace GhostBind.App;

public partial class App : Application
{
    // Named system mutex — second-launch detection. The "Local\\" prefix scopes to
    // the user's session so multiple users on the same box can each have their own
    // instance. "Global\\" would be cross-user.
    private const string MutexName = "Local\\GhostBind.SingleInstance";

    // Custom Win32 message ID, registered once per Windows session. Both the
    // primary instance and any subsequent launches resolve to the same uint, so
    // a broadcast PostMessage hits every top-level window in the session and
    // the primary's WndProc can wake the window up.
    public static readonly uint WmShowGhostBind = RegisterWindowMessage("GhostBind.WM_SHOW");

    private static readonly IntPtr HWND_BROADCAST = (IntPtr)0xFFFF;

    private Mutex? _instanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        // Try to claim the single-instance mutex. If another instance already owns
        // it, broadcast our "please surface" message and exit. The primary's
        // MainWindow listens for that message and restores itself.
        _instanceMutex = new Mutex(initiallyOwned: true, MutexName, out bool isFirstInstance);
        if (!isFirstInstance)
        {
            PostMessage(HWND_BROADCAST, WmShowGhostBind, IntPtr.Zero, IntPtr.Zero);
            Shutdown();
            return;
        }

        base.OnStartup(e);
        AppHost.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppHost.Stop();
        try { _instanceMutex?.ReleaseMutex(); } catch { /* never owned */ }
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);
}
