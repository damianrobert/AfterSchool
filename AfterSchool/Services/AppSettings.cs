using System.Text.Json;

namespace AfterSchool.Services;

public static class AppSettings
{
    private static readonly string _path = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    private static SettingsData _data = new();

    public static string Language => _data.Language;

    public static void Load()
    {
        if (!File.Exists(_path)) return;
        try
        {
            _data = JsonSerializer.Deserialize<SettingsData>(
                File.ReadAllText(_path)) ?? new();
        }
        catch { _data = new(); }
    }

    public static void SetLanguage(string lang)
    {
        _data = _data with { Language = lang };
        Save();
    }

    private static void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_data)); } catch { }
    }

    private sealed record SettingsData(string Language = "en");
}
