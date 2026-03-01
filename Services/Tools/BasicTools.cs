using DesktopAgent.Utils;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DesktopAgent.Services.Tools;

public class ReadFileTool : ITool
{
    public string Name => "read_file";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        if (!args.TryGetProperty("path", out var pathProp))
            return new ToolResult(false, string.Empty, "Missing required argument: path");
        var path = pathProp.GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        if (!File.Exists(full))
            return new ToolResult(false, string.Empty, "File not found");
        var text = await File.ReadAllTextAsync(full, ct);
        return new ToolResult(true, text);
    }
}

public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        if (!args.TryGetProperty("path", out var pathProp))
            return new ToolResult(false, string.Empty, "Missing required argument: path");
        if (!args.TryGetProperty("content", out var contentProp))
            return new ToolResult(false, string.Empty, "Missing required argument: content");
        var path = pathProp.GetString() ?? string.Empty;
        var content = contentProp.GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        var directory = Path.GetDirectoryName(full);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(full, content, ct);
        return new ToolResult(true, "Written");
    }
}

public class EditFileTool : ITool
{
    public string Name => "edit_file";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        if (!args.TryGetProperty("path", out var pathProp))
            return new ToolResult(false, string.Empty, "Missing required argument: path");
        if (!args.TryGetProperty("search", out var searchProp))
            return new ToolResult(false, string.Empty, "Missing required argument: search");
        if (!args.TryGetProperty("replace", out var replaceProp))
            return new ToolResult(false, string.Empty, "Missing required argument: replace");
        var path = pathProp.GetString() ?? string.Empty;
        var search = searchProp.GetString() ?? string.Empty;
        var replace = replaceProp.GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        if (!File.Exists(full)) return new ToolResult(false, string.Empty, "File not found");
        var text = await File.ReadAllTextAsync(full, ct);

        // Normalize line endings so that \r\n files match \n search strings from the model.
        var normalizedText   = text.Replace("\r\n", "\n");
        var normalizedSearch  = search.Replace("\r\n", "\n");
        var normalizedReplace = replace.Replace("\r\n", "\n");

        if (!normalizedText.Contains(normalizedSearch))
            return new ToolResult(false, string.Empty, "Text not found");

        var result = normalizedText.Replace(normalizedSearch, normalizedReplace);
        await File.WriteAllTextAsync(full, result, ct);
        return new ToolResult(true, "Modified");
    }
}

public class ListFilesTool : ITool
{
    public string Name => "list_files";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "." : ".";
            var recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();
            var full = PathHelper.Resolve(path);
            var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (!Directory.Exists(full))
                return Task.FromResult(new ToolResult(false, string.Empty, $"Directory not found: {full}"));

            var items = Directory.EnumerateFileSystemEntries(full, "*", option).ToList();
            var output = items.Count == 0
                ? $"(empty directory: {full})"
                : string.Join(Environment.NewLine, items);

            return Task.FromResult(new ToolResult(true, output));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(false, string.Empty, ex.Message));
        }
    }
}

