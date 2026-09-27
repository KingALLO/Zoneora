using Zoneora.Core;

namespace Zoneora.Services;

public enum OledModeState
{
    Disabled,
    Hidden,
    FadingIn,
    Visible,
    FadingOut
}

public enum ActivityKind
{
    MouseMove,
    KeyboardInput,
    MouseClick,
    ScreenEdge
}

public interface IActivityMonitor
{
    event EventHandler<ActivityKind>? ActivityDetected;
}

public interface IFullscreenStateDetector
{
    bool IsFullscreenApplicationActive();
    bool IsPresentationActive();
}

public interface IOledVisibilityController
{
    Task FadeInAsync(TimeSpan duration, CancellationToken cancellationToken);
    Task FadeOutAsync(TimeSpan duration, CancellationToken cancellationToken);
}

public sealed class OledModeService : IDisposable
{
    private readonly IActivityMonitor activityMonitor;
    private readonly IFullscreenStateDetector fullscreenStateDetector;
    private readonly IOledVisibilityController visibilityController;
    private CancellationTokenSource? transitionCancellation;
    private Timer? idleTimer;
    private bool disposed;

    public OledModeService(
        OledModeSettings settings,
        IActivityMonitor activityMonitor,
        IFullscreenStateDetector fullscreenStateDetector,
        IOledVisibilityController visibilityController)
    {
        Settings = settings;
        this.activityMonitor = activityMonitor;
        this.fullscreenStateDetector = fullscreenStateDetector;
        this.visibilityController = visibilityController;
        State = settings.Enabled ? OledModeState.Hidden : OledModeState.Disabled;
    }

    public OledModeSettings Settings { get; }
    public OledModeState State { get; private set; }
    public event EventHandler<OledModeState>? StateChanged;

    public void Start()
    {
        ThrowIfDisposed();
        activityMonitor.ActivityDetected += OnActivityDetected;
        if (Settings.Enabled)
        {
            State = OledModeState.Hidden;
            RaiseStateChanged();
        }
    }

    public void HandleActivity(ActivityKind activity)
    {
        ThrowIfDisposed();
        if (!ShouldWake(activity) || State is OledModeState.Disabled or OledModeState.FadingIn)
        {
            return;
        }

        if (IsSuppressed())
        {
            return;
        }

        RestartIdleTimer();
        if (State is OledModeState.Hidden or OledModeState.FadingOut)
        {
            _ = FadeInAsync();
        }
    }

    public void TriggerIdleTimeout()
    {
        ThrowIfDisposed();
        if (State != OledModeState.Visible || Settings.IdleTimeoutMinutes is null || IsSuppressed())
        {
            return;
        }

        _ = FadeOutAsync();
    }

    public void SetEnabled(bool enabled)
    {
        ThrowIfDisposed();
        Settings.Enabled = enabled;
        CancelIdleTimer();
        transitionCancellation?.Cancel();
        State = enabled ? OledModeState.Hidden : OledModeState.Disabled;
        RaiseStateChanged();
    }

    private async Task FadeInAsync()
    {
        transitionCancellation?.Cancel();
        transitionCancellation = new CancellationTokenSource();
        State = OledModeState.FadingIn;
        RaiseStateChanged();
        try
        {
            await visibilityController.FadeInAsync(TimeSpan.FromMilliseconds(Settings.FadeInMilliseconds), transitionCancellation.Token);
            State = OledModeState.Visible;
            RaiseStateChanged();
            RestartIdleTimer();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task FadeOutAsync()
    {
        transitionCancellation?.Cancel();
        transitionCancellation = new CancellationTokenSource();
        CancelIdleTimer();
        State = OledModeState.FadingOut;
        RaiseStateChanged();
        try
        {
            await visibilityController.FadeOutAsync(TimeSpan.FromMilliseconds(Settings.FadeOutMilliseconds), transitionCancellation.Token);
            State = OledModeState.Hidden;
            RaiseStateChanged();
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void RestartIdleTimer()
    {
        CancelIdleTimer();
        if (Settings.IdleTimeoutMinutes is int minutes && minutes > 0)
        {
            idleTimer = new Timer(_ => TriggerIdleTimeout(), null, TimeSpan.FromMinutes(minutes), Timeout.InfiniteTimeSpan);
        }
    }

    private void CancelIdleTimer()
    {
        idleTimer?.Dispose();
        idleTimer = null;
    }

    private void OnActivityDetected(object? sender, ActivityKind activity) => HandleActivity(activity);

    private bool ShouldWake(ActivityKind activity) => activity switch
    {
        ActivityKind.MouseMove => Settings.WakeOnMouseMove,
        ActivityKind.KeyboardInput => Settings.WakeOnKeyboardInput,
        ActivityKind.MouseClick => Settings.WakeOnMouseClick,
        ActivityKind.ScreenEdge => Settings.WakeOnScreenEdge,
        _ => false
    };

    private bool IsSuppressed() =>
        (Settings.DisableInFullscreenApps && fullscreenStateDetector.IsFullscreenApplicationActive()) ||
        (Settings.DisableWhilePresenting && fullscreenStateDetector.IsPresentationActive());

    private void RaiseStateChanged() => StateChanged?.Invoke(this, State);
    private void ThrowIfDisposed() { ObjectDisposedException.ThrowIf(disposed, this); }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        activityMonitor.ActivityDetected -= OnActivityDetected;
        CancelIdleTimer();
        transitionCancellation?.Cancel();
        transitionCancellation?.Dispose();
    }
}
