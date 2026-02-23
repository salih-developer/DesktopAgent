using System.Text.Json;

namespace DesktopAgent.Utils;

public sealed class AppSettings
{
    public string OllamaBaseUrl { get; set; } = "http://localhost:11434";
    public string WorkspacePath { get; set; } = @"D:\\AI\\llmtest";
    public string SelectedModel { get; set; } = "";
    public string SystemPrompt { get; set; } = @"Sen guclu bir yazilim gelistirme ajanisin.c#,html,css,jquery kullanabiliyorsun
Arac cagirmak icin JSON kullan:
{""tool"":""tool_name"",""args"":{...}}
Final yanitini [YANIT] etiketi ile bitir.";
}

public static class AppSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private static string SettingsDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OllamaWin");

    private static string SettingsFilePath => Path.Combine(SettingsDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
                return new AppSettings();

            var json = File.ReadAllText(SettingsFilePath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json);
            if (settings is null)
                return new AppSettings();

            if (!TryNormalizeBaseUrl(settings.OllamaBaseUrl, out var normalized, out _))
                return new AppSettings();

            settings.OllamaBaseUrl = normalized;
            settings.WorkspacePath = NormalizeWorkspacePath(settings.WorkspacePath);
            return settings;
        }
        catch
        {
            return new AppSettings();
        }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(SettingsFilePath, json);
    }

    public static bool TryNormalizeBaseUrl(string? value, out string normalized, out string error)
    {
        normalized = string.Empty;
        error = string.Empty;

        var input = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            error = "URL boş olamaz.";
            return false;
        }

        if (!Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            error = "Geçerli bir URL girin. Örn: http://localhost:11434";
            return false;
        }

        if (uri.Scheme is not ("http" or "https"))
        {
            error = "URL http:// veya https:// ile başlamalı.";
            return false;
        }

        normalized = uri.GetLeftPart(UriPartial.Authority);
        return true;
    }

    public static string NormalizeWorkspacePath(string? value)
    {
        var input = (value ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(input))
            return Environment.CurrentDirectory;

        try
        {
            return Path.GetFullPath(input);
        }
        catch
        {
            return Environment.CurrentDirectory;
        }
    }
}
