using System.Runtime.InteropServices;

namespace GhostBind.Core.Output;

// Thin wrapper over Win32 SendInput for relative mouse motion + button clicks.
// Used by the touchpad-as-mouse and (later) keyboard/mouse output bindings.
public static class MouseInjector
{
    [Flags]
    private enum MouseEventFlags : uint
    {
        Move = 0x0001,
        LeftDown = 0x0002,
        LeftUp = 0x0004,
        RightDown = 0x0008,
        RightUp = 0x0010,
        MiddleDown = 0x0020,
        MiddleUp = 0x0040,
        Wheel = 0x0800,
        Absolute = 0x8000,
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public IntPtr ExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void MoveRelative(int dx, int dy)
    {
        if (dx == 0 && dy == 0) return;
        var input = new INPUT
        {
            Type = 0, // INPUT_MOUSE
            Data = { Mouse = { Dx = dx, Dy = dy, Flags = (uint)MouseEventFlags.Move } },
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }

    public static void LeftDown() => Send(MouseEventFlags.LeftDown);
    public static void LeftUp() => Send(MouseEventFlags.LeftUp);
    public static void RightDown() => Send(MouseEventFlags.RightDown);
    public static void RightUp() => Send(MouseEventFlags.RightUp);
    public static void MiddleDown() => Send(MouseEventFlags.MiddleDown);
    public static void MiddleUp() => Send(MouseEventFlags.MiddleUp);

    private static void Send(MouseEventFlags flag)
    {
        var input = new INPUT
        {
            Type = 0,
            Data = { Mouse = { Flags = (uint)flag } },
        };
        SendInput(1, new[] { input }, Marshal.SizeOf<INPUT>());
    }
}
