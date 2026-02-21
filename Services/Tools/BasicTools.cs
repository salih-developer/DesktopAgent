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
        var path = args.GetProperty("path").GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        if (!File.Exists(full))
            return new ToolResult(false, string.Empty, "Dosya bulunamadı");
        var text = await File.ReadAllTextAsync(full, ct);
        return new ToolResult(true, text);
    }
}

public class WriteFileTool : ITool
{
    public string Name => "write_file";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? string.Empty;
        var content = args.GetProperty("content").GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllTextAsync(full, content, ct);
        return new ToolResult(true, "Yazıldı");
    }
}

public class EditFileTool : ITool
{
    public string Name => "edit_file";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? string.Empty;
        var search = args.GetProperty("search").GetString() ?? string.Empty;
        var replace = args.GetProperty("replace").GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        if (!File.Exists(full)) return new ToolResult(false, string.Empty, "Dosya yok");
        var text = await File.ReadAllTextAsync(full, ct);
        if (!text.Contains(search))
            return new ToolResult(false, string.Empty, "Aranan metin yok");
        text = text.Replace(search, replace);
        await File.WriteAllTextAsync(full, text, ct);
        return new ToolResult(true, "Değiştirildi");
    }
}

public class ListFilesTool : ITool
{
    public string Name => "list_files";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.TryGetProperty("path", out var p) ? p.GetString() ?? "." : ".";
        var recursive = args.TryGetProperty("recursive", out var r) && r.GetBoolean();
        var full = PathHelper.Resolve(path);
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var items = Directory.Exists(full)
            ? Directory.EnumerateFileSystemEntries(full, "*", option)
            : Enumerable.Empty<string>();
        return Task.FromResult(new ToolResult(true, string.Join(Environment.NewLine, items)));
    }
}

public class RunTerminalTool : ITool
{
    public string Name => "run_terminal";
    public async Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        var command = args.GetProperty("command").GetString() ?? string.Empty;
        var cwd = args.TryGetProperty("cwd", out var cwdProp)
            ? cwdProp.GetString()
            : WorkspaceContext.CurrentPath;

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
            error = $"Komut zaman asimina ugradi. ExitCode: {runResult.ExitCode}";
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
            error = $"Komut basarisiz. ExitCode: {runResult.ExitCode}";
        }

        return new ToolResult(runResult.Success, output, error);
    }
}

public class SearchInFilesTool : ITool
{
    public string Name => "search_in_files";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        var query = args.GetProperty("query").GetString() ?? string.Empty;
        var include = args.TryGetProperty("include", out var inc) ? inc.GetString() ?? "*.*" : "*.*";
        var regex = new Regex(query, RegexOptions.Multiline);
        var matches = new StringBuilder();

        foreach (var file in Directory.EnumerateFiles(WorkspaceContext.CurrentPath, include, SearchOption.AllDirectories))
        {
            if (ct.IsCancellationRequested) break;
            var text = File.ReadAllText(file);
            if (regex.IsMatch(text))
            {
                matches.AppendLine($"{file}");
            }
        }

        return Task.FromResult(new ToolResult(true, matches.ToString()));
    }
}

public class CreateDirectoryTool : ITool
{
    public string Name => "create_directory";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        var path = args.GetProperty("path").GetString() ?? string.Empty;
        var full = PathHelper.Resolve(path);
        Directory.CreateDirectory(full);
        return Task.FromResult(new ToolResult(true, "Oluşturuldu"));
    }
}

public class GetDiagnosticsTool : ITool
{
    public string Name => "get_diagnostics";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(true, "WinForms ortamında VS Code diagnostikleri yok."));
    }
}

public class GetOpenFileInfoTool : ITool
{
    public string Name => "get_open_file_info";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(true, "Aktif editör bilgisi yok (WinForms)."));
    }
}

public class InsertCodeTool : ITool
{
    public string Name => "insert_code";
    public Task<ToolResult> RunAsync(JsonElement args, CancellationToken ct)
    {
        return Task.FromResult(new ToolResult(true, "InsertCode desteklenmiyor (yalnızca VS Code)."));
    }
}

internal static class PathHelper
{
    public static string Resolve(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        return Path.GetFullPath(Path.Combine(WorkspaceContext.CurrentPath, path));
    }
}
