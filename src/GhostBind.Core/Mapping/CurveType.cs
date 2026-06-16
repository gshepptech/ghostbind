namespace GhostBind.Core.Mapping;

public enum CurveType
{
    Linear,
    Smooth,        // smoothstep — gentler ramp around center, accelerates near max
    Aggressive,    // x^(1/exp) — quick response off center
    Precision,     // x^exp — slow/fine near center, max at edges
    AntiDeadzone,  // boosts the low end past a fixed offset (good for games with built-in deadzones)
    Custom,        // shape defined by anchor points the user drags on the curve editor
}
