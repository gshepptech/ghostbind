using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostBind.App.Views;

public partial class LightingPage : Page
{
    private bool _suppressEvents = true;

    public LightingPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _suppressEvents = true;
        try
        {
            var lb = AppHost.Controller.CurrentProfile.Lightbar;
            Enabled.IsChecked = lb.Enabled;
            R.Value = lb.Red;
            G.Value = lb.Green;
            B.Value = lb.Blue;
            UpdatePreview();
        }
        finally { _suppressEvents = false; }
    }

    private void OnSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        Commit();
        UpdatePreview();
    }

    private void OnChanged(object sender, RoutedEventArgs e) => Commit();

    private void Commit()
    {
        if (_suppressEvents) return;
        var lb = AppHost.Controller.CurrentProfile.Lightbar;
        lb.Enabled = Enabled.IsChecked == true;
        lb.Red = (byte)R.Value;
        lb.Green = (byte)G.Value;
        lb.Blue = (byte)B.Value;
    }

    private void UpdatePreview()
    {
        Preview.Background = new SolidColorBrush(Color.FromRgb((byte)R.Value, (byte)G.Value, (byte)B.Value));
    }

    private void OnPreset(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe) return;
        (byte r, byte g, byte b) = fe.Tag as string switch
        {
            "Red"    => ((byte)255, (byte)0,   (byte)0),
            "Green"  => ((byte)0,   (byte)200, (byte)0),
            "Blue"   => ((byte)0,   (byte)50,  (byte)200),
            "Purple" => ((byte)160, (byte)0,   (byte)200),
            "Orange" => ((byte)255, (byte)80,  (byte)0),
            "Teal"   => ((byte)0,   (byte)180, (byte)180),
            "Off"    => ((byte)0,   (byte)0,   (byte)0),
            _        => ((byte)0,   (byte)0,   (byte)0),
        };
        R.Value = r; G.Value = g; B.Value = b;
        Commit();
        UpdatePreview();
    }
}
