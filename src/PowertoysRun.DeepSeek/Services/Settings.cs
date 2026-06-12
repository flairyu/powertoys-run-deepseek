using System.Text.Json;

namespace PowertoysRun.DeepSeek.Services;

public class PluginSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "deepseek-chat";

    private string? _settingsFilePath;

    public void SetSettingsPath(string path)
    {
        _settingsFilePath = Path.Combine(path, "settings.json");
    }

    public static PluginSettings Load(string settingsPath)
    {
        var filePath = Path.Combine(settingsPath, "settings.json");
        if (!File.Exists(filePath))
        {
            var defaultSettings = new PluginSettings();
            defaultSettings.SetSettingsPath(settingsPath);
            return defaultSettings;
        }

        try
        {
            var json = File.ReadAllText(filePath);
            var settings = JsonSerializer.Deserialize<PluginSettings>(json, JsonOptions) ?? new PluginSettings();
            settings.SetSettingsPath(settingsPath);
            return settings;
        }
        catch
        {
            return new PluginSettings();
        }
    }

    public void Save()
    {
        if (string.IsNullOrEmpty(_settingsFilePath)) return;

        try
        {
            var json = JsonSerializer.Serialize(this, JsonOptions);
            File.WriteAllText(_settingsFilePath, json);
        }
        catch { }
    }
}
