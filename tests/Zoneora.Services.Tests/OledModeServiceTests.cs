using Zoneora.Core;
using Zoneora.Services;

namespace Zoneora.Services.Tests;

public sealed class OledModeServiceTests
{
    [Fact]
    public async Task MouseActivityFadesInFromHidden()
    {
        TestActivityMonitor activity = new();
        TestVisibilityController visibility = new();
        using OledModeService service = CreateService(activity, visibility);
        service.Start();

        activity.Raise(ActivityKind.MouseMove);
        await visibility.InCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(OledModeState.Visible, service.State);
        Assert.Equal(TimeSpan.FromMilliseconds(500), visibility.LastInDuration);
    }

    [Fact]
    public async Task IdleTimeoutFadesOutAndStopsAtHidden()
    {
        TestActivityMonitor activity = new();
        TestVisibilityController visibility = new();
        using OledModeService service = CreateService(activity, visibility);
        service.Start();
        activity.Raise(ActivityKind.MouseMove);
        await visibility.InCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        service.TriggerIdleTimeout();
        await visibility.OutCompleted.Task.WaitAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(OledModeState.Hidden, service.State);
    }

    [Fact]
    public void FullscreenSuppressionDoesNotWakeHiddenUi()
    {
        TestActivityMonitor activity = new();
        TestVisibilityController visibility = new();
        using OledModeService service = new(
            new OledModeSettings { Enabled = true, DisableInFullscreenApps = true },
            activity,
            new TestFullscreenDetector { Fullscreen = true },
            visibility);
        service.Start();

        activity.Raise(ActivityKind.MouseMove);

        Assert.Equal(OledModeState.Hidden, service.State);
        Assert.False(visibility.InCompleted.Task.IsCompleted);
    }

    private static OledModeService CreateService(TestActivityMonitor activity, TestVisibilityController visibility) => new(
        new OledModeSettings { Enabled = true, IdleTimeoutMinutes = 10 },
        activity,
        new TestFullscreenDetector(),
        visibility);

    private sealed class TestActivityMonitor : IActivityMonitor
    {
        public event EventHandler<ActivityKind>? ActivityDetected;
        public void Raise(ActivityKind activity) => ActivityDetected?.Invoke(this, activity);
    }

    private sealed class TestFullscreenDetector : IFullscreenStateDetector
    {
        public bool Fullscreen { get; init; }
        public bool IsFullscreenApplicationActive() => Fullscreen;
        public bool IsPresentationActive() => false;
    }

    private sealed class TestVisibilityController : IOledVisibilityController
    {
        public TaskCompletionSource InCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource OutCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TimeSpan LastInDuration { get; private set; }
        public TimeSpan LastOutDuration { get; private set; }
        public Task FadeInAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            LastInDuration = duration;
            InCompleted.TrySetResult();
            return Task.CompletedTask;
        }
        public Task FadeOutAsync(TimeSpan duration, CancellationToken cancellationToken)
        {
            LastOutDuration = duration;
            OutCompleted.TrySetResult();
            return Task.CompletedTask;
        }
    }
}
