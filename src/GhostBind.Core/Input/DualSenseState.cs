namespace GhostBind.Core.Input;

public struct DualSenseState
{
    public byte LeftStickX;
    public byte LeftStickY;
    public byte RightStickX;
    public byte RightStickY;
    public byte LeftTrigger;
    public byte RightTrigger;
    public DPadDirection DPad;
    public DualSenseButton Buttons;
    public TouchpadFinger Finger1;
    public TouchpadFinger Finger2;
    public byte BatteryPercent;   // 0..100
    public bool IsCharging;

    public static DualSenseState Parse(ReadOnlySpan<byte> report)
    {
        byte b1 = report[DualSenseProtocol.OffsetButtons1];
        byte b2 = report[DualSenseProtocol.OffsetButtons2];
        byte b3 = report[DualSenseProtocol.OffsetButtons3];

        var buttons = DualSenseButton.None;
        if ((b1 & 0x10) != 0) buttons |= DualSenseButton.Square;
        if ((b1 & 0x20) != 0) buttons |= DualSenseButton.Cross;
        if ((b1 & 0x40) != 0) buttons |= DualSenseButton.Circle;
        if ((b1 & 0x80) != 0) buttons |= DualSenseButton.Triangle;
        if ((b2 & 0x01) != 0) buttons |= DualSenseButton.L1;
        if ((b2 & 0x02) != 0) buttons |= DualSenseButton.R1;
        if ((b2 & 0x04) != 0) buttons |= DualSenseButton.L2;
        if ((b2 & 0x08) != 0) buttons |= DualSenseButton.R2;
        if ((b2 & 0x10) != 0) buttons |= DualSenseButton.Create;
        if ((b2 & 0x20) != 0) buttons |= DualSenseButton.Options;
        if ((b2 & 0x40) != 0) buttons |= DualSenseButton.L3;
        if ((b2 & 0x80) != 0) buttons |= DualSenseButton.R3;
        if ((b3 & 0x01) != 0) buttons |= DualSenseButton.Ps;
        if ((b3 & 0x02) != 0) buttons |= DualSenseButton.TouchpadClick;
        if ((b3 & 0x04) != 0) buttons |= DualSenseButton.Mute;

        var dpad = (DPadDirection)(b1 & 0x0F);
        if (dpad is DPadDirection.North or DPadDirection.NorthEast or DPadDirection.NorthWest)
            buttons |= DualSenseButton.DPadUp;
        if (dpad is DPadDirection.South or DPadDirection.SouthEast or DPadDirection.SouthWest)
            buttons |= DualSenseButton.DPadDown;
        if (dpad is DPadDirection.West or DPadDirection.NorthWest or DPadDirection.SouthWest)
            buttons |= DualSenseButton.DPadLeft;
        if (dpad is DPadDirection.East or DPadDirection.NorthEast or DPadDirection.SouthEast)
            buttons |= DualSenseButton.DPadRight;

        return new DualSenseState
        {
            LeftStickX = report[DualSenseProtocol.OffsetLeftStickX],
            LeftStickY = report[DualSenseProtocol.OffsetLeftStickY],
            RightStickX = report[DualSenseProtocol.OffsetRightStickX],
            RightStickY = report[DualSenseProtocol.OffsetRightStickY],
            LeftTrigger = report[DualSenseProtocol.OffsetLeftTrigger],
            RightTrigger = report[DualSenseProtocol.OffsetRightTrigger],
            DPad = dpad,
            Buttons = buttons,
            Finger1 = ParseFinger(report, DualSenseProtocol.OffsetTouch1),
            Finger2 = ParseFinger(report, DualSenseProtocol.OffsetTouch2),
            BatteryPercent = ParseBatteryPercent(report),
            IsCharging = (report[DualSenseProtocol.OffsetBattery] & 0x10) != 0,
        };
    }

    private static byte ParseBatteryPercent(ReadOnlySpan<byte> report)
    {
        // Low nibble is 0..15. Map to 0..100, clamp because some firmwares send slightly higher.
        int level = report[DualSenseProtocol.OffsetBattery] & 0x0F;
        int pct = (int)Math.Round(level * (100.0 / 15.0));
        return (byte)Math.Clamp(pct, 0, 100);
    }

    private static TouchpadFinger ParseFinger(ReadOnlySpan<byte> report, int offset)
    {
        // Byte 0: high bit set = no contact; low 7 bits = id.
        // Bytes 1..3: 12-bit X packed with 12-bit Y.
        byte b0 = report[offset];
        byte b1 = report[offset + 1];
        byte b2 = report[offset + 2];
        byte b3 = report[offset + 3];

        bool active = (b0 & 0x80) == 0;
        int x = b1 | ((b2 & 0x0F) << 8);
        int y = (b2 >> 4) | (b3 << 4);

        return new TouchpadFinger
        {
            Active = active,
            Id = (byte)(b0 & 0x7F),
            X = x,
            Y = y,
        };
    }
}

[Flags]
public enum DualSenseButton : uint
{
    None = 0,
    Square = 1u << 0,
    Cross = 1u << 1,
    Circle = 1u << 2,
    Triangle = 1u << 3,
    L1 = 1u << 4,
    R1 = 1u << 5,
    L2 = 1u << 6,
    R2 = 1u << 7,
    Create = 1u << 8,
    Options = 1u << 9,
    L3 = 1u << 10,
    R3 = 1u << 11,
    Ps = 1u << 12,
    TouchpadClick = 1u << 13,
    Mute = 1u << 14,
    DPadUp = 1u << 15,
    DPadDown = 1u << 16,
    DPadLeft = 1u << 17,
    DPadRight = 1u << 18,
}

public enum DPadDirection : byte
{
    North = 0,
    NorthEast = 1,
    East = 2,
    SouthEast = 3,
    South = 4,
    SouthWest = 5,
    West = 6,
    NorthWest = 7,
    Neutral = 8,
}
