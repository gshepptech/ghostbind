using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using GhostBind.Core.Input;
using GhostBind.Core.Mapping;
using GhostBind.Core.Profiles;

namespace GhostBind.App.Views;

public partial class ButtonsPage : Page
{
    private static readonly DualSenseButton[] RemappableSources =
    {
        DualSenseButton.Cross, DualSenseButton.Circle, DualSenseButton.Square, DualSenseButton.Triangle,
        DualSenseButton.DPadUp, DualSenseButton.DPadDown, DualSenseButton.DPadLeft, DualSenseButton.DPadRight,
        DualSenseButton.L1, DualSenseButton.R1,
        DualSenseButton.L3, DualSenseButton.R3,
        DualSenseButton.Create, DualSenseButton.Options, DualSenseButton.Ps,
        DualSenseButton.TouchpadClick, DualSenseButton.Mute,
    };

    public IReadOnlyList<DualSenseButton> ShiftOptions { get; } =
        new[] { DualSenseButton.None }.Concat(RemappableSources).ToArray();

    private bool _suppressShift;

    public ButtonsPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += OnLoaded;
    }

    private DualSenseButton _shiftButton;
    public DualSenseButton ShiftButton
    {
        get => _shiftButton;
        set
        {
            _shiftButton = value;
            if (_suppressShift) return;
            AppHost.Controller.CurrentProfile.ButtonMap.ShiftButton = value;
        }
    }

    private void OnShiftChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressShift) return;
        AppHost.Controller.CurrentProfile.ButtonMap.ShiftButton = ShiftButton;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var map = AppHost.Controller.CurrentProfile.ButtonMap;

        _suppressShift = true;
        ShiftButton = map.ShiftButton;
        _suppressShift = false;

        var rows = new ObservableCollection<MappingRow>();
        foreach (var src in RemappableSources)
        {
            map.Mappings.TryGetValue(src, out var simple);
            map.Activators.TryGetValue(src, out var activator);
            map.Layer2Mappings.TryGetValue(src, out var layer2);
            rows.Add(new MappingRow(src, simple, activator, layer2));
        }
        MappingList.ItemsSource = rows;
    }

    public class MappingRow : INotifyPropertyChanged
    {
        public DualSenseButton Source { get; }
        public string SourceLabel => Source.DisplayLabel();
        public IReadOnlyList<OutputButton> TargetOptions { get; } = Enum.GetValues<OutputButton>();
        public IReadOnlyList<ActivatorMode> ModeOptions { get; } = Enum.GetValues<ActivatorMode>();

        private OutputButton _target;
        public OutputButton Target
        {
            get => _target;
            set { _target = value; Persist(); OnPropertyChanged(); }
        }

        private OutputButton _layer2Target;
        public OutputButton Layer2Target
        {
            get => _layer2Target;
            set { _layer2Target = value; PersistLayer2(); OnPropertyChanged(); }
        }

        private ActivatorMode _mode;
        public ActivatorMode Mode
        {
            get => _mode;
            set
            {
                _mode = value;
                Persist();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ThresholdVisible));
            }
        }

        private int _threshold = 250;
        public int Threshold
        {
            get => _threshold;
            set { _threshold = value; Persist(); OnPropertyChanged(); }
        }

        public Visibility ThresholdVisible =>
            _mode is ActivatorMode.Hold or ActivatorMode.Tap or ActivatorMode.DoubleTap
                ? Visibility.Visible : Visibility.Collapsed;

        public MappingRow(DualSenseButton source, OutputButton current, ButtonActivator? activator, OutputButton layer2)
        {
            Source = source;
            _target = activator?.Output ?? current;
            _layer2Target = layer2;
            _mode = activator?.Mode ?? ActivatorMode.Regular;
            _threshold = activator?.Mode switch
            {
                ActivatorMode.Hold => activator.HoldThresholdMs,
                ActivatorMode.Tap => activator.TapMaxMs,
                ActivatorMode.DoubleTap => activator.DoubleTapWindowMs,
                _ => 250,
            };
        }

        private void Persist()
        {
            var map = AppHost.Controller.CurrentProfile.ButtonMap;

            if (_mode == ActivatorMode.Regular)
            {
                map.Activators.Remove(Source);

                if (_target == OutputButton.None)
                    map.Mappings.Remove(Source);
                else
                    map.Mappings[Source] = _target;
            }
            else
            {
                var act = new ButtonActivator { Mode = _mode, Output = _target };
                switch (_mode)
                {
                    case ActivatorMode.Hold:      act.HoldThresholdMs   = _threshold; break;
                    case ActivatorMode.Tap:       act.TapMaxMs          = _threshold; break;
                    case ActivatorMode.DoubleTap: act.DoubleTapWindowMs = _threshold; break;
                }
                map.Activators[Source] = act;
            }
        }

        private void PersistLayer2()
        {
            var map = AppHost.Controller.CurrentProfile.ButtonMap;
            if (_layer2Target == OutputButton.None)
                map.Layer2Mappings.Remove(Source);
            else
                map.Layer2Mappings[Source] = _layer2Target;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? prop = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
    }
}
