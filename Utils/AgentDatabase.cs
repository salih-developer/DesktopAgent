using Microsoft.Data.Sqlite;

namespace DesktopAgent.Utils;

/// <summary>Summary row stored in run_history for every completed/cancelled/failed run.</summary>
public class RunSummary
{
    public long      Id              { get; set; }
    public string    TaskId          { get; set; } = string.Empty;
    public string    Task            { get; set; } = string.Empty;
    public string    Model           { get; set; } = string.Empty;
    public DateTime  StartedAt       { get; set; }
    public DateTime? CompletedAt     { get; set; }
    public double?   DurationSeconds { get; set; }
    public int       StepCount       { get; set; }
    public int       ToolCalls       { get; set; }
    public int       Errors          { get; set; }
    /// <summary>"completed" | "cancelled" | "error"</summary>
    public string    Status          { get; set; } = string.Empty;
    public string?   ReportPath      { get; set; }
}

/// <summary>
/// Permanent run history stored in SQLite.
/// File: &lt;AppDir&gt;/data/agent.db
/// Active checkpoint state is handled separately by CheckpointStore (JSON).
/// Call Initialize() once at startup — creates the file and schema if missing.
/// </summary>
public static class AgentDatabase
{
    private static readonly string DbPath =
        Path.Combine(AppContext.BaseDirectory, "data", "agent.db");

    private static string ConnectionString => $"Data Source={DbPath}";

    // ── Lifecycle ────────────────────────────────────────────────────────

    /// <summary>Creates data/agent.db and the run_history table on first run; no-op if already exists.
    /// Also runs migrations (e.g. adds deleted_at column to existing databases).</summary>
    public static void Initialize()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath)!);
        using var conn = Open();

        // Create table if missing
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = """
                CREATE TABLE IF NOT EXISTS run_history (
                    id               INTEGER PRIMARY KEY AUTOINCREMENT,
                    task_id          TEXT    NOT NULL,
                    task             TEXT    NOT NULL,
                    model            TEXT    NOT NULL,
                    started_at       TEXT    NOT NULL,
                    completed_at     TEXT,
                    duration_seconds REAL,
                    step_count       INTEGER NOT NULL DEFAULT 0,
                    tool_calls       INTEGER NOT NULL DEFAULT 0,
                    errors           INTEGER NOT NULL DEFAULT 0,
                    status           TEXT    NOT NULL DEFAULT 'unknown',
                    report_path      TEXT,
                    deleted_at       TEXT
                );
                """;
            cmd.ExecuteNonQuery();
        }

        // Migration: add deleted_at to existing databases that don't have it yet
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "PRAGMA table_info(run_history);";
            using var reader = cmd.ExecuteReader();
            var hasDeletedAt = false;
            while (reader.Read())
                if (reader.GetString(1) == "deleted_at") { hasDeletedAt = true; break; }

            if (!hasDeletedAt)
            {
                using var alter = conn.CreateCommand();
                alter.CommandText = "ALTER TABLE run_history ADD COLUMN deleted_at TEXT;";
                alter.ExecuteNonQuery();
            }
        }
    }

    // ── Run History ──────────────────────────────────────────────────────

    public static void SaveRunHistory(RunSummary run)
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO run_history
                (task_id, task, model, started_at, completed_at, duration_seconds,
                 step_count, tool_calls, errors, status, report_path)
            VALUES
                ($task_id, $task, $model, $started_at, $completed_at, $duration_seconds,
                 $step_count, $tool_calls, $errors, $status, $report_path);
            """;
        cmd.Parameters.AddWithValue("$task_id",          run.TaskId);
        cmd.Parameters.AddWithValue("$task",             run.Task);
        cmd.Parameters.AddWithValue("$model",            run.Model);
        cmd.Parameters.AddWithValue("$started_at",       run.StartedAt.ToString("O"));
        cmd.Parameters.AddWithValue("$completed_at",     (object?)run.CompletedAt?.ToString("O") ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$duration_seconds", (object?)run.DurationSeconds            ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$step_count",       run.StepCount);
        cmd.Parameters.AddWithValue("$tool_calls",       run.ToolCalls);
        cmd.Parameters.AddWithValue("$errors",           run.Errors);
        cmd.Parameters.AddWithValue("$status",           run.Status);
        cmd.Parameters.AddWithValue("$report_path",      (object?)run.ReportPath                 ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Returns the most recent non-deleted runs ordered by start time descending.</summary>
    public static List<RunSummary> GetRecentRuns(int limit = 5)
    {
        var list = new List<RunSummary>();
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = $"""
            SELECT id, task_id, task, model, started_at, completed_at,
                   duration_seconds, step_count, tool_calls, errors, status, report_path
            FROM run_history
            WHERE deleted_at IS NULL
            ORDER BY started_at DESC
            LIMIT {limit};
            """;
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new RunSummary
            {
                Id              = reader.GetInt64(0),
                TaskId          = reader.GetString(1),
                Task            = reader.GetString(2),
                Model           = reader.GetString(3),
                StartedAt       = DateTime.Parse(reader.GetString(4)),
                CompletedAt     = reader.IsDBNull(5)  ? null : DateTime.Parse(reader.GetString(5)),
                DurationSeconds = reader.IsDBNull(6)  ? null : reader.GetDouble(6),
                StepCount       = reader.GetInt32(7),
                ToolCalls       = reader.GetInt32(8),
                Errors          = reader.GetInt32(9),
                Status          = reader.GetString(10),
                ReportPath      = reader.IsDBNull(11) ? null : reader.GetString(11),
            });
        }
        return list;
    }

    /// <summary>Soft-deletes all non-deleted run history rows by setting deleted_at to now.
    /// Records remain in the database but are excluded from GetRecentRuns queries.</summary>
    public static void SoftDeleteAllRuns()
    {
        using var conn = Open();
        using var cmd  = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE run_history
            SET deleted_at = $now
            WHERE deleted_at IS NULL;
            """;
        cmd.Parameters.AddWithValue("$now", DateTime.Now.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    // ── Internal ─────────────────────────────────────────────────────────

    private static SqliteConnection Open()
    {
        var conn = new SqliteConnection(ConnectionString);
        conn.Open();
        return conn;
    }
}
