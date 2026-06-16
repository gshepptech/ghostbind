using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GhostBind.Core.Mapping;

namespace GhostBind.App.Views;

public partial class TriggersPage : Page
{
    // Stays true through InitializeComponent so slider Minimum-coercion-driven
    // ValueChanged events don't fire Commit handlers before named fields exist.
    private bool _suppressEvents = true;
    private ProcessedSnapshot _latest;
    private bool _hasLatest;

    public TriggersPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        LoadFromProfile();
        AppHost.Controller.StateUpdated += OnStateUpdated;
        CompositionTarget.Rendering += OnRender;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true) LoadFromProfile();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.StateUpdated -= OnStateUpdated;
        CompositionTarget.Rendering -= OnRender;
    }

    private void LoadFromProfile()
    {
        _suppressEvents = true;
        try
        {
            var p = AppHost.Controller.CurrentProfile;

            LMode.ItemsSource = Enum.GetValues<TriggerEffectMode>();
            RMode.ItemsSource = Enum.GetValues<TriggerEffectMode>();

            LDz.Value = p.LeftTrigger.Deadzone;
            LAntiDz.Value = p.LeftTrigger.AntiDeadzone;
            LSat.Value = p.LeftTrigger.Saturation;
            LCurve.SelectedItem = p.LeftTrigger.Curve;
            LMode.SelectedItem = p.LeftTrigger.EffectMode;
            LStart.Value = p.LeftTrigger.EffectStart;
            LEnd.Value = p.LeftTrigger.EffectEnd;
            LForce.Value = p.LeftTrigger.EffectForce;

            RDz.Value = p.RightTrigger.Deadzone;
            RAntiDz.Value = p.RightTrigger.AntiDeadzone;
            RSat.Value = p.RightTrigger.Saturation;
            RCurve.SelectedItem = p.RightTrigger.Curve;
            RMode.SelectedItem = p.RightTrigger.EffectMode;
            RStart.Value = p.RightTrigger.EffectStart;
            REnd.Value = p.RightTrigger.EffectEnd;
            RForce.Value = p.RightTrigger.EffectForce;
        }
        finally { _suppressEvents = false; }
    }

    private void OnLEffectChanged(object sender, SelectionChangedEventArgs e) => CommitLeftEffect();
    private void OnLEffectNum(object sender, RoutedEventArgs e) => CommitLeftEffect();
    private void OnREffectChanged(object sender, SelectionChangedEventArgs e) => CommitRightEffect();
    private void OnREffectNum(object sender, RoutedEventArgs e) => CommitRightEffect();

    private static byte ClampByte(double? v, int min, int max) =>
        (byte)Math.Clamp((int)Math.Round(v ?? 0), min, max);

    private void CommitLeftEffect()
    {
        if (_suppressEvents) return;
        var t = AppHost.Controller.CurrentProfile.LeftTrigger;
        if (LMode.SelectedItem is TriggerEffectMode m) t.EffectMode = m;
        t.EffectStart = ClampByte(LStart.Value, 0, 9);
        t.EffectEnd = ClampByte(LEnd.Value, 0, 9);
        t.EffectForce = ClampByte(LForce.Value, 0, 255);
    }

    private void CommitRightEffect()
    {
        if (_suppressEvents) return;
        var t = AppHost.Controller.CurrentProfile.RightTrigger;
        if (RMode.SelectedItem is TriggerEffectMode m) t.EffectMode = m;
        t.EffectStart = ClampByte(RStart.Value, 0, 9);
        t.EffectEnd = ClampByte(REnd.Value, 0, 9);
        t.EffectForce = ClampByte(RForce.Value, 0, 255);
    }

    private void OnLSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => CommitLeft();
    private void OnLCurveChanged(object sender, SelectionChangedEventArgs e) => CommitLeft();
    private void OnRSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => CommitRight();
    private void OnRCurveChanged(object sender, SelectionChangedEventArgs e) => CommitRight();

    private void CommitLeft()
    {
        if (_suppressEvents) return;
        var t = AppHost.Controller.CurrentProfile.LeftTrigger;
        t.Deadzone = LDz.Value;
        t.AntiDeadzone = LAntiDz.Value;
        t.Saturation = LSat.Value;
        if (LCurve.SelectedItem is CurveType ct) t.Curve = ct;
    }

    private void CommitRight()
    {
        if (_suppressEvents) return;
        var t = AppHost.Controller.CurrentProfile.RightTrigger;
        t.Deadzone = RDz.Value;
        t.AntiDeadzone = RAntiDz.Value;
        t.Saturation = RSat.Value;
        if (RCurve.SelectedItem is CurveType ct) t.Curve = ct;
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
        LBar.Value = snap.LeftTrigger;
        RBar.Value = snap.RightTrigger;
        LValue.Text = $"{snap.LeftTrigger:F2}";
        RValue.Text = $"{snap.RightTrigger:F2}";
    }
}
