using Zoneora.Core;

namespace Zoneora.Core.Tests;

public sealed class SettingsManagerTests
{
    [Fact]
    public async Task SaveAndLoadRoundTripsOledSettings()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ZoneoraTests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "settings.json");
        try
        {
            SettingsManager manager = new(path);
            AppSettings settings = new()
            {
                OledMode = new OledModeSettings { Enabled = true, IdleTimeoutMinutes = null, FadeOutMilliseconds = 3_000 }
            };

            await manager.SaveAsync(settings);
            AppSettings loaded = await manager.LoadAsync();

            Assert.True(loaded.OledMode.Enabled);
            Assert.Null(loaded.OledMode.IdleTimeoutMinutes);
            Assert.Equal(3_000, loaded.OledMode.FadeOutMilliseconds);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public async Task CorruptSettingsFallBackToDefaults()
    {
        string path = Path.Combine(Path.GetTempPath(), "ZoneoraTests", Guid.NewGuid().ToString("N"), "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, "{ invalid json");

        AppSettings loaded = await new SettingsManager(path).LoadAsync();

        Assert.False(loaded.OledMode.Enabled);
        File.Delete(path);
        Directory.Delete(Path.GetDirectoryName(path)!);
    }

    [Fact]
    public async Task ConcurrentSavesDoNotRaceOverTemporaryFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), "ZoneoraTests", Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "settings.json");
        try
        {
            SettingsManager manager = new(path);
            Task[] saves = Enumerable.Range(0, 20)
                .Select(index => manager.SaveAsync(new AppSettings
                {
                    Zones = [new ZoneModel { Name = $"Zone {index}" }]
                }))
                .ToArray();

            await Task.WhenAll(saves);

            AppSettings loaded = await manager.LoadAsync();
            Assert.Single(loaded.Zones);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
