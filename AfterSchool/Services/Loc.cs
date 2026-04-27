using System.Text.Json;

namespace AfterSchool.Services;

/// <summary>
/// Localisation service.
///
/// HOW TO ADD TRANSLATIONS FOR A NEW FEATURE
/// ------------------------------------------
/// 1. Pick a dot-separated key, e.g. "attendance.btn.mark".
/// 2. Add the key + English text to  Localisation/en.json.
/// 3. Add the key + Romanian text to Localisation/ro.json.
/// 4. Call Loc.T("attendance.btn.mark") anywhere in your UI code.
///
/// KEY NAMING CONVENTION
/// ----------------------
///   nav.<page>           – sidebar nav label
///   nav.<page>.title     – top-bar title for that page
///   common.<action>      – generic labels used across many screens
///   <screen>.<element>   – screen-specific text, e.g. "enrollment.search"
/// </summary>
public static class Loc
{
    /// <summary>Raised on the UI thread after the language has changed.</summary>
    public static event Action? LanguageChanged;

    private static Dictionary<string, string> _strings = new();
    private static string _current = "en";

    public static string Current => _current;

    /// <summary>Call once at startup with the persisted language code.</summary>
    public static void Init(string language)
    {
        _current = language;
        Load();
    }

    /// <summary>Switch language, persist the choice, and notify all subscribers.</summary>
    public static void SetLanguage(string language)
    {
        if (_current == language) return;
        _current = language;
        Load();
        AppSettings.SetLanguage(language);
        LanguageChanged?.Invoke();
    }

    /// <summary>
    /// Returns the translation for <paramref name="key"/> in the current language.
    /// Falls back to the key itself so missing translations are visible but never crash.
    /// </summary>
    public static string T(string key) =>
        _strings.TryGetValue(key, out var v) ? v : key;

    private static void Load()
    {
        var path = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Localisation", $"{_current}.json");
        try
        {
            _strings = File.Exists(path)
                ? JsonSerializer.Deserialize<Dictionary<string, string>>(
                    File.ReadAllText(path)) ?? new()
                : new();
        }
        catch { _strings = new(); }
    }
}
