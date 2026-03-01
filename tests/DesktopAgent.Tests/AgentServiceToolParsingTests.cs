using DesktopAgent.Services;
using DesktopAgent.Services.Tools;
using System.Reflection;
using System.Text.Json;

namespace DesktopAgent.Tests;

public class AgentServiceToolParsingTests
{
    [Fact]
    public void TryParseToolCall_ParsesJsonToolCall()
    {
        var service = CreateService();

        var (success, toolName, args) = InvokeTryParseToolCall(
            service,
            """{"tool":"create_directory","args":{"path":"proje"}}""");

        Assert.True(success);
        Assert.Equal("create_directory", toolName);
        Assert.True(args.TryGetProperty("path", out var path));
        Assert.Equal("proje", path.GetString());
    }

    [Fact]
    public void TryParseToolCall_ParsesFunctionStyleToolCall()
    {
        var service = CreateService();

        var (success, toolName, args) = InvokeTryParseToolCall(
            service,
            """[create_directory(path="proje")]""");

        Assert.True(success);
        Assert.Equal("create_directory", toolName);
        Assert.True(args.TryGetProperty("path", out var path));
        Assert.Equal("proje", path.GetString());
    }

    private static AgentService CreateService()
    {
        var ollama = new OllamaClient("http://localhost:11434");
        var tools = new ITool[]
        {
            new CreateDirectoryTool(),
            new ListFilesTool(),
            new ReadFileTool(),
            new WriteFileTool(),
            new EditFileTool(),
            new RunTerminalTool(),
            new SearchInFilesTool()
        };
        var registry = new ToolRegistry(tools);
        return new AgentService(ollama, registry);
    }

    private static (bool success, string toolName, JsonElement args) InvokeTryParseToolCall(AgentService service, string input)
    {
        var method = typeof(AgentService).GetMethod(
            "TryParseToolCall",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var invokeArgs = new object[] { input, string.Empty, default(JsonElement) };
        var success = (bool)(method!.Invoke(service, invokeArgs) ?? false);
        var toolName = (string)invokeArgs[1];
        var args = (JsonElement)invokeArgs[2];

        return (success, toolName, args);
    }
}
