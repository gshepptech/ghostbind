namespace GhostBind.Core.Input;

internal static class DualSenseProtocol
{
    public const int VendorIdSony = 0x054C;
    public const int ProductIdDualSense = 0x0CE6;
    public const int ProductIdDualSenseEdge = 0x0DF2;

    public const byte UsbInputReportId = 0x01;
    public const int UsbInputReportLength = 64;

    // Byte offsets into the raw USB input report (HidStream.Read includes the
    // report ID at index 0 on Windows, so all data offsets shift by one).
    public const int OffsetLeftStickX = 1;
    public const int OffsetLeftStickY = 2;
    public const int OffsetRightStickX = 3;
    public const int OffsetRightStickY = 4;
    public const int OffsetLeftTrigger = 5;
    public const int OffsetRightTrigger = 6;
    public const int OffsetButtons1 = 8;  // dpad (low nibble) + face buttons (high nibble)
    public const int OffsetButtons2 = 9;  // L1/R1, L2/R2 click, Create, Options, L3/R3
    public const int OffsetButtons3 = 10; // PS, touchpad click, mute

    // Touchpad: two simultaneous fingers, 4 bytes each.
    // High bit of byte 0 = "no contact" (1 = no touch). Low 7 bits = touch id.
    // Then 12-bit X (1920 wide) packed with 12-bit Y (1080 tall) across 3 bytes.
    public const int OffsetTouch1 = 33;
    public const int OffsetTouch2 = 37;
    public const int TouchpadWidth = 1920;
    public const int TouchpadHeight = 1080;

    // Output report (USB) — 48 bytes incl. report ID, sent via HidStream.Write.
    // Lightbar/rumble live here. Bluetooth uses ID 0x31 with CRC32; we're USB-only.
    public const byte UsbOutputReportId = 0x02;
    public const int UsbOutputReportLength = 48;

    // Battery byte — low nibble = level (0..15), bit 4 = charging.
    public const int OffsetBattery = 53;
}
