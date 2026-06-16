namespace GhostBind.Core.Mapping;

public static class Deadzone
{
    // Radial deadzone: zero output inside `inner` magnitude, saturate outside `outer`,
    // and rescale the surviving range to [0..1] so the stick still reaches max output.
    public static (double X, double Y) ApplyRadial(double x, double y, double inner, double outer)
    {
        double mag = Math.Sqrt(x * x + y * y);
        if (mag < inner || mag <= 0.0001) return (0, 0);

        double clipped = Math.Min(mag, outer);
        double range = Math.Max(0.0001, outer - inner);
        double rescaled = (clipped - inner) / range;
        double scale = rescaled / mag;
        return (x * scale, y * scale);
    }

    public static double ApplyAxial(double v, double inner, double outer)
    {
        double abs = Math.Abs(v);
        if (abs < inner) return 0;

        double clipped = Math.Min(abs, outer);
        double range = Math.Max(0.0001, outer - inner);
        double rescaled = (clipped - inner) / range;
        return Math.Sign(v) * rescaled;
    }
}
