using System.Text.Json;

namespace DesktopAgent.Services.Tools;

public class ListToolsTool : ITool
{
    private readonly Func<IReadOnlyCollection<string>> _getToolNames;

    public ListToolsTool(Func<IReadOnlyCollection<string>> getToolNames)
    {
        _getToolNames = getToolNames ?? throw new ArgumentNullException(nameof(getToolNames));
    }

    public string Name => "list_tools";

    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            var toolNames = _getToolNames()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var output = toolNames.Length == 0
                ? "(bos)"
                : string.Join(Environment.NewLine, toolNames);

            return Task.FromResult(new ToolResult(true, output));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(false, string.Empty, $"Tool listesi alinamadi: {ex.Message}"));
        }
    }
}
