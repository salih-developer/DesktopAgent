using System.Diagnostics;
using System.Text;

namespace DesktopAgent.Utils;

public sealed record ProcessRunResult(
    bool Success,
    string StdOut,
    string StdErr,
    int ExitCode,
    bool TimedOut,
    bool OutputEmpty);

public static class ProcessRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(180);

    public static Task<ProcessRunResult> RunAsync(
        string command,
        CancellationToken ct = default)
    {
        return RunAsync(command, WorkspaceContext.CurrentPath, DefaultTimeout, ct);
    }

    public static async Task<ProcessRunResult> RunAsync(
        string command,
        string? workingDirectory,
        TimeSpan? timeout,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return new ProcessRunResult(
                Success: false,
                StdOut: string.Empty,
                StdErr: "Komut bos olamaz.",
                ExitCode: -1,
                TimedOut: false,
                OutputEmpty: true);
        }

        var cwd = string.IsNullOrWhiteSpace(workingDirectory)
            ? WorkspaceContext.CurrentPath
            : workingDirectory;

        if (!Directory.Exists(cwd))
        {
            return new ProcessRunResult(
                Success: false,
                StdOut: string.Empty,
                StdErr: $"Calisma dizini bulunamadi: {cwd}",
                ExitCode: -1,
                TimedOut: false,
                OutputEmpty: true);
        }

        var effectiveTimeout = timeout.GetValueOrDefault(DefaultTimeout);
        if (effectiveTimeout <= TimeSpan.Zero)
        {
            effectiveTimeout = DefaultTimeout;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c " + command,
            WorkingDirectory = cwd,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true
        };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            return new ProcessRunResult(
                Success: false,
                StdOut: string.Empty,
                StdErr: ex.Message,
                ExitCode: -1,
                TimedOut: false,
                OutputEmpty: true);
        }

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var timedOut = false;
        var cancelled = false;

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested;
            cancelled = ct.IsCancellationRequested;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch
            {
                // Kill failures are non-fatal for result shaping.
            }

            try
            {
                await process.WaitForExitAsync(CancellationToken.None);
            }
            catch
            {
                // Ignore wait failures after kill attempt.
            }
        }

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();

        var exitCode = process.HasExited ? process.ExitCode : -1;
        if (cancelled && string.IsNullOrWhiteSpace(stderr))
        {
            stderr = "Komut iptal edildi.";
        }
        else if (timedOut && string.IsNullOrWhiteSpace(stderr))
        {
            stderr = $"Komut zaman asimina ugradi ({(int)effectiveTimeout.TotalSeconds}s).";
        }

        var outputEmpty = string.IsNullOrWhiteSpace(stdout);
        var success = exitCode == 0 && !timedOut && !cancelled;

        return new ProcessRunResult(
            Success: success,
            StdOut: stdout,
            StdErr: stderr,
            ExitCode: exitCode,
            TimedOut: timedOut,
            OutputEmpty: outputEmpty);
    }
}
