using System.Text.Json;

namespace DesktopAgent.Services.Tools;

public record ToolResult(bool Success, string Output, string? Error = null);

public interface ITool
{
    string Name { get; }
    Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct);
}

public class ToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyCollection<string> GetToolNames()
    {
        return _tools.Keys.ToArray();
    }

    public async Task<ToolResult> ExecuteAsync(string name, JsonElement args, CancellationToken ct)
    {
        if (!_tools.TryGetValue(name, out var tool))
        {
            return new ToolResult(false, string.Empty, $"Tool bulunamadi: {name}");
        }

        try
        {
            return await tool.RunAsync(args, ct);
        }
        catch (Exception ex)
        {
            return new ToolResult(false, string.Empty, ex.Message);
        }
    }
}
