using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using GhostBind.Core.Input;
using GhostBind.Core.Mapping;

namespace GhostBind.App.Views;

public partial class SettingsPage : Page
{
    private bool _capturing;
    private DualSenseButton _lastCapturedButtons;
    private DateTime _captureStartedAt;
    private readonly StringBuilder _captureBuffer = new();
    private int _captureLineCount;

    public SettingsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.PropertyChanged += OnControllerChanged;
        AppHost.Controller.StateUpdated += OnStateForCapture;
        CompositionTarget.Rendering += OnRender;
        PollDelay.Value = AppHost.Controller.PollingDelayMicros;
        DebounceSlider.Value = AppHost.Controller.ButtonDebounceMs;
        Refresh();
    }

    private void OnCaptureStart(object sender, RoutedEventArgs e)
    {
        _captureBuffer.Clear();
        _captureLineCount = 0;
        _capturing = true;
        _captureStartedAt = DateTime.UtcNow;
        _lastCapturedButtons = DualSenseButton.None;
        CaptureLog.Text = "(capturing… tap a button)";
    }

    private void OnCaptureStop(object sender, RoutedEventArgs e) => _capturing = false;

    private void OnCaptureClear(object sender, RoutedEventArgs e)
    {
        _capturing = false;
        _captureBuffer.Clear();
        _captureLineCount = 0;
        CaptureLog.Text = "(idle — click Start to begin capturing)";
    }

    private void OnStateForCapture(object? sender, ProcessedSnapshot snap)
    {
        if (!_capturing) return;

        var now = DateTime.UtcNow;
        var current = snap.Raw.Buttons;
        if (current == _lastCapturedButtons) return;

        var changed = current ^ _lastCapturedButtons;
        var elapsed = (now - _captureStartedAt).TotalMilliseconds;

        // Build all the transition lines for this frame.
        foreach (DualSenseButton b in Enum.GetValues<DualSenseButton>())
        {
            if (b == DualSenseButton.None) continue;
            if ((changed & b) == 0) continue;
            bool nowPressed = (current & b) != 0;
            _captureBuffer.AppendLine($"{elapsed,9:F1} ms  {(nowPressed ? "DOWN" : " UP "),-4}  {b}");
            _captureLineCount++;
        }
        _lastCapturedButtons = current;

        // Trim if huge — keep only the last 200 lines
        if (_captureLineCount > 200)
        {
            var lines = _captureBuffer.ToString().Split('\n');
            _captureBuffer.Clear();
            for (int i = lines.Length - 200; i < lines.Length; i++)
                _captureBuffer.AppendLine(lines[i]);
            _captureLineCount = 200;
        }

        // Update UI on the dispatcher; debounce by only updating on change.
        Dispatcher.BeginInvoke(() =>
        {
            CaptureLog.Text = _captureBuffer.ToString();
            CaptureScroll.ScrollToEnd();
        });
    }

    private void OnPollDelayChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppHost.Controller.PollingDelayMicros = (int)PollDelay.Value;
    }

    private void OnDebounceChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        AppHost.Controller.ButtonDebounceMs = (int)DebounceSlider.Value;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.PropertyChanged -= OnControllerChanged;
        AppHost.Controller.StateUpdated -= OnStateForCapture;
        CompositionTarget.Rendering -= OnRender;
        _capturing = false;
    }

    private void OnRender(object? sender, EventArgs e)
    {
        var c = AppHost.Controller;
        PollRateText.Text = c.PollRateHz > 0 ? $"{c.PollRateHz,6:F0} Hz   ({1000.0 / c.PollRateHz,5:F2} ms / packet)" : "—";
        FrameTimeText.Text = c.FrameProcessingMicros > 0 ? $"{c.FrameProcessingMicros,6:F0} μs   ({c.FrameProcessingMicros / 1000.0,5:F2} ms)" : "—";
        WriteStatusText.Text = c.LastOutputWriteOk
            ? "OK  (last write succeeded)"
            : c.LastWriteError != null
                ? $"FAILED  ·  {c.LastWriteError}"
                : "(not yet written)";
    }

    private void OnControllerChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.BeginInvoke(Refresh);
    }

    private void Refresh()
    {
        var c = AppHost.Controller;
        ServiceStatusText.Text = $"{c.Status}: {c.StatusMessage}";
        HidHideStatusText.Text = AppHost.HidHideStatus switch
        {
            HidHideManager.Status.NotInstalled => "HidHide is not installed (optional).",
            HidHideManager.Status.WhitelistedAlready => "HidHide: GhostBind is whitelisted.",
            HidHideManager.Status.WhitelistedNow => "HidHide: GhostBind was just added to the whitelist.",
            HidHideManager.Status.Failed => "HidHide: whitelist failed — " + (AppHost.HidHideDetail ?? "unknown error"),
            _ => "HidHide: unknown status.",
        };
    }

    private void OnRestart(object sender, RoutedEventArgs e) => AppHost.Controller.Restart();
    private void OnStop(object sender, RoutedEventArgs e) => AppHost.Controller.Stop();

    private void OnNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
