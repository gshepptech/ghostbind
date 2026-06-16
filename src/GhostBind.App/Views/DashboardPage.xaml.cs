using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GhostBind.Core.Input;
using GhostBind.Core.Mapping;

namespace GhostBind.App.Views;

public partial class DashboardPage : Page
{
    private static readonly DualSenseButton[] DisplayedButtons =
    {
        DualSenseButton.Cross, DualSenseButton.Circle, DualSenseButton.Square, DualSenseButton.Triangle,
        DualSenseButton.L1, DualSenseButton.R1, DualSenseButton.L2, DualSenseButton.R2,
        DualSenseButton.L3, DualSenseButton.R3,
        DualSenseButton.Create, DualSenseButton.Options, DualSenseButton.Ps,
        DualSenseButton.TouchpadClick, DualSenseButton.Mute,
    };

    private readonly Dictionary<DualSenseButton, Border> _buttonChips = new();
    private ProcessedSnapshot _latest;
    private bool _hasLatest;

    public DashboardPage()
    {
        InitializeComponent();

        foreach (var b in DisplayedButtons)
        {
            var chip = new Border
            {
                Background = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"],
                BorderBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"],
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(10, 6, 10, 6),
                Margin = new Thickness(4),
                Child = new TextBlock
                {
                    Text = b.ToString(),
                    Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                },
            };
            _buttonChips[b] = chip;
            ButtonsList.Items.Add(chip);
        }

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.StateUpdated += OnStateUpdated;
        CompositionTarget.Rendering += OnRender;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.StateUpdated -= OnStateUpdated;
        CompositionTarget.Rendering -= OnRender;
    }

    private void OnStateUpdated(object? sender, ProcessedSnapshot snap)
    {
        _latest = snap;
        _hasLatest = true;
    }

    private void OnRender(object? sender, EventArgs e)
    {
        if (!_hasLatest) return;
        var snap = _latest;
        var profile = AppHost.Controller.CurrentProfile;

        LeftStickViz.Update(snap.LeftRawX, snap.LeftRawY, snap.LeftX, snap.LeftY,
            profile.LeftStick.InnerDeadzone, profile.LeftStick.OuterDeadzone);
        RightStickViz.Update(snap.RightRawX, snap.RightRawY, snap.RightX, snap.RightY,
            profile.RightStick.InnerDeadzone, profile.RightStick.OuterDeadzone);

        LeftCoords.Text = $"{snap.LeftX,6:F2}, {snap.LeftY,6:F2}";
        RightCoords.Text = $"{snap.RightX,6:F2}, {snap.RightY,6:F2}";

        LeftTriggerBar.Value = snap.LeftTrigger;
        RightTriggerBar.Value = snap.RightTrigger;

        var accent = (Brush)Application.Current.Resources["SystemAccentColorBrush"];
        var idle = (Brush)Application.Current.Resources["ControlFillColorDefaultBrush"];
        foreach (var (btn, chip) in _buttonChips)
        {
            chip.Background = snap.Raw.Buttons.HasFlag(btn) ? accent : idle;
        }
    }
}
