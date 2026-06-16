namespace GhostBind.Core.Mapping;

public static class ResponseCurve
{
    public static double Apply(double normalized, CurveType type, double exponent = 2.0,
                               IReadOnlyList<CurvePoint>? customPoints = null)
    {
        double abs = Math.Abs(normalized);
        double sign = Math.Sign(normalized);

        double curved = type switch
        {
            CurveType.Linear => abs,
            CurveType.Smooth => abs * abs * (3 - 2 * abs),
            CurveType.Aggressive => Math.Pow(abs, 1.0 / Math.Max(0.1, exponent)),
            CurveType.Precision => Math.Pow(abs, Math.Max(0.1, exponent)),
            CurveType.AntiDeadzone => abs <= 0.0001 ? 0 : 0.15 + abs * 0.85,
            CurveType.Custom when customPoints is { Count: >= 2 } => InterpolateCustom(abs, customPoints),
            _ => abs,
        };

        return sign * curved;
    }

    private static double InterpolateCustom(double input, IReadOnlyList<CurvePoint> points)
    {
        // Points are stored sorted by Input. Walk to the first segment that contains
        // the query and linearly interpolate. Endpoints clamp.
        if (input <= points[0].Input) return points[0].Output;
        if (input >= points[^1].Input) return points[^1].Output;

        for (int i = 1; i < points.Count; i++)
        {
            if (input <= points[i].Input)
            {
                var p0 = points[i - 1];
                var p1 = points[i];
                double span = p1.Input - p0.Input;
                if (span <= 0.0001) return p1.Output;
                double t = (input - p0.Input) / span;
                return p0.Output + t * (p1.Output - p0.Output);
            }
        }
        return input;
    }
}
