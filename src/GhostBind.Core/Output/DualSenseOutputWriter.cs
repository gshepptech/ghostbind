using HidSharp;
using GhostBind.Core.Input;
using GhostBind.Core.Mapping;

namespace GhostBind.Core.Output;

// Sends DualSense USB output reports — controls lightbar, rumble, mic-mute LED.
// Layout taken from the Linux kernel hid-playstation driver (struct
// dualsense_output_report_common) with +1 offset for the report ID prepended at
// index 0. Cross-checked against DS4Windows (Ryochan7 fork). Buffer is 48 bytes.
//
// Byte map (offsets WITH the report ID at index 0):
//   0   : 0x02 (report ID)
//   1   : valid_flag0
//   2   : valid_flag1
//   3   : motor right (small / high-frequency)
//   4   : motor left  (large / low-frequency)
//   5-8 : reserved
//   9   : mute_button_led
//   10  : power_save_control
//   11-38 : reserved
//   39  : valid_flag2  (gates lightbar_setup)
//   40-41 : reserved
//   42  : lightbar_setup  (0x02 to take control / release default pulse)
//   43  : led_brightness  (0 = max, 1 = mid, 2 = dim)
//   44  : player_leds     (we leave 0)
//   45  : lightbar RED
//   46  : lightbar GREEN
//   47  : lightbar BLUE
public sealed class DualSenseOutputWriter
{
    private const byte ReportId = 0x02;

    // valid_flag0 bits
    private const byte FLAG0_RUMBLE = 0x01;
    private const byte FLAG0_HAPTICS = 0x02;

    // valid_flag1 bits
    private const byte FLAG1_MIC_MUTE_LED = 0x01;
    private const byte FLAG1_POWER_SAVE = 0x02;
    private const byte FLAG1_LIGHTBAR_CONTROL = 0x04;
    private const byte FLAG1_RELEASE_LEDS = 0x08;
    private const byte FLAG1_PLAYER_INDICATOR = 0x10;

    // valid_flag2 bits
    private const byte FLAG2_LIGHTBAR_SETUP = 0x02;

    // lightbar_setup values
    private const byte LIGHTBAR_SETUP_LIGHT_OUT = 0x02;  // takes control / clears default pulse

    private readonly HidStream _stream;
    private readonly byte[] _buffer;

    public DualSenseOutputWriter(HidStream stream, int outputReportLength)
    {
        _stream = stream;
        // Use the device-reported output report length instead of guessing. Different
        // DualSense firmwares may expect 47, 48, or 64 bytes — the device knows.
        _buffer = new byte[Math.Max(outputReportLength, 48)];
    }

    public byte LightbarR { get; set; }
    public byte LightbarG { get; set; }
    public byte LightbarB { get; set; }
    public byte RumbleLeft { get; set; }
    public byte RumbleRight { get; set; }
    public bool MicMuteLed { get; set; }

    // Adaptive trigger params (PS5). Mode + 9 param bytes per trigger.
    public TriggerEffectMode LeftTriggerMode { get; set; }
    public TriggerEffectMode RightTriggerMode { get; set; }
    public byte LeftTriggerStart { get; set; } = 2;
    public byte LeftTriggerEnd { get; set; } = 7;
    public byte LeftTriggerForce { get; set; } = 200;
    public byte RightTriggerStart { get; set; } = 2;
    public byte RightTriggerEnd { get; set; } = 7;
    public byte RightTriggerForce { get; set; } = 200;

    public bool LastWriteSucceeded { get; private set; }
    public string? LastError { get; private set; }

    public void Submit()
    {
        Array.Clear(_buffer, 0, _buffer.Length);

        _buffer[0] = ReportId;

        // Tell the controller which subsystems we're authoring values for.
        // Trigger control bits (0x04 / 0x08) added on valid_flag0 so the trigger
        // mode + params at offsets 11-32 are accepted.
        _buffer[1] = FLAG0_RUMBLE | FLAG0_HAPTICS | 0x04 | 0x08;
        _buffer[2] = FLAG1_LIGHTBAR_CONTROL | FLAG1_MIC_MUTE_LED;
        _buffer[39] = FLAG2_LIGHTBAR_SETUP;        // gates the lightbar_setup byte below

        // Rumble.
        _buffer[3] = RumbleRight;
        _buffer[4] = RumbleLeft;

        // Mic-mute LED.
        _buffer[9] = (byte)(MicMuteLed ? 1 : 0);

        // Adaptive trigger params. Right trigger at 11-20, left at 22-31. The
        // gap at 21 is reserved between the two trigger blocks.
        WriteTrigger(_buffer, offset: 11, RightTriggerMode, RightTriggerStart, RightTriggerEnd, RightTriggerForce);
        WriteTrigger(_buffer, offset: 22, LeftTriggerMode, LeftTriggerStart, LeftTriggerEnd, LeftTriggerForce);

        // Lightbar setup must be issued the first frame after takeover so the
        // controller stops pulsing its default pattern and accepts our RGB. Sending
        // it every frame is harmless.
        _buffer[42] = LIGHTBAR_SETUP_LIGHT_OUT;
        _buffer[43] = 0;                            // led_brightness (0 = max)
        _buffer[44] = 0;                            // player_leds (all off)

        // RGB at the canonical kernel-driver offsets — the bug we just fixed was
        // that these were at 42/43/44 (lightbar_setup / led_brightness / player_leds)
        // so the colors were silently being written into the wrong fields.
        _buffer[45] = LightbarR;
        _buffer[46] = LightbarG;
        _buffer[47] = LightbarB;

        try
        {
            _stream.Write(_buffer, 0, _buffer.Length);
            LastWriteSucceeded = true;
            LastError = null;
        }
        catch (Exception ex)
        {
            LastWriteSucceeded = false;
            LastError = ex.Message;
        }
    }

    // Pack one trigger's mode + parameters at the given offset. Layout per
    // mode follows DualSense-Windows / community PS5 protocol notes.
    private static void WriteTrigger(byte[] buf, int offset, TriggerEffectMode mode, byte start, byte end, byte force)
    {
        // Clear all 10 bytes (mode + 9 params) so old state doesn't leak.
        for (int i = 0; i < 10; i++) buf[offset + i] = 0;

        buf[offset] = (byte)mode;
        switch (mode)
        {
            case TriggerEffectMode.Rigid:
                // param[0] = start position (0..9), param[1] = force (0..255)
                buf[offset + 1] = (byte)Math.Clamp((int)start, 0, 9);
                buf[offset + 2] = force;
                break;

            case TriggerEffectMode.Section:
                // param[0] = start position, param[1] = end position
                buf[offset + 1] = (byte)Math.Clamp((int)start, 0, 9);
                buf[offset + 2] = (byte)Math.Clamp((int)end, 0, 9);
                break;

            case TriggerEffectMode.Weapon:
                // param[0] = start (2..7), param[1] = end (start+1..8), param[2] = force
                byte s = (byte)Math.Clamp((int)start, 2, 7);
                byte e = (byte)Math.Clamp((int)end, s + 1, 8);
                buf[offset + 1] = s;
                buf[offset + 2] = e;
                buf[offset + 3] = force;
                break;

            // Off / unknown — leave params zeroed.
        }
    }
}
