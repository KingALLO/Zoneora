using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Zoneora.Core;

namespace Zoneora.UI;

public partial class ZoneControl : UserControl
{
    private const double GridColumns = 12;
    private const double GridRows = 8;
    private const int MinSpan = 1;
    private static readonly TimeSpan InitialVisibleDuration = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HoverVisibleDuration = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan FadeDuration = TimeSpan.FromMilliseconds(400);
    private readonly Canvas hostCanvas;
    private Point dragStart;
    private double startLeft;
    private double startTop;
    private bool shiftHeld;
    private bool buildModeActive;
    private DispatcherTimer? autoHideTimer;
    private Point itemDragStart;
    private ItemModel? itemDragCandidate;

    public ZoneControl(ZoneModel model, Canvas hostCanvas)
    {
        Model = model;
        this.hostCanvas = hostCanvas;
        InitializeComponent();
        TitleEditor.Text = model.Name;
        ItemsList.ItemsSource = model.Items.Select(ShellItemViewModel.Create).ToList();
        Loaded += (_, _) =>
        {
            ApplyGridBounds();
            ScheduleAutoHide(InitialVisibleDuration);
        };
    }

    public ZoneModel Model { get; }
    public event EventHandler? Changed;
    public event EventHandler? CloseRequested;

    public void ApplyGridBounds()
    {
        if (hostCanvas.ActualWidth <= 0 || hostCanvas.ActualHeight <= 0)
        {
            return;
        }

        double cellWidth = hostCanvas.ActualWidth / GridColumns;
        double cellHeight = hostCanvas.ActualHeight / GridRows;
        double left = Math.Clamp(Model.GridColumn - 1, 0, GridColumns - Model.GridColumnSpan) * cellWidth;
        double top = Math.Clamp(Model.GridRow - 1, 0, GridRows - Model.GridRowSpan) * cellHeight;
        double width = Math.Max(120, Model.GridColumnSpan * cellWidth - 8);
        double height = Math.Max(90, Model.GridRowSpan * cellHeight - 8);
        Canvas.SetLeft(this, left + 4);
        Canvas.SetTop(this, top + 4);
        Width = width;
        Height = height;
        Model.Left = left + 4;
        Model.Top = top + 4;
        Model.Width = width;
        Model.Height = height;
    }

