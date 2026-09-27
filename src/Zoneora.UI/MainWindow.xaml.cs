using System.Windows;
using System.Windows.Input;
using Zoneora.Core;
using Zoneora.WindowsIntegration;

namespace Zoneora.UI;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly SettingsManager settingsManager = new();
    private DesktopGridWindow? desktopGrid;
    private AppSettings settings = new();
    private ZoneManager zoneManager = new();
    private bool isLoadingSettings;
    private readonly TrayIconController trayIcon = new();
    private readonly DesktopIconController desktopIconController = new();

    public MainWindow()
    {
        isLoadingSettings = true;
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
        trayIcon.RestoreRequested += (_, _) => RestoreFromTray();
        trayIcon.ExitRequested += (_, _) => System.Windows.Application.Current.Shutdown();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Right - Width - 24;
        Top = SystemParameters.WorkArea.Bottom - Height - 24;
        settings = await settingsManager.LoadAsync();
        isLoadingSettings = true;
        UpdateSettingsControls();
        isLoadingSettings = false;
        desktopIconController.SetVisible(!settings.DesktopIconsHidden);
        zoneManager = new ZoneManager(settings.Zones);
        desktopGrid = new DesktopGridWindow(zoneManager.Zones);
        desktopGrid.Changed += OnGridChanged;
        desktopGrid.SetGridVisible(settings.ShowGrid);
        desktopGrid.Show();

        UpdateStatus();
    }

    private async void OnCreateZoneClick(object sender, RoutedEventArgs e)
    {
        ZoneModel zone = zoneManager.Create($"Zone {zoneManager.Zones.Count + 1}");
        settings.Zones = zoneManager.Zones.ToList();
        desktopGrid?.AddZone(zone);
        await settingsManager.SaveAsync(settings);
        UpdateStatus();
    }

    private async void OnGridChanged(object? sender, EventArgs e)
    {
        settings.Zones = desktopGrid?.Zones.ToList() ?? [];
        zoneManager = new ZoneManager(settings.Zones);
        await settingsManager.SaveAsync(settings);
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        settings.Zones = zoneManager.Zones.ToList();
        await settingsManager.SaveAsync(settings);
        desktopGrid?.Close();
        trayIcon.Dispose();
    }

    private void UpdateStatus()
    {
        ZoneStatus.Text = zoneManager.Zones.Count == 0
            ? "No zones yet"
            : $"{zoneManager.Zones.Count} zone{(zoneManager.Zones.Count == 1 ? string.Empty : "s")} active on the desktop";
    }

    private void UpdateSettingsControls()
    {
        OledEnabledCheckBox.IsChecked = settings.OledMode.Enabled;
        FadeInComboBox.SelectedIndex = FindDurationIndex(FadeInComboBox, settings.OledMode.FadeInMilliseconds);
        FadeOutComboBox.SelectedIndex = FindDurationIndex(FadeOutComboBox, settings.OledMode.FadeOutMilliseconds);
        IdleTimeoutComboBox.SelectedIndex = settings.OledMode.IdleTimeoutMinutes is int minutes
            ? FindTagIndex(IdleTimeoutComboBox, minutes.ToString())
            : FindTagIndex(IdleTimeoutComboBox, "never");
        HideZonesCheckBox.IsChecked = settings.OledMode.HideZones;
        HideDesktopIconsCheckBox.IsChecked = settings.OledMode.HideDesktopIcons;
        DisableFullscreenCheckBox.IsChecked = settings.OledMode.DisableInFullscreenApps;
        ShowGridCheckBox.IsChecked = settings.ShowGrid;
        HideDesktopIconsNowCheckBox.IsChecked = settings.DesktopIconsHidden;
    }

    private async void OnOledSettingChanged(object sender, RoutedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        settings.OledMode.Enabled = OledEnabledCheckBox.IsChecked == true;
        settings.OledMode.HideZones = HideZonesCheckBox.IsChecked == true;
        settings.OledMode.HideDesktopIcons = HideDesktopIconsCheckBox.IsChecked == true;
        settings.OledMode.DisableInFullscreenApps = DisableFullscreenCheckBox.IsChecked == true;
        await settingsManager.SaveAsync(settings);
    }

    private async void OnOledSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        if (FadeInComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem fadeIn && int.TryParse(fadeIn.Tag?.ToString(), out int fadeInMilliseconds))
        {
            settings.OledMode.FadeInMilliseconds = fadeInMilliseconds;
        }
        if (FadeOutComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem fadeOut && int.TryParse(fadeOut.Tag?.ToString(), out int fadeOutMilliseconds))
        {
            settings.OledMode.FadeOutMilliseconds = fadeOutMilliseconds;
        }
        if (IdleTimeoutComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem idleTimeout)
        {
            settings.OledMode.IdleTimeoutMinutes = idleTimeout.Tag?.ToString() == "never"
                ? null
                : int.Parse(idleTimeout.Tag!.ToString()!);
        }
        await settingsManager.SaveAsync(settings);
    }

    private void OnDesktopViewClick(object sender, RoutedEventArgs e)
    {
        DesktopView.Visibility = Visibility.Visible;
        SettingsView.Visibility = Visibility.Collapsed;
        DesktopViewButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 243, 107));
        SettingsViewButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 50, 56));
    }

    private void OnSettingsViewClick(object sender, RoutedEventArgs e)
    {
        DesktopView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
        DesktopViewButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(38, 50, 56));
        SettingsViewButton.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(184, 243, 107));
    }

    private void OnHeaderMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
            DragMove();
        }
    }

    private async void OnShowGridChanged(object sender, RoutedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        settings.ShowGrid = ShowGridCheckBox.IsChecked == true;
        desktopGrid?.SetGridVisible(settings.ShowGrid);
        await settingsManager.SaveAsync(settings);
    }

    private async void OnHideDesktopIconsNowChanged(object sender, RoutedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        settings.DesktopIconsHidden = HideDesktopIconsNowCheckBox.IsChecked == true;
        desktopIconController.SetVisible(!settings.DesktopIconsHidden);
        await settingsManager.SaveAsync(settings);
    }

    private void OnBuildModeClick(object sender, RoutedEventArgs e)
    {
        bool enabled = BuildModeToggle.IsChecked == true;
        desktopGrid?.SetBuildMode(enabled);
        BuildModeToggle.Background = new System.Windows.Media.SolidColorBrush(enabled
            ? System.Windows.Media.Color.FromRgb(184, 243, 107)
            : System.Windows.Media.Color.FromRgb(38, 50, 56));
        BuildModeToggle.Foreground = new System.Windows.Media.SolidColorBrush(enabled
            ? System.Windows.Media.Color.FromRgb(16, 21, 24)
            : System.Windows.Media.Color.FromRgb(242, 244, 238));
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        Hide();
        trayIcon.Show("Zoneora");
    }

    private void RestoreFromTray()
    {
        Show();
        Activate();
        trayIcon.Hide();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private static int FindDurationIndex(System.Windows.Controls.ComboBox comboBox, int duration)
    {
        return FindTagIndex(comboBox, duration.ToString());
    }

    private static int FindTagIndex(System.Windows.Controls.ComboBox comboBox, string tag)
    {
        for (int index = 0; index < comboBox.Items.Count; index++)
        {
            if (comboBox.Items[index] is System.Windows.Controls.ComboBoxItem item && item.Tag?.ToString() == tag)
            {
                return index;
            }
        }

        return 0;
    }
}