using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using Zoneora.Core;

namespace Zoneora.UI;

public partial class DesktopGridWindow : Window
{
    private const int GridColumns = 12;
    private const int GridRows = 8;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpNoMove = 0x0002;
    private const uint SwpNoSize = 0x0001;
    private static readonly nint HwndBottom = new(1);
    private readonly List<ZoneControl> controls = [];
    private bool gridVisible;
    private bool buildModeEnabled;

    public DesktopGridWindow(IEnumerable<ZoneModel> zones)
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SizeChanged += OnSizeChanged;
        foreach (ZoneModel zone in zones)
        {
            AddZone(zone);
        }
    }

    public event EventHandler? Changed;
    public IReadOnlyList<ZoneModel> Zones => controls.Select(control => control.Model).ToList();

    public void AddZone(ZoneModel model)
    {
        ZoneControl control = new(model, GridCanvas);
        control.Changed += OnZoneChanged;
        control.CloseRequested += OnZoneCloseRequested;
        control.SetBuildMode(buildModeEnabled);
        controls.Add(control);
        GridCanvas.Children.Add(control);
        control.ApplyGridBounds();
    }

    public void SetGridVisible(bool visible)
    {
        gridVisible = visible;
        if (IsLoaded)
        {
            DrawGrid();
        }
    }

    public void SetBuildMode(bool enabled)
    {
        buildModeEnabled = enabled;
        foreach (ZoneControl control in controls)
        {
            control.SetBuildMode(enabled);
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Left = SystemParameters.WorkArea.Left;
        Top = SystemParameters.WorkArea.Top;
        Width = SystemParameters.WorkArea.Width;
        Height = SystemParameters.WorkArea.Height;
        DrawGrid();
        nint handle = new WindowInteropHelper(this).Handle;
        SetWindowPos(handle, HwndBottom, 0, 0, 0, 0, SwpNoMove | SwpNoSize | SwpNoActivate);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (IsLoaded)
        {
            DrawGrid();
            foreach (ZoneControl control in controls)
            {
                control.ApplyGridBounds();
            }
        }
    }

    private void DrawGrid()
    {
        GridCanvas.Children.OfType<Line>().ToList().ForEach(line => GridCanvas.Children.Remove(line));
        if (!gridVisible)
        {
            return;
        }

        double cellWidth = ActualWidth / GridColumns;
        double cellHeight = ActualHeight / GridRows;
        for (int column = 1; column < GridColumns; column++)
        {
            GridCanvas.Children.Insert(0, new Line { X1 = column * cellWidth, X2 = column * cellWidth, Y2 = ActualHeight, Stroke = new SolidColorBrush(Color.FromArgb(35, 184, 243, 107)), StrokeThickness = 1, IsHitTestVisible = false });
        }
        for (int row = 1; row < GridRows; row++)
        {
            GridCanvas.Children.Insert(0, new Line { Y1 = row * cellHeight, X2 = ActualWidth, Y2 = row * cellHeight, Stroke = new SolidColorBrush(Color.FromArgb(35, 184, 243, 107)), StrokeThickness = 1, IsHitTestVisible = false });
        }
    }

    private void OnZoneChanged(object? sender, EventArgs e) => Changed?.Invoke(this, EventArgs.Empty);

    private void OnZoneCloseRequested(object? sender, EventArgs e)
    {
        if (sender is ZoneControl control)
        {
            controls.Remove(control);
            GridCanvas.Children.Remove(control);
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e) => e.Handled = true;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(nint window, nint insertAfter, int x, int y, int width, int height, uint flags);
}
