using System.ComponentModel;
using System.Runtime;
using System.Runtime.CompilerServices;
using GhostBind.Core.Input;
using GhostBind.Core.Mapping;
using GhostBind.Core.Output;
using GhostBind.Core.Profiles;

namespace GhostBind.Core;

public enum ServiceStatus
{
    Stopped,
    Connecting,
    Running,
    NoController,
    ViGEmMissing,
    Error,
}

public sealed class ControllerService : INotifyPropertyChanged, IDisposable
{
    private DualSenseReader? _reader;
    private IVirtualPad? _output;
    private DualSenseOutputWriter? _ds5Writer;
    public int PollingDelayMicros { get; set; } = 0; // 0 = uncapped (default), >0 = sleep N μs each iteration
    public int ButtonDebounceMs
    {
        get => _engine.ButtonDebounceMs;
        set => _engine.ButtonDebounceMs = value;
    }
    private readonly TouchpadMouseDriver _touchpad = new();
    private readonly MappingEngine _engine = new();
    private CancellationTokenSource? _cts;
    private Thread? _loopThread;
    private int _outputReportTickCounter;

    // Telemetry — exponentially smoothed so the GUI gets a stable readout.
    private double _pollHzEma;
    private double _frameMicrosEma;
    private DateTime _lastPollAt;

    private ServiceStatus _status = ServiceStatus.Stopped;
    private string _statusMessage = "Idle";
    private Profile _currentProfile = Profile.Default();

    public Profile CurrentProfile
    {
        get => _currentProfile;
        set { _currentProfile = value; OnPropertyChanged(); }
    }

    public double PollRateHz => _pollHzEma;
    public double FrameProcessingMicros => _frameMicrosEma;
    public string? LastWriteError => _ds5Writer?.LastError;
    public bool LastOutputWriteOk => _ds5Writer?.LastWriteSucceeded ?? false;

    public ServiceStatus Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public event EventHandler<ProcessedSnapshot>? StateUpdated;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void Start()
    {
        if (_loopThread != null) return;

        Status = ServiceStatus.Connecting;
        StatusMessage = "Looking for DualSense...";

        try
        {
            _reader = DualSenseReader.Open();
        }
        catch (InvalidOperationException ex)
        {
            Status = ServiceStatus.NoController;
            StatusMessage = ex.Message;
            return;
        }

        try
        {
            _output = CurrentProfile.OutputType == OutputControllerType.DualShock4
                ? new DS4Output()
                : new X360Output();
        }
        catch (Exception ex)
        {
            _reader.Dispose();
            _reader = null;
            Status = ServiceStatus.ViGEmMissing;
            StatusMessage = "ViGEmBus not installed or failed to initialize: " + ex.Message;
            return;
        }

        _ds5Writer = new DualSenseOutputWriter(_reader.Stream, _reader.MaxOutputReportLength);

        _cts = new CancellationTokenSource();
        Status = ServiceStatus.Running;
        StatusMessage = "Connected.";

        // Dedicated high-priority thread instead of ThreadPool. The input loop is
        // latency-critical — we don't want it descheduled during heavy CPU load.
        // Also tells the GC to defer collections during gameplay so a stop-the-world
        // pause never lands inside the 4ms USB poll window.
        var token = _cts.Token;
        _loopThread = new Thread(() =>
        {
            try { GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency; } catch { }
            RunLoop(token);
        })
        {
            Name = "GhostBind.ControllerLoop",
            IsBackground = true,
            Priority = ThreadPriority.AboveNormal,
        };
        _loopThread.Start();
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _loopThread?.Join(1000); } catch { /* ignore on shutdown */ }
        _loopThread = null;
        try { GCSettings.LatencyMode = GCLatencyMode.Interactive; } catch { }

        _output?.Dispose();
        _output = null;
        _ds5Writer = null;
        _reader?.Dispose();
        _reader = null;
        _cts?.Dispose();
        _cts = null;

        Status = ServiceStatus.Stopped;
        StatusMessage = "Stopped.";
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    private void RunLoop(CancellationToken ct)
    {
        var sw = new System.Diagnostics.Stopwatch();
        try
        {
            while (!ct.IsCancellationRequested)
            {
                if (_reader!.TryRead(out var raw))
                {
                    var now = DateTime.UtcNow;
                    if (_lastPollAt != default)
                    {
                        double dt = (now - _lastPollAt).TotalSeconds;
                        if (dt > 0)
                        {
                            double hz = 1.0 / dt;
                            _pollHzEma = _pollHzEma == 0 ? hz : _pollHzEma * 0.9 + hz * 0.1;
                        }
                    }
                    _lastPollAt = now;

                    sw.Restart();
                    var snap = _engine.ProcessAndApply(raw, CurrentProfile, _output!);
                    _output.Submit();
                    _touchpad.Tick(raw, CurrentProfile.Touchpad);
                    sw.Stop();
                    double micros = sw.Elapsed.TotalMicroseconds;
                    _frameMicrosEma = _frameMicrosEma == 0 ? micros : _frameMicrosEma * 0.9 + micros * 0.1;

                    // Output reports (lightbar / rumble) need to fire often enough that
                    // game-driven rumble feels live. ~4 input packets = ~60Hz output.
                    if (++_outputReportTickCounter % 4 == 0 && _ds5Writer != null)
                    {
                        var lb = CurrentProfile.Lightbar;
                        if (lb.Enabled)
                        {
                            _ds5Writer.LightbarR = lb.Red;
                            _ds5Writer.LightbarG = lb.Green;
                            _ds5Writer.LightbarB = lb.Blue;
                        }
                        else
                        {
                            _ds5Writer.LightbarR = 0;
                            _ds5Writer.LightbarG = 0;
                            _ds5Writer.LightbarB = 0;
                        }

                        // Forward rumble from game → physical pad.
                        _ds5Writer.RumbleLeft = _output.LargeMotor;
                        _ds5Writer.RumbleRight = _output.SmallMotor;

                        // Adaptive trigger feedback (PS5).
                        var lt = CurrentProfile.LeftTrigger;
                        _ds5Writer.LeftTriggerMode = lt.EffectMode;
                        _ds5Writer.LeftTriggerStart = lt.EffectStart;
                        _ds5Writer.LeftTriggerEnd = lt.EffectEnd;
                        _ds5Writer.LeftTriggerForce = lt.EffectForce;

                        var rt = CurrentProfile.RightTrigger;
                        _ds5Writer.RightTriggerMode = rt.EffectMode;
                        _ds5Writer.RightTriggerStart = rt.EffectStart;
                        _ds5Writer.RightTriggerEnd = rt.EffectEnd;
                        _ds5Writer.RightTriggerForce = rt.EffectForce;

                        _ds5Writer.Submit();
                    }

                    StateUpdated?.Invoke(this, snap);
                }

                if (PollingDelayMicros > 0)
                {
                    // Coarse but adequate — a busy spin gives us sub-ms granularity.
                    var until = sw.ElapsedTicks + (PollingDelayMicros * System.Diagnostics.Stopwatch.Frequency / 1_000_000);
                    while (sw.ElapsedTicks < until && !ct.IsCancellationRequested) { /* spin */ }
                }
            }
        }
        catch (Exception ex)
        {
            Status = ServiceStatus.Error;
            StatusMessage = ex.Message;
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? prop = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));

    public void Dispose() => Stop();
}
