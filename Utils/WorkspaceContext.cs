namespace DesktopAgent.Utils;

public static class WorkspaceContext
{
    private static string _currentPath = @"D:\\AI\\llmtest";

    public static string CurrentPath => _currentPath;

    public static void Set(string path)
    {
        var normalized = AppSettingsStore.NormalizeWorkspacePath(path);
        try
        {
            Directory.CreateDirectory(normalized);
            _currentPath = normalized;
        }
        catch
        {
            var fallback = AppSettingsStore.NormalizeWorkspacePath(Environment.CurrentDirectory);
            Directory.CreateDirectory(fallback);
            _currentPath = fallback;
        }
    }
}
