using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GhostBind.Core.Mapping;
using GhostBind.Core.Profiles;

namespace GhostBind.App.Views;

public partial class SticksPage : Page
{
    // Stays true through InitializeComponent so slider Minimum-coercion-driven
    // ValueChanged events don't fire CommitLeft/CommitRight before named fields exist.
    private bool _suppressEvents = true;
    private ProcessedSnapshot _latest;
    private bool _hasLatest;

    public SticksPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        // Page is cached by NavigationView — re-bind from current profile every
        // time we become visible so external changes (e.g. preset apply) show up.
        IsVisibleChanged += OnIsVisibleChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshFromProfile();
        AppHost.Controller.StateUpdated += OnStateUpdated;
        CompositionTarget.Rendering += OnRender;
    }

    private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is true) RefreshFromProfile();
    }

    private void RefreshFromProfile()
    {
        LoadFromProfile();
        var p = AppHost.Controller.CurrentProfile;
        LeftCurveEditor.Bind(p.LeftStick.CustomPoints, () => { });
        RightCurveEditor.Bind(p.RightStick.CustomPoints, () => { });
        UpdateCurveEditorVisibility();
    }

    private void UpdateCurveEditorVisibility()
    {
        LeftCurveEditor.Visibility = (LeftCurve.SelectedItem is CurveType.Custom)
            ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
        RightCurveEditor.Visibility = (RightCurve.SelectedItem is CurveType.Custom)
            ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
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
            ApplyToControls(p.LeftStick, LeftInnerDz, LeftOuterDz, LeftAntiDz, LeftSens, LeftCurve, LeftExp, LeftInvertX, LeftInvertY, LeftAntiSnap);
            ApplyToControls(p.RightStick, RightInnerDz, RightOuterDz, RightAntiDz, RightSens, RightCurve, RightExp, RightInvertX, RightInvertY, RightAntiSnap);
        }
        finally { _suppressEvents = false; }
    }

    private static void ApplyToControls(StickConfig cfg, Slider inner, Slider outer, Slider antiDz, Slider sens,
        ComboBox curve, Slider exp, CheckBox invX, CheckBox invY, CheckBox antiSnap)
    {
        inner.Value = cfg.InnerDeadzone;
        outer.Value = cfg.OuterDeadzone;
        antiDz.Value = cfg.AntiDeadzone;
        sens.Value = cfg.Sensitivity;
        curve.SelectedItem = cfg.Curve;
        exp.Value = cfg.CurveExponent;
        invX.IsChecked = cfg.InvertX;
        invY.IsChecked = cfg.InvertY;
        antiSnap.IsChecked = cfg.AntiSnapback;
    }

    private void OnLeftSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => CommitLeft();
    private void OnLeftCurveChanged(object sender, SelectionChangedEventArgs e) { CommitLeft(); UpdateCurveEditorVisibility(); }
    private void OnLeftInvertChanged(object sender, RoutedEventArgs e) => CommitLeft();
    private void OnRightSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => CommitRight();
    private void OnRightCurveChanged(object sender, SelectionChangedEventArgs e) { CommitRight(); UpdateCurveEditorVisibility(); }
    private void OnRightInvertChanged(object sender, RoutedEventArgs e) => CommitRight();

    private void CommitLeft()
    {
        if (_suppressEvents) return;
        var cfg = AppHost.Controller.CurrentProfile.LeftStick;
        cfg.InnerDeadzone = LeftInnerDz.Value;
        cfg.OuterDeadzone = LeftOuterDz.Value;
        cfg.AntiDeadzone = LeftAntiDz.Value;
        cfg.Sensitivity = LeftSens.Value;
        if (LeftCurve.SelectedItem is CurveType ct) cfg.Curve = ct;
        cfg.CurveExponent = LeftExp.Value;
        cfg.InvertX = LeftInvertX.IsChecked == true;
        cfg.InvertY = LeftInvertY.IsChecked == true;
        cfg.AntiSnapback = LeftAntiSnap.IsChecked == true;
    }

    private void CommitRight()
    {
        if (_suppressEvents) return;
        var cfg = AppHost.Controller.CurrentProfile.RightStick;
        cfg.InnerDeadzone = RightInnerDz.Value;
        cfg.OuterDeadzone = RightOuterDz.Value;
        cfg.AntiDeadzone = RightAntiDz.Value;
        cfg.Sensitivity = RightSens.Value;
        if (RightCurve.SelectedItem is CurveType ct) cfg.Curve = ct;
        cfg.CurveExponent = RightExp.Value;
        cfg.InvertX = RightInvertX.IsChecked == true;
        cfg.InvertY = RightInvertY.IsChecked == true;
        cfg.AntiSnapback = RightAntiSnap.IsChecked == true;
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
        var p = AppHost.Controller.CurrentProfile;
        LeftViz.Update(snap.LeftRawX, snap.LeftRawY, snap.LeftX, snap.LeftY,
            p.LeftStick.InnerDeadzone, p.LeftStick.OuterDeadzone);
        RightViz.Update(snap.RightRawX, snap.RightRawY, snap.RightX, snap.RightY,
            p.RightStick.InnerDeadzone, p.RightStick.OuterDeadzone);
    }
}
