using System.Text.Json;

namespace AfterSchool.Services;

public static class AppSettings
{
    private static readonly string _path = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    private static SettingsData _data = new();

    public static string Language => _data.Language;

    public static int SessionUserId => _data.SessionUserId;
    public static DateTime SessionExpiry =>
        DateTime.TryParse(_data.SessionExpiryUtc, null,
            System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
            ? dt
            : DateTime.MinValue;

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

    public static void SaveSession(int userId, DateTime expiryUtc)
    {
        _data = _data with
        {
            SessionUserId = userId,
            SessionExpiryUtc = expiryUtc.ToString("O")
        };
        Save();
    }

    public static void ClearSession()
    {
        _data = _data with { SessionUserId = 0, SessionExpiryUtc = "" };
        Save();
    }

    private static void Save()
    {
        try { File.WriteAllText(_path, JsonSerializer.Serialize(_data)); } catch { }
    }

    private sealed record SettingsData(
        string Language = "en",
        int SessionUserId = 0,
        string SessionExpiryUtc = "");
}
