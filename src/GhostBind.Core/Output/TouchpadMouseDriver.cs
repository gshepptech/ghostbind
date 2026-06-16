using GhostBind.Core.Input;
using GhostBind.Core.Profiles;

namespace GhostBind.Core.Output;

// Tracks finger position frame-to-frame and emits relative mouse deltas. Two-finger
// scroll is left for v1.x — for now finger 1 = mouse cursor when touchpad-as-mouse
// is enabled in the profile.
public sealed class TouchpadMouseDriver
{
    private bool _wasActive;
    private int _lastX;
    private int _lastY;

    public void Tick(in DualSenseState state, TouchpadConfig cfg)
    {
        if (!cfg.AsMouse)
        {
            _wasActive = false;
            return;
        }

        var finger = state.Finger1;
        if (!finger.Active)
        {
            _wasActive = false;
            return;
        }

        if (!_wasActive)
        {
            // Just touched — start tracking, no delta this frame to avoid a jump.
            _lastX = finger.X;
            _lastY = finger.Y;
            _wasActive = true;
            return;
        }

        int dx = finger.X - _lastX;
        int dy = finger.Y - _lastY;
        _lastX = finger.X;
        _lastY = finger.Y;

        if (cfg.InvertX) dx = -dx;
        if (cfg.InvertY) dy = -dy;

        double sx = dx * cfg.Sensitivity;
        double sy = dy * cfg.Sensitivity;

        MouseInjector.MoveRelative((int)Math.Round(sx), (int)Math.Round(sy));
    }
}
