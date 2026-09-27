# Zoneora

Zoneora is a lightweight Windows 11 desktop organization tool built with WPF and .NET 8. It is being developed to organize files, folders, shortcuts, and applications into configurable desktop zones without constantly redrawing or scanning the desktop.

## Current Status

The current application opens one borderless Zoneora control surface and one desktop grid host. Zones are Zoneora controls inside that grid, not separate user-facing Windows windows. Zones are restored between launches and remain below normal application windows so they behave like desktop organization surfaces. Desktop and Settings are views inside the same control surface, not separate settings dialogs.

### Implemented

- .NET 8 WPF solution structure
- Initial Zoneora desktop UI
- Desktop-resident control surface with Desktop and Settings views
- Customization view with lilac, blue, and rose zone colors
- Custom hex zone colors and rounded or square zone corners
- A 12 x 8 desktop grid for Zoneora zones
- Create, move, resize, rename, and close desktop zones
- Restore zone positions and sizes between launches
- Drag files and folders into zones with their Windows shell icons
- Click zone items to open them with Windows
- Zone, item, profile, and OLED Mode settings models
- JSON configuration persistence under `%LocalAppData%\Zoneora\settings.json`
- Graceful fallback when the configuration is missing or invalid
- Atomic configuration writes using a temporary file
- Tested OLED Mode state machine with fade-in, fade-out, inactivity timeout, wake conditions, and fullscreen/presentation suppression
- Event-driven Windows input monitor and desktop icon visibility adapter

### Not Yet Available in the UI

- Multiple desktop overlay windows
- Profiles and profile switching
- Live OLED overlay activation and automatic desktop icon hiding
- Startup, tray controls, multi-monitor layout restoration, and Explorer recovery

## Requirements

- Windows 10 or Windows 11
- .NET 8 SDK to build from source
- .NET 8 Desktop Runtime to run a published build
- 64-bit Windows is recommended

## Run Zoneora

From the repository root:

```powershell
dotnet run --project src/Zoneora.App/Zoneora.App.csproj
```

For a double-clickable Windows executable, use the published file at `publish\Zoneora-win-x64\Zoneora.exe`. It is self-contained and does not require the .NET runtime to be installed separately.

The current build opens the Zoneora control surface near the lower-right of the desktop and restores the desktop grid. Use the **Desktop** view and click **Create zone** to place a zone in the grid. Double-click a zone's title to rename it; otherwise dragging anywhere inside a zone (including its top edge) moves it to another grid position. Enable **Build Mode** to show a `+` handle on each edge of a zone; clicking a handle grows the zone by one grid unit in that direction. Hold **Shift** while Build Mode is active to turn the handles into `-` handles that shrink the zone edge by edge instead. Drop files or folders into a zone to add them with their Windows shell icons. The grid lines themselves are only visible when **Show desktop grid** is enabled in Settings.

Zones fade out a few seconds after they appear and stay hidden until the mouse moves over them, then fade back in and remain visible for about 3 minutes before fading out again. Build Mode keeps all zones visible while it is active.

Click an item to open it with its normal Windows application. Close a zone with the `x` button in its header. Click the `–` button in the control surface header to minimize Zoneora to the Windows hidden-icons tray area; double-click the tray icon or use its context menu to reopen it. Zone positions, sizes, names, and dropped item paths are saved automatically.

Select **Settings** in the same control surface to hide the standard Windows desktop icons immediately or enable Zoneora at Windows startup. Select **Customization** to change the zone color and choose rounded or square corners. Settings are saved automatically.

## Build and Test

Build the complete solution:

```powershell
dotnet build Zoneora.sln
```

Run all tests:

```powershell
dotnet test Zoneora.sln
```

The tests currently cover settings round-tripping, invalid configuration recovery, OLED Mode wake/fade transitions, and fullscreen suppression.

## Configuration

Zoneora stores its configuration at:

```text
%LocalAppData%\Zoneora\settings.json
```

The file is created when settings are saved. If it is missing or contains invalid JSON, Zoneora starts with default settings instead of failing. Back up this file while Zoneora is closed if you want to preserve a configuration manually.

## OLED Mode

OLED Mode is currently implemented as a tested service foundation, but it is not connected to the visible desktop overlay yet.

The implemented service supports:

- Mouse, keyboard, click, and screen-edge wake conditions
- Configurable fade-in and fade-out durations
- Configurable inactivity timeout, including no timeout
- Fullscreen and presentation suppression checks
- Event-driven activity handling
- A single inactivity timer that is stopped when the UI is hidden

Once the overlay window is connected, the intended workflow will be:

1. Enable OLED Mode.
2. Zoneora hides its zones after the configured idle timeout.
3. Mouse or keyboard activity wakes the interface.
4. Zoneora fades the interface back in.
5. Further inactivity hides the interface again while leaving the wallpaper untouched.

## Development Layout

```text
src/
	Zoneora.Core/                 Models and JSON persistence
	Zoneora.Services/             OLED Mode and application services
	Zoneora.WindowsIntegration/   Windows hooks and shell integration
	Zoneora.UI/                   WPF user interface
	Zoneora.App/                  Executable entry point
tests/
	Zoneora.Core.Tests/
	Zoneora.Services.Tests/
```

## Planned Features

- Zone renaming and item removal controls
- Application and shortcut drag and drop refinements
- Desktop icon management and transparent overlay windows
- Multiple monitors and DPI-aware layouts
- Profiles and quick hide/show controls
- OLED Mode settings UI and overlay integration
- Startup integration, diagnostics, accessibility, installer, and documentation polish

## Performance Direction

Zoneora is designed to remain event-driven while idle. The implementation avoids continuous desktop polling, wallpaper rendering, and unnecessary animation. Performance numbers will only be documented after repeatable measurements on a stated test system.

## License

See [LICENSE](LICENSE).
