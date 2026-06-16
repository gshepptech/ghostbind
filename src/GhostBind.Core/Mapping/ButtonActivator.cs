namespace GhostBind.Core.Mapping;

public class ButtonActivator
{
    public ActivatorMode Mode { get; set; } = ActivatorMode.Regular;
    public OutputButton Output { get; set; }

    // Hold mode: output asserts only after this many ms of continuous press.
    public int HoldThresholdMs { get; set; } = 250;

    // Tap modes: a press shorter than this counts as a tap.
    public int TapMaxMs { get; set; } = 200;

    // DoubleTap mode: two taps must occur within this window.
    public int DoubleTapWindowMs { get; set; } = 350;

    // Tap / DoubleTap: output is asserted for this duration after the trigger fires,
    // since the source is already released by definition.
    public int OutputPulseMs { get; set; } = 60;
}
