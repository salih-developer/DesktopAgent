namespace DesktopAgent.Utils;

public static class WorkspaceContext
{
    private static string _currentPath = @"D:\\AI\\llmtest";

    public static string CurrentPath => _currentPath;

    public static void Set(string path)
    {
        var normalized = AppSettingsStore.NormalizeWorkspacePath(path);
        _currentPath = normalized;
    }
}
