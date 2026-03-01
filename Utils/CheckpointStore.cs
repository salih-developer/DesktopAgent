using System.Text.Json;

namespace DesktopAgent.Utils;

public class CheckpointMessage
{
    public string Role    { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class CheckpointData
{
    public string   TaskId    { get; set; } = string.Empty;
    public string   Task      { get; set; } = string.Empty;
    public string   Model     { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int      StepCount { get; set; }
    public List<CheckpointMessage> Messages { get; set; } = new();
}

public static class CheckpointStore
{
    private static readonly string CheckpointPath = Path.Combine(
        AppContext.BaseDirectory, "checkpoints", "active.json");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented        = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static bool HasCheckpoint => File.Exists(CheckpointPath);

    public static CheckpointData? Load()
    {
        if (!File.Exists(CheckpointPath)) return null;
        try
        {
            var json = File.ReadAllText(CheckpointPath);
            return JsonSerializer.Deserialize<CheckpointData>(json, JsonOpts);
        }
        catch
        {
            return null;
        }
    }

    public static void Save(CheckpointData data)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(CheckpointPath)!);
        data.UpdatedAt = DateTime.Now;
        File.WriteAllText(CheckpointPath, JsonSerializer.Serialize(data, JsonOpts));
    }

    public static void Delete()
    {
        try { if (File.Exists(CheckpointPath)) File.Delete(CheckpointPath); }
        catch { /* ignore */ }
    }
}
