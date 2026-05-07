namespace AfterSchool.Services;

public static class EnvConfig
{
    private static readonly Dictionary<string, string> _values = new();

    public static void Load()
    {
        var exeDir = AppDomain.CurrentDomain.BaseDirectory;
        var path   = Path.Combine(exeDir, ".env.local");
        if (!File.Exists(path)) return;

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
            var eq = line.IndexOf('=');
            if (eq < 1) continue;
            var key = line[..eq].Trim();
            var val = line[(eq + 1)..].Trim();
            _values[key] = val;
        }
    }

    public static string Get(string key) =>
        _values.TryGetValue(key, out var v) ? v : "";
}
