using System.Windows;
using System.Windows.Controls;
using GhostBind.Core;
using GhostBind.Core.Profiles;

namespace GhostBind.App.Views;

public partial class ProfilesPage : Page
{
    private bool _suppressEvents;

    public ProfilesPage()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        OutputTypePicker.ItemsSource = Enum.GetValues<OutputControllerType>();
        RefreshList();
    }

    private void RefreshList()
    {
        _suppressEvents = true;
        try
        {
            var names = AppHost.ProfileStore.ListNames();
            ProfileSelector.ItemsSource = names;
            ProfileSelector.SelectedItem = AppHost.ActiveProfileName ?? names.FirstOrDefault();
            OutputTypePicker.SelectedItem = AppHost.Controller.CurrentProfile.OutputType;
        }
        finally { _suppressEvents = false; }
    }

    private void OnOutputTypeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (OutputTypePicker.SelectedItem is not OutputControllerType t) return;

        AppHost.Controller.CurrentProfile.OutputType = t;
        AppHost.Controller.Restart();
        StatusLine.Text = $"Output type changed to {t}. Service restarted.";
    }

    private void OnProfileChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressEvents) return;
        if (ProfileSelector.SelectedItem is not string name) return;

        // Save current first so unsaved tweaks aren't lost when switching.
        try { AppHost.ProfileStore.Save(AppHost.Controller.CurrentProfile); } catch { }

        AppHost.Controller.CurrentProfile = AppHost.ProfileStore.Load(name);
        AppHost.ActiveProfileName = name;
        StatusLine.Text = $"Switched to '{name}'.";
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            AppHost.ProfileStore.Save(AppHost.Controller.CurrentProfile);
            StatusLine.Text = $"Saved '{AppHost.Controller.CurrentProfile.Name}'.";
        }
        catch (Exception ex)
        {
            StatusLine.Text = "Save failed: " + ex.Message;
        }
    }

    private void OnDelete(object sender, RoutedEventArgs e)
    {
        if (ProfileSelector.SelectedItem is not string name) return;
        if (name == "Default")
        {
            StatusLine.Text = "Default profile cannot be deleted.";
            return;
        }

        AppHost.ProfileStore.Delete(name);
        StatusLine.Text = $"Deleted '{name}'.";
        RefreshList();
    }

    private void OnCreate(object sender, RoutedEventArgs e)
    {
        var name = NewName.Text?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusLine.Text = "Profile name cannot be empty.";
            return;
        }

        var profile = new Profile { Name = name };
        AppHost.ProfileStore.Save(profile);
        AppHost.Controller.CurrentProfile = profile;
        AppHost.ActiveProfileName = name;
        NewName.Text = string.Empty;
        StatusLine.Text = $"Created '{name}'.";
        RefreshList();
    }
}