    private void OnZoneMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left || e.OriginalSource is Button or TextBox)
        {
            return;
        }

        KeepVisible();

        if (e.ClickCount == 2 && sender == TitleBorder)
        {
            BeginRename();
            e.Handled = true;
            return;
        }

        dragStart = e.GetPosition(hostCanvas);
        startLeft = Canvas.GetLeft(this);
        startTop = Canvas.GetTop(this);
        CaptureMouse();
        MouseMove += OnZoneMouseMove;
        MouseLeftButtonUp += OnZoneMouseLeftButtonUp;
        e.Handled = true;
    }

    private void OnZoneMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            StopDragging();
            return;
        }

        Point current = e.GetPosition(hostCanvas);
        double cellWidth = hostCanvas.ActualWidth / GridColumns;
        double cellHeight = hostCanvas.ActualHeight / GridRows;
        double left = Math.Clamp(startLeft + current.X - dragStart.X, 0, hostCanvas.ActualWidth - ActualWidth);
        double top = Math.Clamp(startTop + current.Y - dragStart.Y, 0, hostCanvas.ActualHeight - ActualHeight);
        int column = Math.Clamp((int)Math.Round(left / cellWidth) + 1, 1, (int)GridColumns - Model.GridColumnSpan + 1);
        int row = Math.Clamp((int)Math.Round(top / cellHeight) + 1, 1, (int)GridRows - Model.GridRowSpan + 1);
        Model.GridColumn = column;
        Model.GridRow = row;
        ApplyGridBounds();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnZoneMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => StopDragging();

    private void StopDragging()
    {
        ReleaseMouseCapture();
        MouseMove -= OnZoneMouseMove;
        MouseLeftButtonUp -= OnZoneMouseLeftButtonUp;
    }

    private void OnTitleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            CommitTitle();
            Keyboard.ClearFocus();
        }
    }

    private void OnTitleLostFocus(object sender, RoutedEventArgs e)
    {
        CommitTitle();
        TitleEditor.IsHitTestVisible = false;
    }

    private void BeginRename()
    {
        TitleEditor.IsHitTestVisible = true;
        TitleEditor.Focus();
        TitleEditor.SelectAll();
    }

    private void CommitTitle()
    {
        string name = TitleEditor.Text.Trim();
        Model.Name = string.IsNullOrWhiteSpace(name) ? "New zone" : name;
        TitleEditor.Text = Model.Name;
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void SetBuildMode(bool enabled)
    {
        buildModeActive = enabled;
        Visibility visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        GrowTopButton.Visibility = visibility;
        GrowBottomButton.Visibility = visibility;
        GrowLeftButton.Visibility = visibility;
        GrowRightButton.Visibility = visibility;

        if (enabled)
        {
            PreviewMouseMove += OnBuildModeMouseMove;
            CancelAutoHideTimer();
            FadeTo(1.0);
        }
        else
        {
            PreviewMouseMove -= OnBuildModeMouseMove;
            shiftHeld = false;
            UpdateEdgeButtonLabels();
            ScheduleAutoHide(InitialVisibleDuration);
        }
    }

    private void OnBuildModeMouseMove(object sender, MouseEventArgs e)
    {
        bool pressed = IsShiftPressed();
        if (pressed != shiftHeld)
        {
            shiftHeld = pressed;
            UpdateEdgeButtonLabels();
        }
    }

    private void UpdateEdgeButtonLabels()
    {
        string label = shiftHeld ? "-" : "+";
        GrowTopButton.Content = label;
        GrowBottomButton.Content = label;
        GrowLeftButton.Content = label;
        GrowRightButton.Content = label;
    }

    private static bool IsShiftPressed() => (GetAsyncKeyState(VkShift) & 0x8000) != 0;

    private void OnZoneMouseEnter(object sender, MouseEventArgs e) => KeepVisible();

    private void KeepVisible()
    {
        FadeTo(1.0);
        ScheduleAutoHide(HoverVisibleDuration);
    }

    private void ScheduleAutoHide(TimeSpan delay)
    {
        CancelAutoHideTimer();
        if (buildModeActive)
        {
            return;
        }

        autoHideTimer = new DispatcherTimer { Interval = delay };
        autoHideTimer.Tick += OnAutoHideTick;
        autoHideTimer.Start();
    }

    private void OnAutoHideTick(object? sender, EventArgs e)
    {
        CancelAutoHideTimer();
        FadeTo(0.0);
    }

    private void CancelAutoHideTimer()
    {
        if (autoHideTimer is not null)
        {
            autoHideTimer.Tick -= OnAutoHideTick;
            autoHideTimer.Stop();
            autoHideTimer = null;
        }
    }

    private void FadeTo(double target)
    {
        // animate the inner content only; the UserControl itself must stay hit-testable so hover can wake it again
        DoubleAnimation animation = new(target, FadeDuration);
        VisualContent.BeginAnimation(OpacityProperty, animation);
    }

    private void OnGrowTopClick(object sender, RoutedEventArgs e)
    {
        if (shiftHeld)
        {
            ShrinkTop();
        }
        else
        {
            GrowTop();
        }
    }

    private void OnGrowBottomClick(object sender, RoutedEventArgs e)
    {
        if (shiftHeld)
        {
            ShrinkBottom();
        }
        else
        {
            GrowBottom();
        }
    }

    private void OnGrowLeftClick(object sender, RoutedEventArgs e)
    {
        if (shiftHeld)
        {
            ShrinkLeft();
        }
        else
        {
            GrowLeft();
        }
    }

    private void OnGrowRightClick(object sender, RoutedEventArgs e)
    {
        if (shiftHeld)
        {
            ShrinkRight();
        }
        else
        {
            GrowRight();
        }
    }

    private void GrowTop()
    {
        if (Model.GridRow > 1)
        {
            Model.GridRow--;
            Model.GridRowSpan++;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShrinkTop()
    {
        if (Model.GridRowSpan > MinSpan)
        {
            Model.GridRow++;
            Model.GridRowSpan--;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void GrowBottom()
    {
        if (Model.GridRow + Model.GridRowSpan <= GridRows)
        {
            Model.GridRowSpan++;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShrinkBottom()
    {
        if (Model.GridRowSpan > MinSpan)
        {
            Model.GridRowSpan--;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void GrowLeft()
    {
        if (Model.GridColumn > 1)
        {
            Model.GridColumn--;
            Model.GridColumnSpan++;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShrinkLeft()
    {
        if (Model.GridColumnSpan > MinSpan)
        {
            Model.GridColumn++;
            Model.GridColumnSpan--;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void GrowRight()
    {
        if (Model.GridColumn + Model.GridColumnSpan <= GridColumns)
        {
            Model.GridColumnSpan++;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void ShrinkRight()
    {
        if (Model.GridColumnSpan > MinSpan)
        {
            Model.GridColumnSpan--;
            ApplyGridBounds();
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
    private const int VkShift = 0x10;

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            return;
        }
        foreach (string path in (string[])e.Data.GetData(DataFormats.FileDrop))
        {
            if (System.IO.File.Exists(path) || System.IO.Directory.Exists(path))
            {
                Model.Items.Add(new ItemModel { Path = path, DisplayName = System.IO.Path.GetFileName(path) });
            }
        }
        RefreshItems();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void OnItemPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not Button { Tag: ShellItemViewModel viewModel })
        {
            return;
        }

        e.Handled = true;
        KeepVisible();

        if (buildModeActive)
        {
            itemDragStart = e.GetPosition(null);
            itemDragCandidate = viewModel.Source;
            return;
        }

        OpenItem(viewModel.Source);
    }

    private void OnItemPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!buildModeActive || itemDragCandidate is null || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point current = e.GetPosition(null);
        if (Math.Abs(current.X - itemDragStart.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(current.Y - itemDragStart.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        ItemModel dragged = itemDragCandidate;
        itemDragCandidate = null;
        DragDrop.DoDragDrop((DependencyObject)sender, dragged, DragDropEffects.Move);
    }

    private void OnItemDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(typeof(ItemModel)) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnItemDrop(object sender, DragEventArgs e)
    {
        if (sender is not Button { Tag: ShellItemViewModel targetViewModel } || e.Data.GetData(typeof(ItemModel)) is not ItemModel dragged)
        {
            return;
        }

        e.Handled = true;
        ReorderItem(dragged, targetViewModel.Source);
    }

    private void OnItemsListDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(ItemModel)) is not ItemModel dragged)
        {
            return;
        }

        e.Handled = true;
        ReorderItem(dragged, null);
    }

    private void ReorderItem(ItemModel dragged, ItemModel? target)
    {
        int sourceIndex = Model.Items.IndexOf(dragged);
        if (sourceIndex < 0 || ReferenceEquals(dragged, target))
        {
            return;
        }

        Model.Items.RemoveAt(sourceIndex);
        int targetIndex = target is null ? -1 : Model.Items.IndexOf(target);
        Model.Items.Insert(targetIndex < 0 ? Model.Items.Count : targetIndex, dragged);
        RefreshItems();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshItems() => ItemsList.ItemsSource = Model.Items.Select(ShellItemViewModel.Create).ToList();

    private static void OpenItem(ItemModel item)
    {
        if (string.IsNullOrWhiteSpace(item.Path))
        {
            return;
        }

        bool isFolder = System.IO.Directory.Exists(item.Path);
        try
        {
            Process.Start(new ProcessStartInfo(item.Path)
            {
                UseShellExecute = true,
                // apps need their own folder as the working directory to find local resources; folders just open in Explorer regardless
                WorkingDirectory = isFolder ? item.Path : System.IO.Path.GetDirectoryName(item.Path) ?? string.Empty
            });
        }
        catch (InvalidOperationException) { }
        catch (System.ComponentModel.Win32Exception) { }
        catch (System.IO.FileNotFoundException) { }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);

    private sealed class ShellItemViewModel
    {
        public required ItemModel Source { get; init; }
        public required string DisplayName { get; init; }
        public required ImageSource Icon { get; init; }
        public static ShellItemViewModel Create(ItemModel item) => new() { Source = item, DisplayName = item.DisplayName, Icon = ShellIconProvider.GetIcon(item.Path) };
    }
}
