using HidSharp;

namespace GhostBind.Core.Input;

public sealed class DualSenseReader : IDisposable
{
    private readonly HidStream _stream;
    private readonly byte[] _buffer;

    private DualSenseReader(HidStream stream, int reportLength)
    {
        _stream = stream;
        _buffer = new byte[reportLength];
    }

    public static DualSenseReader Open()
    {
        var device = DeviceList.Local
            .GetHidDevices(DualSenseProtocol.VendorIdSony)
            .FirstOrDefault(d =>
                d.ProductID == DualSenseProtocol.ProductIdDualSense ||
                d.ProductID == DualSenseProtocol.ProductIdDualSenseEdge)
            ?? throw new InvalidOperationException(
                "No DualSense controller found. Plug it in via USB and try again.");

        var config = new OpenConfiguration();
        config.SetOption(OpenOption.Exclusive, true);

        HidStream stream;
        try
        {
            stream = device.Open(config);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to open DualSense exclusively. Another app (DS4Windows, " +
                "Steam Input, browser, etc.) may be holding the device. Close it and retry.",
                ex);
        }

        stream.ReadTimeout = 1000;
        var reader = new DualSenseReader(stream, device.GetMaxInputReportLength());
        reader.MaxOutputReportLength = device.GetMaxOutputReportLength();
        return reader;
    }

    public HidStream Stream => _stream;
    public int MaxOutputReportLength { get; private set; }

    public bool TryRead(out DualSenseState state)
    {
        try
        {
            int read = _stream.Read(_buffer, 0, _buffer.Length);
            if (read >= DualSenseProtocol.UsbInputReportLength &&
                _buffer[0] == DualSenseProtocol.UsbInputReportId)
            {
                state = DualSenseState.Parse(_buffer);
                return true;
            }
        }
        catch (TimeoutException)
        {
        }

        state = default;
        return false;
    }

    public void Dispose() => _stream.Dispose();
}
