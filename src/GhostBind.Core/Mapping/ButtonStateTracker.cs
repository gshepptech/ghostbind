using GhostBind.Core.Input;

namespace GhostBind.Core.Mapping;

// Per-source-button state machine. Drives activator evaluation across input frames.
// Lives in ControllerService so the timing context survives between polls.
public sealed class ButtonStateTracker
{
    private struct State
    {
        public bool IsPressed;
        public DateTime PressStart;
        public DateTime LastReleaseAt;
        public int RecentReleaseCount;
        public DateTime PulseUntil;
    }

    private readonly Dictionary<DualSenseButton, State> _states = new();

    public bool Evaluate(DualSenseButton source, bool currentlyPressed, ButtonActivator activator, DateTime now)
    {
        _states.TryGetValue(source, out var s);

        bool wasPressed = s.IsPressed;
        bool justPressed = currentlyPressed && !wasPressed;
        bool justReleased = !currentlyPressed && wasPressed;

        if (justPressed)
        {
            s.PressStart = now;
        }

        if (justReleased)
        {
            double heldMs = (now - s.PressStart).TotalMilliseconds;

            // Only short presses count toward Tap / DoubleTap.
            if (heldMs <= activator.TapMaxMs)
            {
                // Decay stale release count first.
                if ((now - s.LastReleaseAt).TotalMilliseconds > activator.DoubleTapWindowMs)
                    s.RecentReleaseCount = 0;

                s.RecentReleaseCount++;
                s.LastReleaseAt = now;

                if (activator.Mode == ActivatorMode.Tap)
                {
                    s.PulseUntil = now.AddMilliseconds(activator.OutputPulseMs);
                }
                else if (activator.Mode == ActivatorMode.DoubleTap && s.RecentReleaseCount >= 2)
                {
                    s.PulseUntil = now.AddMilliseconds(activator.OutputPulseMs);
                    s.RecentReleaseCount = 0;
                }
            }
            else
            {
                // Long press resets the tap-counting window.
                s.RecentReleaseCount = 0;
            }
        }

        s.IsPressed = currentlyPressed;
        _states[source] = s;

        return activator.Mode switch
        {
            ActivatorMode.Regular => currentlyPressed,
            ActivatorMode.Hold => currentlyPressed && (now - s.PressStart).TotalMilliseconds >= activator.HoldThresholdMs,
            ActivatorMode.Tap => now < s.PulseUntil,
            ActivatorMode.DoubleTap => now < s.PulseUntil,
            _ => false,
        };
    }

    public void Reset() => _states.Clear();
}
