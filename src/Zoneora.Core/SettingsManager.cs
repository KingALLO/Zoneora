using System.Text.Json;

namespace Zoneora.Core;

public sealed class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly SemaphoreSlim saveGate = new(1, 1);

    public SettingsManager(string? filePath = null)
    {
        FilePath = filePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Zoneora",
            "settings.json");
    }

    public string FilePath { get; }

    public async Task<AppSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(FilePath))
            {
                return new AppSettings();
            }

            await using FileStream stream = File.OpenRead(FilePath);
            return await JsonSerializer.DeserializeAsync<AppSettings>(stream, JsonOptions, cancellationToken)
                ?? new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        catch (JsonException)
        {
            return new AppSettings();
        }
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken cancellationToken = default)
    {
        await saveGate.WaitAsync(cancellationToken);
        try
        {
            string? directory = Path.GetDirectoryName(FilePath);
            if (directory is not null)
            {
                Directory.CreateDirectory(directory);
            }

            string temporaryPath = $"{FilePath}.{Guid.NewGuid():N}.tmp";
            await using (FileStream stream = File.Create(temporaryPath))
            {
                await JsonSerializer.SerializeAsync(stream, settings, JsonOptions, cancellationToken);
            }

            File.Move(temporaryPath, FilePath, overwrite: true);
        }
        finally
        {
            saveGate.Release();
        }
    }
}
