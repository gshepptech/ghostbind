using System.Windows;
using System.Windows.Controls;

namespace GhostBind.App.Views;

public partial class TuningGuidePage : Page
{
    public sealed record TuneRow(string Symptom, string Change, string Direction);

    private static readonly TuneRow[] CurveTuningRows =
    {
        new(
            "Edits/builds feel too slow — micro-adjusting the build wheel takes forever",
            "Drag the 0.25 anchor UP (currently 0.07 → try 0.10–0.13)",
            "Lifts low-end output"),
        new(
            "Aim is too twitchy near center — pulling slightly = camera flies",
            "Drag the 0.25 anchor DOWN (toward 0.04–0.05)",
            "Slows low-end"),
        new(
            "Mid-range feels gummy — half-stick doesn't go anywhere",
            "Drag the 0.50 anchor UP (currently 0.22 → try 0.30)",
            "Faster mid-range"),
        new(
            "180-turns aren't fast enough — can't whip around for builds",
            "Drag the 0.75 anchor UP (currently 0.55 → try 0.65–0.70). Don't move the endpoint — keep 1.0 → 1.0",
            "More speed in upper half"),
        new(
            "Tracking feels off — can't follow a moving target smoothly",
            "Drag the 0.50 anchor DOWN (try 0.18)",
            "Smoother low-end transition"),
        new(
            "Aim assist doesn't engage / feels broken",
            "Drag 0.25 anchor DOWN slightly (0.05–0.06) so you stay in aim assist's 'tracking zone' longer",
            "Aim assist needs predictable low-end"),
    };

    private static readonly TuneRow[] OtherTuningRows =
    {
        new(
            "Game still feels like there's a deadzone even with our 0.05 set",
            "Anti-deadzone slider",
            "Bump up: 0.13 → 0.16 → 0.18"),
        new(
            "Output spazzes when stick is centered (drift)",
            "Inner deadzone",
            "Bump up: 0.05 → 0.07 → 0.10"),
        new(
            "Stick can't reach max output (worn DualSense)",
            "Outer deadzone",
            "Drop: 1.0 → 0.95 → 0.90"),
        new(
            "Whole game is too slow",
            "Sensitivity in GhostBind, OR Fortnite in-game look sensitivity",
            "Sensitivity slider in GhostBind is a multiplier; in-game has finer granularity"),
        new(
            "Whole game is too twitchy",
            "Same — drop sensitivity",
            "Start with sensitivity 1.0 in GhostBind, dial Fortnite's own"),
        new(
            "Phantom inputs after fast flicks (rare in Fortnite, common with worn sticks)",
            "Anti-snapback checkbox",
            "Toggle on"),
        new(
            "Shooting feels delayed",
            "R2 saturation",
            "Lower: 0.65 → 0.55. The lower it goes, the less travel before fully firing"),
        new(
            "Accidental ADS",
            "L2 saturation",
            "Raise: 0.85 → 0.95 — requires more pull to ADS"),
    };

    public TuningGuidePage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        CurveRows.ItemsSource = CurveTuningRows;
        OtherRows.ItemsSource = OtherTuningRows;
    }
}
