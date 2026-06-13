using System.IO;

namespace magazyn.Config;

public static class AppConfig
{
    public static string ApiUrl { get; private set; } = string.Empty;
    public static string ApiKey { get; private set; } = string.Empty;

    public static void Load(string? configPath = null)
    {
        var path = configPath ?? Path.Combine(AppContext.BaseDirectory, "config.ini");

        if (!File.Exists(path))
            throw new FileNotFoundException($"Nie znaleziono pliku konfiguracyjnego: {path}");

        foreach (var rawLine in File.ReadAllLines(path))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('[') || !line.Contains('='))
                continue;

            var separator = line.IndexOf('=');
            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            if (key.Equals("ApiUrl", StringComparison.OrdinalIgnoreCase))
                ApiUrl = value;
            else if (key.Equals("ApiKey", StringComparison.OrdinalIgnoreCase))
                ApiKey = value;
        }

        if (!Uri.TryCreate(ApiUrl, UriKind.Absolute, out _))
            throw new InvalidOperationException("Niepoprawny adres ApiUrl w pliku config.ini.");
    }
}
