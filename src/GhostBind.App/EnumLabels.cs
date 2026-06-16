using GhostBind.Core.Input;
using GhostBind.Core.Mapping;

namespace GhostBind.App;

// One-stop spot for human-readable labels. Dropdowns bind to enum values directly
// (so persistence stays clean), and a value converter consults this table at render time.
public static class EnumLabels
{
    public static string DisplayLabel(this OutputButton b) => b switch
    {
        OutputButton.None             => "(disabled)",
        OutputButton.A                => "A  ·  Cross ✕",
        OutputButton.B                => "B  ·  Circle ◯",
        OutputButton.X                => "X  ·  Square ☐",
        OutputButton.Y                => "Y  ·  Triangle △",
        OutputButton.LeftBumper       => "Left Bumper  ·  L1",
        OutputButton.RightBumper      => "Right Bumper  ·  R1",
        OutputButton.Back             => "Back  ·  Select / View / Share",
        OutputButton.Start            => "Start  ·  Menu / Options",
        OutputButton.Guide            => "Guide  ·  Xbox / PS button",
        OutputButton.LeftStickClick   => "Left Stick Click  ·  L3",
        OutputButton.RightStickClick  => "Right Stick Click  ·  R3",
        OutputButton.DPadUp           => "D-Pad ↑",
        OutputButton.DPadDown         => "D-Pad ↓",
        OutputButton.DPadLeft         => "D-Pad ←",
        OutputButton.DPadRight        => "D-Pad →",
        _ => b.ToString(),
    };

    public static string DisplayLabel(this DualSenseButton b) => b switch
    {
        DualSenseButton.None           => "(none)",
        DualSenseButton.Cross          => "Cross ✕",
        DualSenseButton.Circle         => "Circle ◯",
        DualSenseButton.Square         => "Square ☐",
        DualSenseButton.Triangle       => "Triangle △",
        DualSenseButton.L1             => "L1",
        DualSenseButton.R1             => "R1",
        DualSenseButton.L2             => "L2 (click bit)",
        DualSenseButton.R2             => "R2 (click bit)",
        DualSenseButton.L3             => "L3  (left stick click)",
        DualSenseButton.R3             => "R3  (right stick click)",
        DualSenseButton.Create         => "Create  ·  Share",
        DualSenseButton.Options        => "Options",
        DualSenseButton.Ps             => "PS Button",
        DualSenseButton.TouchpadClick  => "Touchpad Click",
        DualSenseButton.Mute           => "Mute",
        DualSenseButton.DPadUp         => "D-Pad ↑",
        DualSenseButton.DPadDown       => "D-Pad ↓",
        DualSenseButton.DPadLeft       => "D-Pad ←",
        DualSenseButton.DPadRight      => "D-Pad →",
        _ => b.ToString(),
    };

    public static string DisplayLabel(this ActivatorMode m) => m switch
    {
        ActivatorMode.Regular   => "Regular  ·  fires while held",
        ActivatorMode.Tap       => "Tap  ·  quick press",
        ActivatorMode.Hold      => "Hold  ·  held past threshold",
        ActivatorMode.DoubleTap => "Double-Tap  ·  two quick taps",
        _ => m.ToString(),
    };

    public static string DisplayLabel(this CurveType c) => c switch
    {
        CurveType.Linear        => "Linear  ·  no shaping",
        CurveType.Smooth        => "Smooth  ·  smoothstep",
        CurveType.Aggressive    => "Aggressive  ·  fast off-center",
        CurveType.Precision     => "Precision  ·  fine near center",
        CurveType.AntiDeadzone  => "AntiDeadzone  ·  boost low end",
        CurveType.Custom        => "Custom  ·  drag your own",
        _ => c.ToString(),
    };
}
