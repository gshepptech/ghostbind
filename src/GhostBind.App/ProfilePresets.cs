using GhostBind.Core.Mapping;
using GhostBind.Core.Profiles;

namespace GhostBind.App;

// Game-tuned starting points the user can apply to their active profile in one click.
// These are opinionated defaults — the user is expected to tweak from here.
public static class ProfilePresets
{
    public sealed record Preset(string Name, string Tagline, string Notes, Action<Profile> Apply);

    public static IReadOnlyList<Preset> All { get; } = new Preset[]
    {
        new(
            Name: "Fortnite — Fine Aim + Fast Turn (Right stick only)",
            Tagline: "Custom curve on RIGHT stick: gentle precision floor, smooth progressive climb to fast end. Left stick (movement) untouched.",
            Notes: "Right stick custom shape — bottom 25% stays slow for fine aim and Edit-mode precision; mid-range climbs progressively without an abrupt 'switch flip'; top 25% covers ~41% of the output range for instant 180s and build flicks. Anti-deadzone 0.13 to push past Fortnite's in-game deadzone. Hair trigger on R2 (0.65) for shotguns; ADS on L2 at 0.85. Left stick is left alone — movement doesn't need a curve. Pair with Fortnite in-game look-stick deadzone = 0%.",
            Apply: p =>
            {
                // RIGHT stick (camera/aim) — the curve goes here.
                var s = p.RightStick;
                s.InnerDeadzone = 0.05;
                s.OuterDeadzone = 1.0;
                s.AntiDeadzone = 0.13;
                s.Sensitivity = 1.0;
                s.Curve = CurveType.Custom;
                s.CurveExponent = 2.0;
                s.CustomPoints = new List<CurvePoint>
                {
                    new() { Input = 0.00, Output = 0.00 },
                    new() { Input = 0.25, Output = 0.05 },
                    new() { Input = 0.50, Output = 0.26 },
                    new() { Input = 0.75, Output = 0.59 },
                    new() { Input = 1.00, Output = 1.00 },
                };
                s.AntiSnapback = false;
                s.InvertX = false;
                s.InvertY = false;

                // LEFT stick (movement) — intentionally NOT modified.

                // Triggers — hair trigger right (fire) + slightly less hair left (ADS).
                p.LeftTrigger.Deadzone = 0;
                p.LeftTrigger.AntiDeadzone = 0;
                p.LeftTrigger.Saturation = 0.85;
                p.LeftTrigger.Curve = CurveType.Linear;

                p.RightTrigger.Deadzone = 0;
                p.RightTrigger.AntiDeadzone = 0;
                p.RightTrigger.Saturation = 0.65;
                p.RightTrigger.Curve = CurveType.Linear;
            }
        ),
    };
}
