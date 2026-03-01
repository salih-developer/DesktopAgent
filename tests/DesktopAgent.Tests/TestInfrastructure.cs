using DesktopAgent.Utils;
using System.Text.Json;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace DesktopAgent.Tests;

internal static class TestInfrastructure
{
    public static JsonElement Args(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }
}

internal sealed class WorkspaceScope : IDisposable
{
    private readonly string _previousPath;
    public string RootPath { get; }

    public WorkspaceScope()
    {
        _previousPath = WorkspaceContext.CurrentPath;
        RootPath = Path.Combine(Path.GetTempPath(), "DesktopAgentTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
        WorkspaceContext.Set(RootPath);
    }

    public void Dispose()
    {
        WorkspaceContext.Set(_previousPath);
        try
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
        catch
        {
            // Best-effort cleanup for temporary test folders.
        }
    }
}