public class RunTerminalTool : ITool
{
    public string Name => "run_terminal";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        if (!args.TryGetProperty("command", out var commandProp))
            return new ToolResult(false, string.Empty, "Missing required argument: command");
        var command = commandProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(command))
            return new ToolResult(false, string.Empty, "Command cannot be empty.");

        var rawCwd = args.TryGetProperty("cwd", out var cwdProp) ? cwdProp.GetString() : null;
        string cwd;
        if (!string.IsNullOrWhiteSpace(rawCwd))
        {
            var resolved = PathHelper.Resolve(rawCwd);
            cwd = Directory.Exists(resolved) ? resolved : WorkspaceContext.CurrentPath;
        }
        else
        {
            cwd = WorkspaceContext.CurrentPath;
        }

        // Auto-convert blocking "dotnet run" → kill existing instance + background start.
        if (IsBlockingDotnetRun(command))
            return await AutoStartInBackgroundAsync(command, cwd, ct);

        var runResult = await ProcessRunner.RunAsync(command, cwd, TimeSpan.FromSeconds(180), ct);

        var output = runResult.StdOut;
        if (string.IsNullOrWhiteSpace(output))
        {
            output = "(no stdout)";
            if (!string.IsNullOrWhiteSpace(runResult.StdErr))
            {
                output += $"{Environment.NewLine}[stderr]{Environment.NewLine}{runResult.StdErr}";
            }
        }

        string? error = null;
        if (runResult.TimedOut)
        {
            error = $"Command timed out. ExitCode: {runResult.ExitCode}";
            if (!string.IsNullOrWhiteSpace(runResult.StdErr))
            {
                error += $"{Environment.NewLine}{runResult.StdErr}";
            }
        }
        else if (!string.IsNullOrWhiteSpace(runResult.StdErr))
        {
            error = runResult.StdErr;
        }

        if (!runResult.Success && string.IsNullOrWhiteSpace(error))
        {
            error = $"Command failed. ExitCode: {runResult.ExitCode}";
        }

        return new ToolResult(runResult.Success, output, error);
    }

    /// <summary>
    /// Returns true if the command is a bare "dotnet run" (blocking) without a background wrapper.
    /// Allows: powershell Start-Process, cmd start /B, etc.
    /// Blocks: dotnet run / dotnet run --project ... / dotnet watch run
    /// </summary>
    private static bool IsBlockingDotnetRun(string command)
    {
        var lower = command.Trim().ToLowerInvariant();

        // Allowed: already wrapped in a background launcher
        if (lower.Contains("start-process") ||
            lower.Contains("start /b")      ||
            lower.Contains("start /B"))
            return false;

        // Block: dotnet run / dotnet watch run (with or without flags)
        return System.Text.RegularExpressions.Regex.IsMatch(
            lower,
            @"\bdotnet\s+(run|watch\s+run)\b");
    }

    /// <summary>
    /// Intercepts "dotnet run [--project path]", kills any existing running instance of
    /// the same project exe, then starts the process in the background via PowerShell
    /// Start-Process (which does NOT inherit pipe handles and returns immediately).
    /// </summary>
    private static async Task<ToolResult> AutoStartInBackgroundAsync(
        string command, string cwd, CancellationToken ct)
    {
        // Resolve project directory and exe name.
        var projectDir  = ResolveProjectDir(command, cwd);
        var csprojPath  = FindCsproj(projectDir);
        var exeName     = csprojPath != null
            ? Path.GetFileNameWithoutExtension(csprojPath)
            : Path.GetFileName(projectDir);

        var steps = new System.Text.StringBuilder();

        // 1. Kill existing instance (best-effort, ignore errors).
        var killCmd  = $"taskkill /F /IM \"{exeName}.exe\" 2>nul & exit /b 0";
        var killResult = await ProcessRunner.RunAsync(killCmd, cwd, TimeSpan.FromSeconds(10), ct);
        if (!string.IsNullOrWhiteSpace(killResult.StdOut) &&
            killResult.StdOut.Contains("SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            steps.AppendLine($"Killed existing instance: {exeName}.exe");
        }

        // 2. Build the Start-Process command.
        var projectArg  = csprojPath != null ? $"run --project \\\"{csprojPath}\\\"" : "run";
        var psCommand   = $"Start-Process dotnet " +
                          $"-ArgumentList '{projectArg}' " +
                          $"-WorkingDirectory '{projectDir}' " +
                          $"-WindowStyle Hidden";
        var bgCmd = $"powershell -Command \"{psCommand}\"";

        // 3. Launch background process.
        var bgResult = await ProcessRunner.RunAsync(bgCmd, cwd, TimeSpan.FromSeconds(15), ct);
        if (!bgResult.Success)
        {
            return new ToolResult(false, steps.ToString(),
                $"Background start failed: {bgResult.StdErr}");
        }

        steps.AppendLine($"Started {exeName} in background.");
        steps.AppendLine("Wait 3 seconds, then open the browser with the URL from Properties/launchSettings.json.");
        return new ToolResult(true, steps.ToString().Trim());
    }

    /// <summary>Extracts --project path from the command or falls back to cwd.</summary>
    private static string ResolveProjectDir(string command, string cwd)
    {
        var m = System.Text.RegularExpressions.Regex.Match(
            command, @"--project\s+""?([^""]+?)""?\s*(?:--|$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (m.Success)
        {
            var path = m.Groups[1].Value.Trim();
            if (Path.IsPathRooted(path))
                return File.Exists(path) ? Path.GetDirectoryName(path)! : path;
            var combined = Path.GetFullPath(Path.Combine(cwd, path));
            return File.Exists(combined) ? Path.GetDirectoryName(combined)! : combined;
        }
        return cwd;
    }

    /// <summary>Finds the first .csproj in the given directory (non-recursive).</summary>
    private static string? FindCsproj(string dir)
    {
        if (!Directory.Exists(dir)) return null;
        return Directory.EnumerateFiles(dir, "*.csproj").FirstOrDefault();
    }
}

public class SearchInFilesTool : ITool
{
    public string Name => "search_in_files";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        try
        {
            if (!args.TryGetProperty("query", out var queryProp))
                return Task.FromResult(new ToolResult(false, string.Empty, "Missing required argument: query"));
            var query = queryProp.GetString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(query))
                return Task.FromResult(new ToolResult(false, string.Empty, "Query cannot be empty."));

            var include = args.TryGetProperty("include", out var inc) ? inc.GetString() ?? "*.*" : "*.*";
            if (!Directory.Exists(WorkspaceContext.CurrentPath))
                return Task.FromResult(new ToolResult(false, string.Empty, $"Workspace not found: {WorkspaceContext.CurrentPath}"));

            var regex = new Regex(query, RegexOptions.Multiline, TimeSpan.FromSeconds(2));
            var matches = new StringBuilder();

            foreach (var file in Directory.EnumerateFiles(WorkspaceContext.CurrentPath, include, SearchOption.AllDirectories))
            {
                if (ct.IsCancellationRequested)
                    break;

                try
                {
                    var text = File.ReadAllText(file);
                    if (regex.IsMatch(text))
                        matches.AppendLine(file);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Ignore unreadable files and continue scanning.
                }
            }

            var output = matches.Length == 0 ? "(no matches)" : matches.ToString();
            return Task.FromResult(new ToolResult(true, output));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(false, string.Empty, ex.Message));
        }
    }
}

