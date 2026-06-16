using GhostBind.Core;
using GhostBind.Core.Input;

if (args.Contains("--diag-buttons"))
{
    return DiagnoseButtons();
}

Console.WriteLine("GhostBind CLI — DualSense → Xbox 360 bridge");

using var service = new ControllerService();
service.PropertyChanged += (_, e) =>
{
    if (e.PropertyName is nameof(ControllerService.Status) or nameof(ControllerService.StatusMessage))
    {
        Console.WriteLine($"[{service.Status}] {service.StatusMessage}");
    }
};

service.Start();

if (service.Status is ServiceStatus.NoController or ServiceStatus.ViGEmMissing or ServiceStatus.Error)
{
    return 1;
}

Console.WriteLine("Press Ctrl+C to exit.");

using var cancel = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };
cancel.Token.WaitHandle.WaitOne();

service.Stop();
return 0;

// --diag-buttons: Just read the DualSense and print every button state change
// with millisecond-precision timestamps. Reveals microswitch chatter that's
// invisible at frame rate. Run with the main GhostBind app stopped (it holds
// the HID handle exclusively).
static int DiagnoseButtons()
{
    Console.WriteLine("DIAGNOSE: button transitions with millisecond timestamps.");
    Console.WriteLine("Stop GhostBind first if it's running. Press Ctrl+C to exit.");
    Console.WriteLine();

    DualSenseReader reader;
    try { reader = DualSenseReader.Open(); }
    catch (Exception ex) { Console.Error.WriteLine(ex.Message); return 1; }

    using var cancel = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancel.Cancel(); };

    DualSenseButton lastButtons = DualSenseButton.None;
    var startedAt = DateTime.UtcNow;

    try
    {
        while (!cancel.IsCancellationRequested)
        {
            if (!reader.TryRead(out var state)) continue;

            var now = DateTime.UtcNow;
            if (state.Buttons == lastButtons) continue;

            // Find which buttons changed (XOR), then log each transition direction.
            var changed = state.Buttons ^ lastButtons;
            foreach (DualSenseButton b in Enum.GetValues<DualSenseButton>())
            {
                if (b == DualSenseButton.None) continue;
                if ((changed & b) == 0) continue;
                bool nowPressed = (state.Buttons & b) != 0;
                var elapsed = (now - startedAt).TotalMilliseconds;
                Console.WriteLine($"{elapsed,9:F1} ms  {(nowPressed ? "DOWN" : " UP "),-4}  {b}");
            }
            lastButtons = state.Buttons;
        }
    }
    finally { reader.Dispose(); }

    return 0;
}
