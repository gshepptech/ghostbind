using System.Windows;
using System.Windows.Controls;

namespace GhostBind.App.Views;

public partial class PresetsPage : Page
{
    public PresetsPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PresetList.ItemsSource = ProfilePresets.All;
    }

    private void OnApplyClick(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement fe || fe.Tag is not string name) return;

        var preset = ProfilePresets.All.FirstOrDefault(p => p.Name == name);
        if (preset == null) return;

        var profile = AppHost.Controller.CurrentProfile;
        preset.Apply(profile);

        try { AppHost.ProfileStore.Save(profile); }
        catch { /* best effort save; in-memory profile is still updated */ }

        StatusLine.Text = $"Applied '{preset.Name}' to '{profile.Name}'. Settings live now.";
    }
}
