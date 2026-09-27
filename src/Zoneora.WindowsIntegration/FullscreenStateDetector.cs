using System.Runtime.InteropServices;
using Zoneora.Services;

namespace Zoneora.WindowsIntegration;

public sealed class FullscreenStateDetector : IFullscreenStateDetector
{
    public bool IsFullscreenApplicationActive() => QueryState() == UserNotificationState.RunningD3dFullScreen;

    public bool IsPresentationActive() => QueryState() == UserNotificationState.PresentationMode;

    private static UserNotificationState QueryState()
    {
        return SHQueryUserNotificationState(out UserNotificationState state) == 0
            ? state
            : UserNotificationState.AcceptsNotifications;
    }

    [DllImport("shell32.dll")]
    private static extern int SHQueryUserNotificationState(out UserNotificationState state);

    private enum UserNotificationState
    {
        AcceptsNotifications = 5,
        RunningD3dFullScreen = 6,
        PresentationMode = 7
    }
}
