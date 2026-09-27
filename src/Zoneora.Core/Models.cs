namespace Zoneora.Core;

public sealed class ZoneModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New zone";
    public double Left { get; set; } = 48;
    public double Top { get; set; } = 48;
    public double Width { get; set; } = 360;
    public double Height { get; set; } = 240;
    public int GridColumn { get; set; } = 1;
    public int GridRow { get; set; } = 1;
    public int GridColumnSpan { get; set; } = 4;
    public int GridRowSpan { get; set; } = 3;
    public bool IsMinimized { get; set; }
    public List<ItemModel> Items { get; set; } = [];
}

public sealed class ItemModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Path { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
}

public sealed class ProfileModel
{
    public string Name { get; set; } = "Default";
    public List<ZoneModel> Zones { get; set; } = [];
}

public sealed class OledModeSettings
{
    public bool Enabled { get; set; }
    public int FadeInMilliseconds { get; set; } = 500;
    public int FadeOutMilliseconds { get; set; } = 2_000;
    public int? IdleTimeoutMinutes { get; set; } = 10;
    public bool WakeOnMouseMove { get; set; } = true;
    public bool WakeOnKeyboardInput { get; set; } = true;
    public bool WakeOnMouseClick { get; set; } = true;
    public bool WakeOnScreenEdge { get; set; }
    public int ScreenEdgeMarginPixels { get; set; } = 4;
    public bool HideZones { get; set; } = true;
    public bool HideDesktopIcons { get; set; } = true;
    public bool KeepTrayControlsAvailable { get; set; } = true;
    public bool DisableInFullscreenApps { get; set; } = true;
    public bool DisableWhileGaming { get; set; }
    public bool DisableWhilePresenting { get; set; } = true;
}

public sealed class AppSettings
{
    public List<ZoneModel> Zones { get; set; } = [];
    public List<ProfileModel> Profiles { get; set; } = [];
    public OledModeSettings OledMode { get; set; } = new();
    public bool ShowGrid { get; set; }
    public bool DesktopIconsHidden { get; set; }
}