public class CreateDirectoryTool : ITool
{
    public string Name => "create_directory";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        // accept both "path" and "name" (model sometimes uses "name")
        if (!args.TryGetProperty("path", out var pathProp) && !args.TryGetProperty("name", out pathProp))
            return Task.FromResult(new ToolResult(false, string.Empty, "Missing required argument: path"));
        var path = pathProp.GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        Directory.CreateDirectory(full);
        return Task.FromResult(new ToolResult(true, "Created"));
    }
}

public class GetDiagnosticsTool : ITool
{
    public string Name => "get_diagnostics";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(true, "VS Code diagnostics are not available in WinForms environment."));
    }
}

public class GetOpenFileInfoTool : ITool
{
    public string Name => "get_open_file_info";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(true, "No active editor information available (WinForms)."));
    }
}

public class InsertCodeTool : ITool
{
    public string Name => "insert_code";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(true, "InsertCode is not supported (VS Code only)."));
    }
}

public class OpenBrowserTool : ITool
{
    public string Name => "open_browser";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        if (!args.TryGetProperty("url", out var urlProp))
            return Task.FromResult(new ToolResult(false, string.Empty, "Missing required argument: url"));

        var url = urlProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(url))
            return Task.FromResult(new ToolResult(false, string.Empty, "url cannot be empty"));

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = url,
                UseShellExecute = true
            });
            return Task.FromResult(new ToolResult(true, $"Opened browser: {url}"));
        }
        catch (Exception ex)
        {
            return Task.FromResult(new ToolResult(false, string.Empty, ex.Message));
        }
    }
}

internal static class PathHelper
{
    public static string Resolve(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return WorkspaceContext.CurrentPath;

        if (Path.IsPathRooted(path)) return path;
        return Path.GetFullPath(Path.Combine(WorkspaceContext.CurrentPath, path));
    }
}
