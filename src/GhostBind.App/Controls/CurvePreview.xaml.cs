using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using GhostBind.Core.Mapping;

namespace GhostBind.App.Controls;

// Small read-only graph that visualizes one curve type. Same math as the live
// engine (ResponseCurve.Apply), so the preview matches what the controller feels.
public partial class CurvePreview : UserControl
{
    public static readonly DependencyProperty CurveProperty =
        DependencyProperty.Register(nameof(Curve), typeof(CurveType), typeof(CurvePreview),
            new PropertyMetadata(CurveType.Linear, OnRenderPropertyChanged));

    public static readonly DependencyProperty ExponentProperty =
        DependencyProperty.Register(nameof(Exponent), typeof(double), typeof(CurvePreview),
            new PropertyMetadata(2.0, OnRenderPropertyChanged));

    public CurveType Curve
    {
        get => (CurveType)GetValue(CurveProperty);
        set => SetValue(CurveProperty, value);
    }

    public double Exponent
    {
        get => (double)GetValue(ExponentProperty);
        set => SetValue(ExponentProperty, value);
    }

    private static void OnRenderPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is CurvePreview p) p.Render();
    }

    public CurvePreview()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
    }

    private const double Pad = 6;

    private (double X, double Y) ToScreen(double inputNorm, double outputNorm)
    {
        double w = Surface.ActualWidth - Pad * 2;
        double h = Surface.ActualHeight - Pad * 2;
        return (Pad + inputNorm * w, Pad + (1 - outputNorm) * h);
    }

    private void Render()
    {
        Surface.Children.Clear();
        if (Surface.ActualWidth <= 0 || Surface.ActualHeight <= 0) return;

        var gridBrush = (Brush)Application.Current.Resources["ControlStrokeColorSecondaryBrush"];
        var refBrush = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"];
        var curveBrush = (Brush)Application.Current.Resources["SystemAccentColorBrush"];

        // Faint quartile grid
        for (int i = 1; i < 4; i++)
        {
            double t = i / 4.0;
            var (x1, y1) = ToScreen(t, 0);
            var (x2, y2) = ToScreen(t, 1);
            Surface.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = gridBrush, StrokeThickness = 0.4, Opacity = 0.5 });

            var (xa, ya) = ToScreen(0, t);
            var (xb, yb) = ToScreen(1, t);
            Surface.Children.Add(new Line { X1 = xa, Y1 = ya, X2 = xb, Y2 = yb, Stroke = gridBrush, StrokeThickness = 0.4, Opacity = 0.5 });
        }

        // Dashed reference line (linear) so you can see how each curve deviates.
        var (rx1, ry1) = ToScreen(0, 0);
        var (rx2, ry2) = ToScreen(1, 1);
        Surface.Children.Add(new Line
        {
            X1 = rx1, Y1 = ry1, X2 = rx2, Y2 = ry2,
            Stroke = refBrush, StrokeThickness = 0.7,
            StrokeDashArray = new DoubleCollection { 3, 3 }, Opacity = 0.55,
        });

        // The actual curve, sampled across the input range.
        var poly = new Polyline { Stroke = curveBrush, StrokeThickness = 1.6, StrokeLineJoin = PenLineJoin.Round };
        const int samples = 40;
        // Default custom points — only matters if Curve == Custom in the preview.
        var custom = new List<CurvePoint>
        {
            new() { Input = 0.00, Output = 0.00 },
            new() { Input = 0.25, Output = 0.15 },
            new() { Input = 0.50, Output = 0.50 },
            new() { Input = 0.75, Output = 0.85 },
            new() { Input = 1.00, Output = 1.00 },
        };

        for (int i = 0; i <= samples; i++)
        {
            double x = (double)i / samples;
            double y = ResponseCurve.Apply(x, Curve, Exponent, custom);
            var (sx, sy) = ToScreen(x, y);
            poly.Points.Add(new Point(sx, sy));
        }
        Surface.Children.Add(poly);
    }
}
