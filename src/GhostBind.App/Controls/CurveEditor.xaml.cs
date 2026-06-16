using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using GhostBind.Core.Mapping;

namespace GhostBind.App.Controls;

public partial class CurveEditor : UserControl
{
    private const double Pad = 16;            // canvas padding (so dots aren't flush to edge)
    private const double GridDivisions = 4;
    private const double DotSize = 12;
    private const double HitRadius = 14;

    private List<CurvePoint>? _points;
    private Action? _onChanged;
    private int _draggingIndex = -1;

    public CurveEditor()
    {
        InitializeComponent();
        Loaded += (_, _) => Render();
        SizeChanged += (_, _) => Render();
    }

    public void Bind(List<CurvePoint> points, Action onChanged)
    {
        _points = points;
        _onChanged = onChanged;
        Render();
    }

    private double PlotW => Surface.ActualWidth - Pad * 2;
    private double PlotH => Surface.ActualHeight - Pad * 2;

    private (double X, double Y) ToScreen(double inputNorm, double outputNorm)
    {
        double x = Pad + inputNorm * PlotW;
        // Y inverted — screen Y grows downward, output grows upward.
        double y = Pad + (1 - outputNorm) * PlotH;
        return (x, y);
    }

    private double ScreenToOutputNorm(double screenY)
    {
        double y = (screenY - Pad) / PlotH;
        return Math.Clamp(1 - y, 0, 1);
    }

    private void Render()
    {
        Surface.Children.Clear();
        if (_points == null || PlotW <= 0 || PlotH <= 0) return;

        var gridBrush = (Brush)Application.Current.Resources["ControlStrokeColorDefaultBrush"];
        var curveBrush = (Brush)Application.Current.Resources["SystemAccentColorBrush"];
        var lineBrush = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"];

        // Grid
        for (int i = 0; i <= GridDivisions; i++)
        {
            double t = i / GridDivisions;
            var (x1, y1) = ToScreen(t, 0);
            var (x2, y2) = ToScreen(t, 1);
            Surface.Children.Add(new Line { X1 = x1, Y1 = y1, X2 = x2, Y2 = y2, Stroke = gridBrush, StrokeThickness = 0.5, Opacity = 0.4 });

            var (xa, ya) = ToScreen(0, t);
            var (xb, yb) = ToScreen(1, t);
            Surface.Children.Add(new Line { X1 = xa, Y1 = ya, X2 = xb, Y2 = yb, Stroke = gridBrush, StrokeThickness = 0.5, Opacity = 0.4 });
        }

        // Reference 1:1 line (linear curve for visual context)
        var (rx1, ry1) = ToScreen(0, 0);
        var (rx2, ry2) = ToScreen(1, 1);
        Surface.Children.Add(new Line { X1 = rx1, Y1 = ry1, X2 = rx2, Y2 = ry2, Stroke = lineBrush, StrokeThickness = 1, StrokeDashArray = new DoubleCollection { 4, 4 }, Opacity = 0.5 });

        // The actual curve as a polyline through anchors
        var poly = new Polyline { Stroke = curveBrush, StrokeThickness = 2, StrokeLineJoin = PenLineJoin.Round };
        foreach (var p in _points)
        {
            var (px, py) = ToScreen(p.Input, p.Output);
            poly.Points.Add(new Point(px, py));
        }
        Surface.Children.Add(poly);

        // Anchor dots — endpoints non-draggable (smaller, dimmer), middles draggable.
        // Each draggable dot gets a live "input → output" label next to it so the
        // user knows the exact value as they drag.
        var labelBg = (Brush)Application.Current.Resources["SolidBackgroundFillColorBaseBrush"];
        for (int i = 0; i < _points.Count; i++)
        {
            bool isEnd = i == 0 || i == _points.Count - 1;
            var (cx, cy) = ToScreen(_points[i].Input, _points[i].Output);
            var dot = new Ellipse
            {
                Width = isEnd ? 8 : DotSize,
                Height = isEnd ? 8 : DotSize,
                Fill = isEnd ? lineBrush : curveBrush,
                Stroke = (Brush)Application.Current.Resources["ApplicationBackgroundBrush"],
                StrokeThickness = 1,
            };
            Canvas.SetLeft(dot, cx - dot.Width / 2);
            Canvas.SetTop(dot, cy - dot.Height / 2);
            Surface.Children.Add(dot);

            if (!isEnd)
            {
                var label = new TextBlock
                {
                    Text = $"{_points[i].Input:F2} → {_points[i].Output:F2}",
                    Foreground = curveBrush,
                    FontFamily = new FontFamily("Consolas, Cascadia Mono"),
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Background = labelBg,
                    Padding = new Thickness(3, 0, 3, 0),
                };
                // Position to the right of the dot, except the rightmost draggable
                // point — flip to the left so the label doesn't fall off the canvas.
                bool isRightmost = i == _points.Count - 2;
                Canvas.SetLeft(label, isRightmost ? cx - 76 : cx + 10);
                Canvas.SetTop(label, cy - 7);
                Surface.Children.Add(label);
            }
        }

        // Axis labels along the bottom (input) and left (output) edges.
        var axisBrush = (Brush)Application.Current.Resources["TextFillColorTertiaryBrush"];
        for (int i = 0; i <= 4; i++)
        {
            double t = i / 4.0;
            var (xb, yb) = ToScreen(t, 0);
            Surface.Children.Add(new TextBlock
            {
                Text = t.ToString("F2"),
                Foreground = axisBrush,
                FontFamily = new FontFamily("Consolas, Cascadia Mono"),
                FontSize = 9,
                Margin = new Thickness(xb - 10, yb + 2, 0, 0),
            });
            var (xl, yl) = ToScreen(0, t);
            Surface.Children.Add(new TextBlock
            {
                Text = t.ToString("F2"),
                Foreground = axisBrush,
                FontFamily = new FontFamily("Consolas, Cascadia Mono"),
                FontSize = 9,
                Margin = new Thickness(xl - 30, yl - 7, 0, 0),
            });
        }
    }

    private int HitTest(Point p)
    {
        if (_points == null) return -1;
        // Endpoints aren't draggable — start at index 1, end before last.
        for (int i = 1; i < _points.Count - 1; i++)
        {
            var (cx, cy) = ToScreen(_points[i].Input, _points[i].Output);
            if (Math.Abs(p.X - cx) <= HitRadius && Math.Abs(p.Y - cy) <= HitRadius)
                return i;
        }
        return -1;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_points == null) return;
        var pos = e.GetPosition(Surface);
        _draggingIndex = HitTest(pos);
        if (_draggingIndex >= 0) Surface.CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggingIndex < 0 || _points == null) return;
        var pos = e.GetPosition(Surface);
        var p = _points[_draggingIndex];
        p.Output = ScreenToOutputNorm(pos.Y);
        _points[_draggingIndex] = p;
        Render();
        _onChanged?.Invoke();
    }

    private void OnMouseUp(object sender, MouseEventArgs e)
    {
        if (_draggingIndex >= 0)
        {
            Surface.ReleaseMouseCapture();
            _draggingIndex = -1;
        }
    }
}
