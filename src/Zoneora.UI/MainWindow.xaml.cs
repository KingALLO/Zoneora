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
    private readonly WindowsStartupController startupController = new();

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
        ShowGridCheckBox.IsChecked = settings.ShowGrid;
        HideDesktopIconsNowCheckBox.IsChecked = settings.DesktopIconsHidden;
        StartWithWindowsCheckBox.IsChecked = startupController.IsEnabled();
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

    private void OnStartWithWindowsChanged(object sender, RoutedEventArgs e)
    {
        if (isLoadingSettings)
        {
            return;
        }

        startupController.SetEnabled(StartWithWindowsCheckBox.IsChecked == true);
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
}