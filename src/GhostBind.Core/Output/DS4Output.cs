using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.DualShock4;
using GhostBind.Core.Mapping;

namespace GhostBind.Core.Output;

public sealed class DS4Output : IVirtualPad
{
    private readonly ViGEmClient _client;
    private readonly IDualShock4Controller _controller;
    private byte _lastLargeMotor;
    private byte _lastSmallMotor;

    // DS4's d-pad is a single direction enum (8-way + None). We track the four
    // direction bits internally and recompute the combined direction every frame.
    private bool _dpadUp, _dpadDown, _dpadLeft, _dpadRight;

    public DS4Output()
    {
        _client = new ViGEmClient();
        _controller = _client.CreateDualShock4Controller();
        _controller.FeedbackReceived += OnFeedback;
        _controller.Connect();
    }

    public byte LargeMotor => _lastLargeMotor;
    public byte SmallMotor => _lastSmallMotor;

    private static readonly DualShock4Button[] Resettable =
    {
        DualShock4Button.Cross, DualShock4Button.Circle, DualShock4Button.Square, DualShock4Button.Triangle,
        DualShock4Button.ShoulderLeft, DualShock4Button.ShoulderRight,
        DualShock4Button.ThumbLeft, DualShock4Button.ThumbRight,
        DualShock4Button.Share, DualShock4Button.Options,
    };

    public void Reset()
    {
        foreach (var b in Resettable) _controller.SetButtonState(b, false);
        _controller.SetButtonState(DualShock4SpecialButton.Ps, false);
        _dpadUp = _dpadDown = _dpadLeft = _dpadRight = false;
        _controller.SetDPadDirection(DualShock4DPadDirection.None);
    }

    public void SetButton(OutputButton button, bool pressed)
    {
        // D-pad lives in a separate enum on DS4 — fold the four direction bits
        // into a single combined direction.
        switch (button)
        {
            case OutputButton.DPadUp:    _dpadUp = pressed; RecomputeDPad(); return;
            case OutputButton.DPadDown:  _dpadDown = pressed; RecomputeDPad(); return;
            case OutputButton.DPadLeft:  _dpadLeft = pressed; RecomputeDPad(); return;
            case OutputButton.DPadRight: _dpadRight = pressed; RecomputeDPad(); return;
            case OutputButton.Guide:
                // PS button is in the "special" enum on ViGEm DS4.
                _controller.SetButtonState(DualShock4SpecialButton.Ps, pressed);
                return;
        }

        var ds4 = ToDS4(button);
        if (ds4 != null) _controller.SetButtonState(ds4, pressed);
    }

    public void SetThumb(bool right, short x, short y)
    {
        // DS4 sticks are unsigned 0..255 with 128 = center, opposite of X360.
        byte bx = (byte)Math.Clamp((x + 32768) >> 8, 0, 255);
        byte by = (byte)Math.Clamp(((-y) + 32768) >> 8, 0, 255); // also Y is flipped on DS4

        if (right)
        {
            _controller.SetAxisValue(DualShock4Axis.RightThumbX, bx);
            _controller.SetAxisValue(DualShock4Axis.RightThumbY, by);
        }
        else
        {
            _controller.SetAxisValue(DualShock4Axis.LeftThumbX, bx);
            _controller.SetAxisValue(DualShock4Axis.LeftThumbY, by);
        }
    }

    public void SetTrigger(bool right, byte value)
    {
        _controller.SetSliderValue(right ? DualShock4Slider.RightTrigger : DualShock4Slider.LeftTrigger, value);
    }

    public void Submit() => _controller.SubmitReport();

    private void OnFeedback(object? sender, DualShock4FeedbackReceivedEventArgs e)
    {
        _lastLargeMotor = e.LargeMotor;
        _lastSmallMotor = e.SmallMotor;
    }

    private void RecomputeDPad()
    {
        var dir = (_dpadUp, _dpadRight, _dpadDown, _dpadLeft) switch
        {
            (true, false, false, false)  => DualShock4DPadDirection.North,
            (true, true, false, false)   => DualShock4DPadDirection.Northeast,
            (false, true, false, false)  => DualShock4DPadDirection.East,
            (false, true, true, false)   => DualShock4DPadDirection.Southeast,
            (false, false, true, false)  => DualShock4DPadDirection.South,
            (false, false, true, true)   => DualShock4DPadDirection.Southwest,
            (false, false, false, true)  => DualShock4DPadDirection.West,
            (true, false, false, true)   => DualShock4DPadDirection.Northwest,
            _ => DualShock4DPadDirection.None,
        };
        _controller.SetDPadDirection(dir);
    }

    private static DualShock4Button? ToDS4(OutputButton b) => b switch
    {
        OutputButton.A => DualShock4Button.Cross,
        OutputButton.B => DualShock4Button.Circle,
        OutputButton.X => DualShock4Button.Square,
        OutputButton.Y => DualShock4Button.Triangle,
        OutputButton.LeftBumper => DualShock4Button.ShoulderLeft,
        OutputButton.RightBumper => DualShock4Button.ShoulderRight,
        OutputButton.LeftStickClick => DualShock4Button.ThumbLeft,
        OutputButton.RightStickClick => DualShock4Button.ThumbRight,
        OutputButton.Back => DualShock4Button.Share,
        OutputButton.Start => DualShock4Button.Options,
        // Guide handled separately in SetButton (lives in DualShock4SpecialButton).
        _ => null,
    };

    public void Dispose()
    {
        _controller.FeedbackReceived -= OnFeedback;
        _controller.Disconnect();
        _client.Dispose();
    }
}
