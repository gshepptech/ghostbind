using GhostBind.Core.Input;
using GhostBind.Core.Output;
using GhostBind.Core.Profiles;

namespace GhostBind.Core.Mapping;

public sealed class MappingEngine
{
    private static readonly DualSenseButton[] AllSourceButtons =
        Enum.GetValues<DualSenseButton>().Where(v => v != DualSenseButton.None).ToArray();

    private readonly ButtonStateTracker _tracker = new();
    private readonly ButtonDebouncer _debouncer = new();
    private (double X, double Y) _leftStickHistory;
    private (double X, double Y) _rightStickHistory;

    // ms window during which a button state change is ignored after the previous
    // change. 0 = pass-through. Set by ControllerService from the app-level slider.
    // Covers microswitch chatter on DualSense bumpers (LB/RB) that some games
    // interpret as multiple separate presses.
    public int ButtonDebounceMs { get; set; } = 0;

    public ProcessedSnapshot ProcessAndApply(in DualSenseState raw, Profile profile, IVirtualPad output)
    {
        var snap = new ProcessedSnapshot { Raw = raw };

        // Apply debouncing on a working copy so the snapshot still shows the truly-raw
        // state (Dashboard / Diagnostics see the unfiltered button state).
        var filteredRaw = raw;
        filteredRaw.Buttons = _debouncer.Filter(raw.Buttons, ButtonDebounceMs, DateTime.UtcNow);

        // Sticks: normalize to [-1,1], invert Y so up = positive (Xbox convention).
        snap.LeftRawX = NormalizeStick(raw.LeftStickX);
        snap.LeftRawY = -NormalizeStick(raw.LeftStickY);
        (snap.LeftX, snap.LeftY) = ProcessStick(snap.LeftRawX, snap.LeftRawY, profile.LeftStick, ref _leftStickHistory);

        snap.RightRawX = NormalizeStick(raw.RightStickX);
        snap.RightRawY = -NormalizeStick(raw.RightStickY);
        (snap.RightX, snap.RightY) = ProcessStick(snap.RightRawX, snap.RightRawY, profile.RightStick, ref _rightStickHistory);

        output.SetThumb(right: false, ToShort(snap.LeftX), ToShort(snap.LeftY));
        output.SetThumb(right: true, ToShort(snap.RightX), ToShort(snap.RightY));

        // Triggers: 0..1 normalized, deadzone + saturation + curve.
        snap.LeftTriggerRaw = raw.LeftTrigger / 255.0;
        snap.RightTriggerRaw = raw.RightTrigger / 255.0;
        snap.LeftTrigger = ProcessTrigger(snap.LeftTriggerRaw, profile.LeftTrigger);
        snap.RightTrigger = ProcessTrigger(snap.RightTriggerRaw, profile.RightTrigger);

        output.SetTrigger(right: false, ToByte(snap.LeftTrigger));
        output.SetTrigger(right: true, ToByte(snap.RightTrigger));

        ApplyButtons(filteredRaw, profile.ButtonMap, output);

        return snap;
    }

    public void ResetActivatorState() => _tracker.Reset();

    private static double NormalizeStick(byte raw) => (raw - 128) / 127.0;

    private static (double X, double Y) ProcessStick(double x, double y, StickConfig cfg, ref (double X, double Y) history)
    {
        if (cfg.InvertX) x = -x;
        if (cfg.InvertY) y = -y;

        var (dx, dy) = Deadzone.ApplyRadial(x, y, cfg.InnerDeadzone, cfg.OuterDeadzone);

        double mag = Math.Sqrt(dx * dx + dy * dy);

        double outX, outY;
        if (mag <= 0.0001)
        {
            outX = 0; outY = 0;
        }
        else
        {
            double curvedMag = ResponseCurve.Apply(mag, cfg.Curve, cfg.CurveExponent, cfg.CustomPoints) * cfg.Sensitivity;

            // Anti-deadzone: lift any non-zero output above the floor so games with
            // their own internal deadzone (most shooters) don't swallow small inputs.
            if (curvedMag > 0.0001 && cfg.AntiDeadzone > 0)
                curvedMag = cfg.AntiDeadzone + curvedMag * (1 - cfg.AntiDeadzone);

            curvedMag = Math.Clamp(curvedMag, 0, 1);
            double scale = curvedMag / mag;
            outX = dx * scale;
            outY = dy * scale;
        }

        if (cfg.AntiSnapback)
        {
            // Snapback heuristic: previous frame had high magnitude on one side, current
            // frame has low magnitude on the OPPOSITE side. That's the phantom blip — null it.
            const double prevMagFloor = 0.5;
            const double currMagCeil = 0.4;

            if (Math.Sign(outX) != 0 && Math.Sign(outX) != Math.Sign(history.X)
                && Math.Abs(history.X) > prevMagFloor && Math.Abs(outX) < currMagCeil)
                outX = 0;
            if (Math.Sign(outY) != 0 && Math.Sign(outY) != Math.Sign(history.Y)
                && Math.Abs(history.Y) > prevMagFloor && Math.Abs(outY) < currMagCeil)
                outY = 0;
        }

        history = (outX, outY);
        return (outX, outY);
    }

    private static double ProcessTrigger(double v, TriggerConfig cfg)
    {
        var d = Deadzone.ApplyAxial(v, cfg.Deadzone, cfg.Saturation);
        var curved = ResponseCurve.Apply(d, cfg.Curve, cfg.CurveExponent);
        if (curved > 0.0001 && cfg.AntiDeadzone > 0)
            curved = cfg.AntiDeadzone + curved * (1 - cfg.AntiDeadzone);
        return Math.Clamp(curved, 0, 1);
    }

    private static short ToShort(double v) =>
        (short)Math.Clamp(v * 32767, short.MinValue, (int)short.MaxValue);

    private static byte ToByte(double v) =>
        (byte)Math.Clamp(v * 255, 0, 255);

    private void ApplyButtons(in DualSenseState raw, ButtonMap map, IVirtualPad output)
    {
        // Reset every remappable output each frame so an unmapped button reads as released.
        output.Reset();

        var now = DateTime.UtcNow;

        bool layer2Active = map.ShiftButton != DualSenseButton.None
                            && raw.Buttons.HasFlag(map.ShiftButton);
        var activeMappings = layer2Active ? map.Layer2Mappings : map.Mappings;

        foreach (var source in AllSourceButtons)
        {
            // Shift button itself never produces an output — it's only a modifier.
            if (source == map.ShiftButton) continue;

            bool currentlyPressed = raw.Buttons.HasFlag(source);

            if (map.Activators.TryGetValue(source, out var activator) && activator.Mode != ActivatorMode.Regular)
            {
                // Activators evaluate against layer 1 only — mixing tap/hold timing with
                // shift-modifier semantics gets confusing fast. v2 territory.
                bool fire = _tracker.Evaluate(source, currentlyPressed, activator, now);
                if (fire) output.SetButton(activator.Output, true);
                continue;
            }

            if (currentlyPressed && activeMappings.TryGetValue(source, out var simple))
            {
                output.SetButton(simple, true);
            }
        }
    }
}
