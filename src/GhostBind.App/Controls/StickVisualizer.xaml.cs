using System.Windows.Controls;

namespace GhostBind.App.Controls;

public partial class StickVisualizer : UserControl
{
    private const double Radius = 100;
    private const double Center = 110;

    public StickVisualizer()
    {
        InitializeComponent();
    }

    public void Update(double rawX, double rawY, double procX, double procY, double innerDeadzone, double outerDeadzone)
    {
        // Note: WPF Y axis grows downward. Our processed coords have +Y = up,
        // so flip Y for screen position.
        double rawScreenX = Center + rawX * Radius - 5;     // -5 = half raw dot size
        double rawScreenY = Center - rawY * Radius - 5;
        RawTransform.X = rawScreenX;
        RawTransform.Y = rawScreenY;

        double procScreenX = Center + procX * Radius - 7;   // -7 = half processed dot size
        double procScreenY = Center - procY * Radius - 7;
        ProcessedTransform.X = procScreenX;
        ProcessedTransform.Y = procScreenY;

        // Deadzone rings: diameter scales with deadzone fraction.
        InnerDeadzoneRing.Width = InnerDeadzoneRing.Height = Math.Max(2, innerDeadzone * 200);
        OuterDeadzoneRing.Width = OuterDeadzoneRing.Height = Math.Max(2, outerDeadzone * 200);
    }
}
