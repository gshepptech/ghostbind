using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GhostBind.App.Views;

public partial class AutoSwitchPage : Page
{
    private DispatcherTimer? _foregroundPoll;
    private ObservableCollection<MappingRow> _rows = new();

    public AutoSwitchPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppHost.AutoSwitcher.Reload();
        EnabledBox.IsChecked = AppHost.AutoSwitcher.Config.Enabled;

        NewProfile.ItemsSource = AppHost.ProfileStore.ListNames();
        if (NewProfile.Items.Count > 0) NewProfile.SelectedIndex = 0;

        RefreshMappings();

        // Show the user what's currently in focus so they can copy/paste the right exe name.
        _foregroundPoll = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _foregroundPoll.Tick += (_, _) => UpdateForegroundLabel();
        _foregroundPoll.Start();
        UpdateForegroundLabel();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _foregroundPoll?.Stop();
        _foregroundPoll = null;
    }

    private void UpdateForegroundLabel()
    {
        var exe = ForegroundExe();
        CurrentForegroundText.Text = exe is null
            ? "Foreground: (unknown)"
            : $"Foreground: {exe}";
    }

    private void RefreshMappings()
    {
        _rows = new ObservableCollection<MappingRow>(
            AppHost.AutoSwitcher.Config.ExeToProfile
                .Select(kv => new MappingRow { Exe = kv.Key, Profile = kv.Value }));
        MappingsList.ItemsSource = _rows;
    }

    private void OnEnabledChanged(object sender, RoutedEventArgs e)
    {
        AppHost.AutoSwitcher.Config.Enabled = EnabledBox.IsChecked == true;
        AppHost.AutoSwitcher.Save();
    }

    private void OnAdd(object sender, RoutedEventArgs e)
    {
        var exe = NewExe.Text?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(exe)) return;
        if (NewProfile.SelectedItem is not string profile) return;

        AppHost.AutoSwitcher.Config.ExeToProfile[exe] = profile;
        AppHost.AutoSwitcher.Save();
        NewExe.Text = "";
        RefreshMappings();
    }

    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is string exe)
        {
            AppHost.AutoSwitcher.Config.ExeToProfile.Remove(exe);
            AppHost.AutoSwitcher.Save();
            RefreshMappings();
        }
    }

    public class MappingRow
    {
        public string Exe { get; set; } = "";
        public string Profile { get; set; } = "";
    }

    private static string? ForegroundExe()
    {
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return null;
            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid == 0) return null;
            return Process.GetProcessById((int)pid).ProcessName.ToLowerInvariant();
        }
        catch { return null; }
    }

    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
}
