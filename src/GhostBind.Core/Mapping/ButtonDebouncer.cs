using GhostBind.Core.Input;

namespace GhostBind.Core.Mapping;

// Settling-time debouncer. A state change is only accepted once the new state has
// held *continuously* for the full debounce window. If the input flickers back to
// the old state mid-window, the pending change is discarded and the stable state
// is preserved.
//
// This is stricter than first-edge-wins. It costs `debounceMs` of latency on every
// press and release, but guarantees zero phantom transitions caused by chatter
// shorter than the window.
public sealed class ButtonDebouncer
{
    private struct State
    {
        public bool Stable;            // last accepted state
        public bool PendingDifferent;  // is there a contradicting state pending acceptance?
        public DateTime PendingSince;  // when the contradicting state was first observed
    }

    private static readonly DualSenseButton[] AllButtons =
        Enum.GetValues<DualSenseButton>().Where(v => v != DualSenseButton.None).ToArray();

    private readonly Dictionary<DualSenseButton, State> _state = new();

    public DualSenseButton Filter(DualSenseButton current, int debounceMs, DateTime now)
    {
        if (debounceMs <= 0) return current; // pass-through fast path

        var result = DualSenseButton.None;
        foreach (var b in AllButtons)
        {
            bool currentlyPressed = current.HasFlag(b);
            _state.TryGetValue(b, out var s);

            if (currentlyPressed == s.Stable)
            {
                // Input matches our stable state — clear any pending contradiction.
                s.PendingDifferent = false;
            }
            else
            {
                // Input contradicts our stable state.
                if (!s.PendingDifferent)
                {
                    // First time we see this contradicting state — start the timer.
                    s.PendingDifferent = true;
                    s.PendingSince = now;
                }
                else if ((now - s.PendingSince).TotalMilliseconds >= debounceMs)
                {
                    // Contradicting state has held for the full window. Accept.
                    s.Stable = currentlyPressed;
                    s.PendingDifferent = false;
                }
                // Otherwise: hold stable state, keep waiting for either settle or revert.
            }

            _state[b] = s;
            if (s.Stable) result |= b;
        }
        return result;
    }

    public void Reset() => _state.Clear();
}
