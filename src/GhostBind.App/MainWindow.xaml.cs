using System.ComponentModel;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using GhostBind.App.Views;
using GhostBind.Core;
using Wpf.Ui.Controls;
using Color = System.Windows.Media.Color;

namespace GhostBind.App;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Closing += OnClosing;
        Closed += OnClosed;
        // Removed StateChanged → minimize handler — that was force-hiding the window
        // to tray on minimize, which made it look like there was no minimize button.
        // Standard taskbar-minimize is the right behavior.
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.PropertyChanged += OnControllerPropertyChanged;
        AppHost.Controller.StateUpdated += OnStateUpdated;
        AppHost.AutoSwitcher.ProfileSwitched += OnAutoSwitched;
        NotifyIcon.Icon = BuildNotifyIcon();
        UpdateStatusBar();
        RootNavigation.Navigate(typeof(DashboardPage));

        // Hook the Win32 message pump so a second-instance launch can wake us up.
        var hwnd = new WindowInteropHelper(this).Handle;
        var source = HwndSource.FromHwnd(hwnd);
        source?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if ((uint)msg == App.WmShowGhostBind)
        {
            // Another launch happened — surface this instance instead.
            RestoreFromTray();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void OnAutoSwitched(string profileName)
    {
        Dispatcher.BeginInvoke(() =>
        {
            NotifyIcon.ShowNotification(title: "GhostBind", message: $"Switched to '{profileName}'");
            UpdateStatusBar();
        });
    }

    private void OnStateUpdated(object? sender, GhostBind.Core.Mapping.ProcessedSnapshot snap)
    {
        var raw = snap.Raw;
        var text = raw.IsCharging
            ? $"⚡ {raw.BatteryPercent}%"
            : $"🔋 {raw.BatteryPercent}%";
        Dispatcher.BeginInvoke(() => BatteryText.Text = text);
    }

    // Loads the bundled multi-res ghost.ico from the assembly's resources and uses
    // it for the tray. Same icon embedded as the exe ApplicationIcon, so the tray,
    // taskbar, Alt-Tab, and Explorer shortcut all match.
    private static Icon BuildNotifyIcon()
    {
        var uri = new Uri("pack://application:,,,/Assets/ghost.ico", UriKind.Absolute);
        using var stream = System.Windows.Application.GetResourceStream(uri).Stream;
        return new Icon(stream, new System.Drawing.Size(32, 32));
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        // Default close button hides to tray. Real exit goes through tray menu.
        e.Cancel = true;
        Hide();
    }

    private void OnClosed(object? sender, System.EventArgs e)
    {
        AppHost.Controller.PropertyChanged -= OnControllerPropertyChanged;
        AppHost.Controller.StateUpdated -= OnStateUpdated;
        AppHost.AutoSwitcher.ProfileSwitched -= OnAutoSwitched;
        NotifyIcon.Dispose();
    }

    private void OnControllerPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(UpdateStatusBar);
    }

    private void OnTrayLeftClick(object sender, RoutedEventArgs e) => RestoreFromTray();
    private void OnTrayShow(object sender, RoutedEventArgs e) => RestoreFromTray();
    private void OnTrayRestart(object sender, RoutedEventArgs e) => AppHost.Controller.Restart();

    private void OnTrayExit(object sender, RoutedEventArgs e)
    {
        NotifyIcon.Dispose();
        Application.Current.Shutdown();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void UpdateStatusBar()
    {
        var ctrl = AppHost.Controller;
        StatusText.Text = $"{ctrl.Status}: {ctrl.StatusMessage}";
        ProfileText.Text = ctrl.CurrentProfile.Name;
        StatusDot.Fill = ctrl.Status switch
        {
            ServiceStatus.Running => new SolidColorBrush(Color.FromRgb(0x40, 0xC0, 0x57)),
            ServiceStatus.Connecting => new SolidColorBrush(Color.FromRgb(0xE0, 0xB0, 0x40)),
            ServiceStatus.NoController or ServiceStatus.ViGEmMissing or ServiceStatus.Error
                => new SolidColorBrush(Color.FromRgb(0xD0, 0x40, 0x40)),
            _ => new SolidColorBrush(Color.FromRgb(0x80, 0x80, 0x80)),
        };
    }
}
