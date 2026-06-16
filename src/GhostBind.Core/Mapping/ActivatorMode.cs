namespace GhostBind.Core.Mapping;

public enum ActivatorMode
{
    // Output is held while source is held. The default — same as a basic remap.
    Regular,

    // Output fires for OutputPulseMs after a quick press-and-release (under TapMaxMs).
    // Good for single-action triggers — "tap circle = roll."
    Tap,

    // Output fires while source has been held longer than HoldThresholdMs.
    // Good for "hold to sprint" / "hold to crouch" style binds without conflicting with tap actions.
    Hold,

    // Output fires for OutputPulseMs after two quick taps within DoubleTapWindowMs.
    // Good for combo triggers — "double-tap dodge."
    DoubleTap,
}
