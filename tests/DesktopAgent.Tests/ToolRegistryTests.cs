using DesktopAgent.Services.Tools;
using System.Text.Json;

namespace DesktopAgent.Tests;

public class ToolRegistryTests
{
    [Fact]
    public async Task ExecuteAsync_ReturnsError_ForUnknownTool()
    {
        var registry = new ToolRegistry(Array.Empty<ITool>());
        var args = TestInfrastructure.Args(new { });

        var result = await registry.ExecuteAsync("unknown_tool", args, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Tool bulunamadi", result.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_ExecutesKnownTool()
    {
        var tool = new FakeTool("fake_ok", new ToolResult(true, "ok"));
        var registry = new ToolRegistry(new[] { tool });
        var args = TestInfrastructure.Args(new { id = 1 });

        var result = await registry.ExecuteAsync("fake_ok", args, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("ok", result.Output);
    }

    [Fact]
    public async Task ExecuteAsync_ConvertsThrownExceptionToErrorResult()
    {
        var tool = new ThrowingTool("throw_tool");
        var registry = new ToolRegistry(new ITool[] { tool });
        var args = TestInfrastructure.Args(new { });

        var result = await registry.ExecuteAsync("throw_tool", args, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("boom", result.Error);
    }

    private sealed class FakeTool : ITool
    {
        private readonly ToolResult _result;

        public FakeTool(string name, ToolResult result)
        {
            Name = name;
            _result = result;
        }

        public string Name { get; }

        public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class ThrowingTool : ITool
    {
        public ThrowingTool(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
        {
            throw new InvalidOperationException("boom");
        }
    }
}
