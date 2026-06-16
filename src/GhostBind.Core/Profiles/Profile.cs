using GhostBind.Core.Input;
using GhostBind.Core.Mapping;

namespace GhostBind.Core.Profiles;

public enum OutputControllerType
{
    Xbox360,
    DualShock4,
}

public class Profile
{
    public string Name { get; set; } = "Default";
    public OutputControllerType OutputType { get; set; } = OutputControllerType.Xbox360;
    public StickConfig LeftStick { get; set; } = new();
    public StickConfig RightStick { get; set; } = new();
    public TriggerConfig LeftTrigger { get; set; } = new();
    public TriggerConfig RightTrigger { get; set; } = new();
    public ButtonMap ButtonMap { get; set; } = ButtonMap.Default();
    public TouchpadConfig Touchpad { get; set; } = new();
    public LightbarConfig Lightbar { get; set; } = new();

    public static Profile Default() => new() { Name = "Default" };
}

public class TouchpadConfig
{
    public bool AsMouse { get; set; }
    public double Sensitivity { get; set; } = 1.5;
    public bool InvertX { get; set; }
    public bool InvertY { get; set; }
}

public class LightbarConfig
{
    public bool Enabled { get; set; } = true;
    public byte Red { get; set; } = 0;
    public byte Green { get; set; } = 50;
    public byte Blue { get; set; } = 200;
}

public class StickConfig
{
    public double InnerDeadzone { get; set; } = 0.08;
    public double OuterDeadzone { get; set; } = 1.0;
    public double AntiDeadzone { get; set; } = 0.0;
    public double Sensitivity { get; set; } = 1.0;
    public CurveType Curve { get; set; } = CurveType.Linear;
    public double CurveExponent { get; set; } = 2.0;
    public bool InvertX { get; set; }
    public bool InvertY { get; set; }

    // Filters the brief opposite-direction signal that worn sticks emit when
    // released quickly. False by default — only flip on if you observe phantom inputs.
    public bool AntiSnapback { get; set; } = false;

    // Anchor points for CurveType.Custom. Endpoints fixed at (0,0) and (1,1);
    // the user drags the three middle points on the curve editor.
    public List<CurvePoint> CustomPoints { get; set; } = new()
    {
        new CurvePoint { Input = 0.00, Output = 0.00 },
        new CurvePoint { Input = 0.25, Output = 0.15 },
        new CurvePoint { Input = 0.50, Output = 0.50 },
        new CurvePoint { Input = 0.75, Output = 0.85 },
        new CurvePoint { Input = 1.00, Output = 1.00 },
    };
}

public class TriggerConfig
{
    public double Deadzone { get; set; }
    public double AntiDeadzone { get; set; } = 0.0;
    public double Saturation { get; set; } = 1.0;
    public CurveType Curve { get; set; } = CurveType.Linear;
    public double CurveExponent { get; set; } = 2.0;
    public double DigitalThreshold { get; set; } = 0.5;

    // Adaptive trigger feedback (PS5-only — physical resistance/click felt in the
    // trigger). Doesn't change what the game sees; purely tactile.
    public TriggerEffectMode EffectMode { get; set; } = TriggerEffectMode.Off;
    public byte EffectStart { get; set; } = 2;     // 0..9 — where the effect begins
    public byte EffectEnd { get; set; } = 7;       // 0..9 — Section / Weapon end position
    public byte EffectForce { get; set; } = 200;   // 0..255 — strength of resistance
}

public class ButtonMap
{
    public Dictionary<DualSenseButton, OutputButton> Mappings { get; set; } = new();

    // Optional advanced binding per source button. If a source has an entry here,
    // the engine evaluates the activator (Tap / Hold / DoubleTap) and the simple
    // Mappings entry is ignored for that source. Old profiles without this field
    // continue to work unchanged.
    public Dictionary<DualSenseButton, ButtonActivator> Activators { get; set; } = new();

    // Layer 2: hold ShiftButton to swap from Mappings → Layer2Mappings. None = disabled.
    public DualSenseButton ShiftButton { get; set; } = DualSenseButton.None;
    public Dictionary<DualSenseButton, OutputButton> Layer2Mappings { get; set; } = new();

    public static ButtonMap Default() => new()
    {
        Mappings = new Dictionary<DualSenseButton, OutputButton>
        {
            [DualSenseButton.Cross] = OutputButton.A,
            [DualSenseButton.Circle] = OutputButton.B,
            [DualSenseButton.Square] = OutputButton.X,
            [DualSenseButton.Triangle] = OutputButton.Y,
            [DualSenseButton.L1] = OutputButton.LeftBumper,
            [DualSenseButton.R1] = OutputButton.RightBumper,
            [DualSenseButton.L3] = OutputButton.LeftStickClick,
            [DualSenseButton.R3] = OutputButton.RightStickClick,
            [DualSenseButton.Create] = OutputButton.Back,
            [DualSenseButton.Options] = OutputButton.Start,
            [DualSenseButton.Ps] = OutputButton.Guide,
            [DualSenseButton.DPadUp] = OutputButton.DPadUp,
            [DualSenseButton.DPadDown] = OutputButton.DPadDown,
            [DualSenseButton.DPadLeft] = OutputButton.DPadLeft,
            [DualSenseButton.DPadRight] = OutputButton.DPadRight,
        },
    };
}
