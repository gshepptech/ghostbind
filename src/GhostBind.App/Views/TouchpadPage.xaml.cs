using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GhostBind.Core.Mapping;

namespace GhostBind.App.Views;

public partial class TouchpadPage : Page
{
    private bool _suppressEvents = true;
    private ProcessedSnapshot _latest;
    private bool _hasLatest;

    public TouchpadPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _suppressEvents = true;
        try
        {
            var t = AppHost.Controller.CurrentProfile.Touchpad;
            AsMouse.IsChecked = t.AsMouse;
            Sens.Value = t.Sensitivity;
            InvX.IsChecked = t.InvertX;
            InvY.IsChecked = t.InvertY;
        }
        finally { _suppressEvents = false; }

        AppHost.Controller.StateUpdated += OnStateUpdated;
        CompositionTarget.Rendering += OnRender;
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        AppHost.Controller.StateUpdated -= OnStateUpdated;
        CompositionTarget.Rendering -= OnRender;
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e) => Commit();
    private void OnChanged(object sender, RoutedEventArgs e) => Commit();

    private void Commit()
    {
        if (_suppressEvents) return;
        var t = AppHost.Controller.CurrentProfile.Touchpad;
        t.AsMouse = AsMouse.IsChecked == true;
        t.Sensitivity = Sens.Value;
        t.InvertX = InvX.IsChecked == true;
        t.InvertY = InvY.IsChecked == true;
    }

    private void OnStateUpdated(object? sender, ProcessedSnapshot snap)
    {
        _latest = snap;
        _hasLatest = true;
    }

    private void OnRender(object? sender, EventArgs e)
    {
        if (!_hasLatest) return;
        var raw = _latest.Raw;
        Finger1Text.Text = raw.Finger1.Active
            ? $"Finger 1: x={raw.Finger1.X,4} y={raw.Finger1.Y,4} (id {raw.Finger1.Id})"
            : "Finger 1: (no contact)";
        Finger2Text.Text = raw.Finger2.Active
            ? $"Finger 2: x={raw.Finger2.X,4} y={raw.Finger2.Y,4} (id {raw.Finger2.Id})"
            : "Finger 2: (no contact)";
    }
}
