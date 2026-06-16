using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using GhostBind.Core.Mapping;

namespace GhostBind.Core.Output;

public sealed class X360Output : IVirtualPad
{
    private readonly ViGEmClient _client;
    private readonly IXbox360Controller _controller;
    private byte _lastLargeMotor;
    private byte _lastSmallMotor;

    private static readonly Xbox360Button[] Resettable =
    {
        Xbox360Button.A, Xbox360Button.B, Xbox360Button.X, Xbox360Button.Y,
        Xbox360Button.LeftShoulder, Xbox360Button.RightShoulder,
        Xbox360Button.LeftThumb, Xbox360Button.RightThumb,
        Xbox360Button.Back, Xbox360Button.Start, Xbox360Button.Guide,
        Xbox360Button.Up, Xbox360Button.Down, Xbox360Button.Left, Xbox360Button.Right,
    };

    public X360Output()
    {
        _client = new ViGEmClient();
        _controller = _client.CreateXbox360Controller();
        _controller.FeedbackReceived += OnFeedback;
        _controller.Connect();
    }

    public IXbox360Controller Controller => _controller;

    public byte LargeMotor => _lastLargeMotor;
    public byte SmallMotor => _lastSmallMotor;

    public void Reset()
    {
        foreach (var b in Resettable) _controller.SetButtonState(b, false);
    }

    public void SetButton(OutputButton button, bool pressed)
    {
        var x = ToX360(button);
        if (x != null) _controller.SetButtonState(x, pressed);
    }

    public void SetThumb(bool right, short x, short y)
    {
        if (right)
        {
            _controller.SetAxisValue(Xbox360Axis.RightThumbX, x);
            _controller.SetAxisValue(Xbox360Axis.RightThumbY, y);
        }
        else
        {
            _controller.SetAxisValue(Xbox360Axis.LeftThumbX, x);
            _controller.SetAxisValue(Xbox360Axis.LeftThumbY, y);
        }
    }

    public void SetTrigger(bool right, byte value)
    {
        _controller.SetSliderValue(right ? Xbox360Slider.RightTrigger : Xbox360Slider.LeftTrigger, value);
    }

    public void Submit() => _controller.SubmitReport();

    private void OnFeedback(object? sender, Xbox360FeedbackReceivedEventArgs e)
    {
        _lastLargeMotor = e.LargeMotor;
        _lastSmallMotor = e.SmallMotor;
    }

    private static Xbox360Button? ToX360(OutputButton b) => b switch
    {
        OutputButton.A => Xbox360Button.A,
        OutputButton.B => Xbox360Button.B,
        OutputButton.X => Xbox360Button.X,
        OutputButton.Y => Xbox360Button.Y,
        OutputButton.LeftBumper => Xbox360Button.LeftShoulder,
        OutputButton.RightBumper => Xbox360Button.RightShoulder,
        OutputButton.LeftStickClick => Xbox360Button.LeftThumb,
        OutputButton.RightStickClick => Xbox360Button.RightThumb,
        OutputButton.Back => Xbox360Button.Back,
        OutputButton.Start => Xbox360Button.Start,
        OutputButton.Guide => Xbox360Button.Guide,
        OutputButton.DPadUp => Xbox360Button.Up,
        OutputButton.DPadDown => Xbox360Button.Down,
        OutputButton.DPadLeft => Xbox360Button.Left,
        OutputButton.DPadRight => Xbox360Button.Right,
        _ => null,
    };

    public void Dispose()
    {
        _controller.FeedbackReceived -= OnFeedback;
        _controller.Disconnect();
        _client.Dispose();
    }
}
