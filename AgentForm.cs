using System.Diagnostics;
using System.Text;
using DesktopAgent.Services;
using DesktopAgent.Services.Tools;
using DesktopAgent.Utils;
using Serilog;

namespace DesktopAgent
{
    public partial class AgentForm : Form
    {
        private CancellationTokenSource? _agentCts;
        private readonly OllamaClient _ollama;
        private readonly ToolRegistry _tools;
        private readonly AgentService _agent;
        private AppSettings _settings;
        private string _selectedModel = "";
        private bool _isAgentRunning = false;
        private System.Windows.Forms.Timer _elapsedTimer = null!;
        private DateTime _taskStartTime;
        private DateTime _lastStepTime;

        private record StepRecord(string Type, string Content, string? Detail, string? ToolName, DateTime Timestamp, double DeltaSeconds);
        private readonly List<StepRecord> _currentRunSteps = new();

        public AgentForm()
        {
            AgentDatabase.Initialize();   // create data/agent.db + tables if missing

            _settings = AppSettingsStore.Load();
            _settings.WorkspacePath = AppSettingsStore.NormalizeWorkspacePath(_settings.WorkspacePath);
            WorkspaceContext.Set(_settings.WorkspacePath);

            _ollama = new OllamaClient(_settings.OllamaBaseUrl);
            ToolRegistry? registry = null;
            var toolList = new ITool[]
            {
                new ReadFileTool(),
                new WriteFileTool(),
                new EditFileTool(),
                new ListFilesTool(),
                new RunTerminalTool(),
                new SearchInFilesTool(),
                new CreateDirectoryTool(),
                new GetDiagnosticsTool(),
                new GetOpenFileInfoTool(),
                new InsertCodeTool(),
                new OpenBrowserTool(),
                new ListToolsTool(() => registry?.GetToolNames() ?? Array.Empty<string>())
            };
            registry = new ToolRegistry(toolList);
            _tools = registry;
            _agent = new AgentService(_ollama, _tools, _settings.SystemPrompt);
            InitializeComponent();
            DoubleBuffered = true;

            // Load app icon
            var icoPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
            var appIcon = File.Exists(icoPath) ? new Icon(icoPath) : SystemIcons.Application;
            Icon = appIcon;
            trayIcon.Icon = appIcon;

            // Tray menu setup
            trayMenu.Items.Add("Open", null, (s, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); });
            trayMenu.Items.Add("-");
            trayMenu.Items.Add("Exit", null, (s, e) => { trayIcon.Visible = false; Application.Exit(); });
            trayIcon.DoubleClick += (s, e) => { Show(); WindowState = FormWindowState.Normal; Activate(); };

            _selectedModel = _settings.SelectedModel;
            messageTextBox.KeyDown += txtTask_KeyDown;

            _elapsedTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            _elapsedTimer.Tick += ElapsedTimer_Tick;

            rtbAgent.DetectUrls = true;
            rtbAgent.LinkClicked += RtbAgent_LinkClicked;

            // Center settings panel on resize
            Resize += (_, _) => CenterSettingsPanel();

            Log.Information(
                "AgentForm initialized. WorkspacePath: {WorkspacePath}, OllamaBaseUrl: {OllamaBaseUrl}",
                _settings.WorkspacePath,
                _settings.OllamaBaseUrl);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                trayIcon.ShowBalloonTip(1500, "Desktop Agent", "Application minimized to system tray.", ToolTipIcon.Info);
                return;
            }
            base.OnFormClosing(e);
        }

        private void CenterSettingsPanel()
        {
            if (settingsInnerPanel == null || settingsOverlayPanel == null) return;
            settingsInnerPanel.Left = (settingsOverlayPanel.ClientSize.Width - settingsInnerPanel.Width) / 2;
            settingsInnerPanel.Top = (settingsOverlayPanel.ClientSize.Height - settingsInnerPanel.Height) / 2;
        }

        // ── Clear Context ────────────────────────────────────────────────

        private void clearContextButton_Click(object? sender, EventArgs e)
        {
            if (_isAgentRunning) return;

            CheckpointStore.Delete();
            AgentDatabase.SoftDeleteAllRuns();
            _agent.SetRecentContext(string.Empty);

            // Brief green flash as visual feedback
            clearContextButton.ForeColor = Color.FromArgb(34, 197, 94);
            var restoreTimer = new System.Windows.Forms.Timer { Interval = 1200 };
            restoreTimer.Tick += (_, _) =>
            {
                clearContextButton.ForeColor = Color.FromArgb(148, 163, 184);
                restoreTimer.Stop();
                restoreTimer.Dispose();
            };
            restoreTimer.Start();

            Log.Information("Context cleared by user (checkpoint deleted, recent context reset)");
        }

        // ── Settings Panel ──────────────────────────────────────────────

        private void settingsButton_Click(object? sender, EventArgs e)
        {
            // Populate current values
            ollamaUrlTextBox.Text = _settings.OllamaBaseUrl;
            workspaceTextBox.Text = _settings.WorkspacePath;
            systemPromptTextBox.Text = _settings.SystemPrompt;
            modelComboBox.Items.Clear();
            if (!string.IsNullOrWhiteSpace(_selectedModel))
            {
                modelComboBox.Items.Add(_selectedModel);
                modelComboBox.SelectedItem = _selectedModel;
            }

            CenterSettingsPanel();
            settingsOverlayPanel.Visible = true;
            settingsOverlayPanel.BringToFront();

            // Auto-fetch models
            _ = FetchModelsAsync();
        }

        private void closeSettingsButton_Click(object? sender, EventArgs e)
        {
            settingsOverlayPanel.Visible = false;
        }

        private async void fetchModelsButton_Click(object? sender, EventArgs e)
        {
            await FetchModelsAsync();
        }

        private async Task FetchModelsAsync()
        {
            var url = ollamaUrlTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(url)) return;

            var previousSelection = modelComboBox.SelectedItem?.ToString();
            fetchModelsButton.Enabled = false;
            fetchModelsButton.Text = "...";
            modelComboBox.Items.Clear();

            try
            {
                // Create a temporary client to fetch models from the entered URL
                var tempClient = new OllamaClient(url);
                var models = await tempClient.ListModelsAsync();

                modelComboBox.Items.Clear();
                foreach (var model in models)
                    modelComboBox.Items.Add(model);

                // Try to select the currently active model
                if (!string.IsNullOrEmpty(_selectedModel) && modelComboBox.Items.Contains(_selectedModel))
                    modelComboBox.SelectedItem = _selectedModel;
                else if (!string.IsNullOrEmpty(previousSelection) && modelComboBox.Items.Contains(previousSelection))
                    modelComboBox.SelectedItem = previousSelection;
                else if (modelComboBox.Items.Count > 0)
                    modelComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch models from {Url}", url);
                MessageBox.Show(
                    $"Failed to fetch model list:\n{ex.Message}",
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                fetchModelsButton.Text = "List";
                fetchModelsButton.Enabled = true;
            }
        }

        private void browseWorkspaceButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Select workspace directory",
                UseDescriptionForTitle = true,
                ShowNewFolderButton = true
            };

            if (!string.IsNullOrEmpty(workspaceTextBox.Text) && Directory.Exists(workspaceTextBox.Text))
                dialog.InitialDirectory = workspaceTextBox.Text;

            if (dialog.ShowDialog(this) == DialogResult.OK)
                workspaceTextBox.Text = dialog.SelectedPath;
        }

        private void saveSettingsButton_Click(object? sender, EventArgs e)
        {
            // Validate URL
            if (!AppSettingsStore.TryNormalizeBaseUrl(ollamaUrlTextBox.Text, out var normalizedUrl, out var urlError))
            {
                MessageBox.Show(urlError, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate workspace
            var workspace = AppSettingsStore.NormalizeWorkspacePath(workspaceTextBox.Text);

            // Get selected model
            var selectedModel = modelComboBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(selectedModel))
            {
                selectedModel = _selectedModel;
            }

            selectedModel ??= string.Empty;
            var systemPrompt = systemPromptTextBox.Text.Trim();

            // Apply
            _settings.OllamaBaseUrl = normalizedUrl;
            _settings.WorkspacePath = workspace;
            _settings.SelectedModel = selectedModel;
            _settings.SystemPrompt = systemPrompt;
            _selectedModel = selectedModel;

            _ollama.SetBaseUrl(normalizedUrl);
            WorkspaceContext.Set(workspace);
            _agent.SetSystemPrompt(systemPrompt);
            AppSettingsStore.Save(_settings);

            settingsOverlayPanel.Visible = false;

            AppendAgentLine("SYSTEM",
                $"Settings saved. Model: {selectedModel}, URL: {normalizedUrl}, Workspace: {workspace}",
                Color.FromArgb(180, 83, 9));

            Log.Information("Settings saved. Model: {Model}, URL: {Url}, Workspace: {Workspace}",
                selectedModel, normalizedUrl, workspace);
        }

        // ── Task Execution ──────────────────────────────────────────────

        private void txtTask_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift)
            {
                e.SuppressKeyPress = true;
                sendButton.PerformClick();
            }
        }

        private void ElapsedTimer_Tick(object? sender, EventArgs e)
        {
            var elapsed = DateTime.Now - _taskStartTime;
            timerLabel.Text = $"⏱ {(int)elapsed.TotalMinutes:D2}:{elapsed.Seconds:D2}";
        }

        private void SetRunningState(bool running)
        {
            _isAgentRunning = running;
            if (running)
            {
                _taskStartTime = DateTime.Now;
                _lastStepTime = _taskStartTime;
                _currentRunSteps.Clear();
                _elapsedTimer.Start();
                timerLabel.Text = "⏱ 00:00";
                timerLabel.Visible = true;
                sendButton.Text = "■";
                sendButton.BackColor = Color.FromArgb(185, 28, 28);
            }
            else
            {
                _elapsedTimer.Stop();
                timerLabel.Visible = false;
                sendButton.Text = ">";
                sendButton.BackColor = Color.FromArgb(210, 113, 69);
            }
        }

        private async void sendButton_Click(object sender, EventArgs e)
        {
            // If agent is running, cancel it
            if (_isAgentRunning)
            {
                _agentCts?.Cancel();
                return;
            }

            // Use selected model, fallback to default
            string model = !string.IsNullOrWhiteSpace(_selectedModel)
                ? _selectedModel
                : "qwen3-coder-32k";

            // ── Checkpoint resume check ──────────────────────────────────────
            List<ChatMessage>? resumeMessages = null;
            string? resumeTask = null;
            var checkpoint = CheckpointStore.Load();
            if (checkpoint != null)
            {
                var preview = checkpoint.Task.Length > 100
                    ? checkpoint.Task[..100] + "…"
                    : checkpoint.Task;

                var answer = MessageBox.Show(
                    $"Yarım kalan bir görev bulundu:\n\n\"{preview}\"\n\n" +
                    $"{checkpoint.StepCount} adım tamamlandı ({checkpoint.UpdatedAt:HH:mm:ss})\n\n" +
                    $"Kaldığı yerden devam edilsin mi?",
                    "Checkpoint Bulundu",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button1);

                if (answer == DialogResult.Yes)
                {
                    resumeTask = checkpoint.Task;
                    model = !string.IsNullOrWhiteSpace(checkpoint.Model) ? checkpoint.Model : model;
                    resumeMessages = checkpoint.Messages
                        .Select(m => new ChatMessage(m.Role, m.Content))
                        .ToList();
                }
                else
                {
                    CheckpointStore.Delete();
                }
            }

            // Task comes from checkpoint (resume) or text box (new task)
            var task = resumeTask ?? messageTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(task))
            {
                Log.Warning("Task submit ignored because input was empty");
                MessageBox.Show("Please enter a task.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Log.Information("Task queued. Model: {Model}, TaskLength: {TaskLength}, Resume: {Resume}",
                model, task.Length, resumeMessages != null);

            // Inject recent run history so the agent has cross-session context.
            var recentRuns = AgentDatabase.GetRecentRuns(5);
            if (recentRuns.Count > 0)
            {
                var ctx = string.Join("\n", recentRuns.Select(r =>
                {
                    var taskPreview = r.Task.Length > 80 ? r.Task[..80] + "…" : r.Task;
                    var dur = r.DurationSeconds.HasValue ? $" {r.DurationSeconds:F0}s" : "";
                    return $"- [{r.StartedAt:yyyy-MM-dd HH:mm}] {r.Status}{dur}: {taskPreview}";
                }));
                _agent.SetRecentContext(ctx);
            }

            _agentCts?.Cancel();
            _agentCts = new CancellationTokenSource();

            // ── Checkpoint data for this run ─────────────────────────────────
            var checkpointData = new CheckpointData
            {
                TaskId    = checkpoint?.TaskId ?? Guid.NewGuid().ToString("N")[..8],
                Task      = task,
                Model     = model,
                CreatedAt = checkpoint?.CreatedAt ?? DateTime.Now,
            };

            // Display user input
            if (resumeMessages != null)
                AppendAgentLine("SYSTEM",
                    $"Checkpoint'ten devam ediliyor — {checkpoint!.StepCount} adım geri yüklendi",
                    Color.FromArgb(180, 83, 9));

            AppendAgentLine("USER", task, Color.FromArgb(100, 200, 255));
            AppendAgentLine("SESSION", $"Agent running... (Model: {model})", Color.FromArgb(30, 64, 175));

            SetRunningState(true);
            try
            {
                messageTextBox.Text = string.Empty;
                await _agent.RunAsync(model, task, step =>
                {
                    if (step.Type is "tool_call" or "tool_result")
                        Log.Debug("Agent step {StepType}. Tool: {ToolName}", step.Type, step.ToolName ?? "-");
                    AppendAgent(step);
                },
                _agentCts.Token,
                resumeMessages: resumeMessages,
                onCheckpoint: msgs =>
                {
                    checkpointData.StepCount = msgs.Count(m => m.role == "assistant");
                    checkpointData.Messages  = msgs
                        .Select(m => new CheckpointMessage { Role = m.role, Content = m.content })
                        .ToList();
                    CheckpointStore.Save(checkpointData);
                });

                var duration = (DateTime.Now - _taskStartTime).TotalSeconds;
                Log.Information("Task completed in {Duration:F1}s", duration);
                CheckpointStore.Delete();
                AgentDatabase.SaveRunHistory(new RunSummary
                {
                    TaskId          = checkpointData.TaskId,
                    Task            = task,
                    Model           = model,
                    StartedAt       = _taskStartTime,
                    CompletedAt     = DateTime.Now,
                    DurationSeconds = duration,
                    StepCount       = _currentRunSteps.Count,
                    ToolCalls       = _currentRunSteps.Count(s => s.Type == "tool_call"),
                    Errors          = _currentRunSteps.Count(s => s.Type == "tool_result" && s.Content.StartsWith("ERR")),
                    Status          = "completed",
                });
                AppendAgentLine("SYSTEM", $"Tamamlandı — {duration:F1}s", Color.FromArgb(22, 101, 52));
                _ = GenerateAndSaveReportAsync(task, model, _currentRunSteps.ToList(), duration);
            }
            catch (OperationCanceledException)
            {
                var duration = (DateTime.Now - _taskStartTime).TotalSeconds;
                Log.Information("Task canceled by user after {Duration:F1}s", duration);
                // Checkpoint is kept so the user can resume later.
                AgentDatabase.SaveRunHistory(new RunSummary
                {
                    TaskId          = checkpointData.TaskId,
                    Task            = task,
                    Model           = model,
                    StartedAt       = _taskStartTime,
                    CompletedAt     = DateTime.Now,
                    DurationSeconds = duration,
                    StepCount       = _currentRunSteps.Count,
                    ToolCalls       = _currentRunSteps.Count(s => s.Type == "tool_call"),
                    Errors          = _currentRunSteps.Count(s => s.Type == "tool_result" && s.Content.StartsWith("ERR")),
                    Status          = "cancelled",
                });
                AppendAgentLine("SYSTEM",
                    $"İptal edildi — checkpoint kaydedildi, tekrar Send ile devam edebilirsiniz ({duration:F1}s)",
                    Color.FromArgb(180, 83, 9));
            }
            catch (Exception ex)
            {
                var duration = (DateTime.Now - _taskStartTime).TotalSeconds;
                Log.Error(ex, "Task execution failed after {Duration:F1}s", duration);
                // Checkpoint is kept so the user can attempt to resume after the error.
                AgentDatabase.SaveRunHistory(new RunSummary
                {
                    TaskId          = checkpointData.TaskId,
                    Task            = task,
                    Model           = model,
                    StartedAt       = _taskStartTime,
                    CompletedAt     = DateTime.Now,
                    DurationSeconds = duration,
                    StepCount       = _currentRunSteps.Count,
                    ToolCalls       = _currentRunSteps.Count(s => s.Type == "tool_call"),
                    Errors          = _currentRunSteps.Count(s => s.Type == "tool_result" && s.Content.StartsWith("ERR")),
                    Status          = "error",
                });
                AppendAgentLine("ERROR", $"{ex.Message} — {duration:F1}s", Color.FromArgb(185, 28, 28));
            }
            finally
            {
                SetRunningState(false);
            }
        }

        private void AppendAgent(AgentStep step)
        {
            if (InvokeRequired)
            {
                Invoke(() => AppendAgent(step));
                return;
            }

            var now = DateTime.Now;
            var delta = (now - _lastStepTime).TotalSeconds;
            _lastStepTime = now;
            _currentRunSteps.Add(new StepRecord(step.Type, step.Content, step.Detail, step.ToolName, now, delta));

            // llm_reply is captured for reports but not shown in the chat UI.
            if (step.Type == "llm_reply")
                return;

            var label = step.Type.ToUpperInvariant();
            var color = step.Type switch
            {
                "thinking"    => Color.FromArgb(3, 105, 161),
                "tool_call"   => Color.FromArgb(79, 70, 229),
                "tool_result" => Color.FromArgb(22, 101, 52),
                "response"    => Color.FromArgb(30, 41, 59),
                _             => Color.FromArgb(30, 41, 59)
            };

            var content = step.Content;
            if (!string.IsNullOrWhiteSpace(step.ToolName))
                content = $"{content} | Tool: {step.ToolName}";

            var deltaStr = delta >= 0.1 ? $" (+{delta:F1}s)" : "";
            AppendAgentLine(label, content + deltaStr, color);
        }

        private async Task GenerateAndSaveReportAsync(string userTask, string model, List<StepRecord> steps, double totalSeconds)
        {
            try
            {
                var reportsDir = Path.Combine(AppContext.BaseDirectory, "reports");
                Directory.CreateDirectory(reportsDir);

                var fileTs = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var filePath = Path.Combine(reportsDir, $"report-{fileTs}.md");

                var toolCallCount = steps.Count(s => s.Type == "tool_call");
                var errCount      = steps.Count(s => s.Type == "tool_result" && s.Content.StartsWith("ERR"));
                var finalResponse = steps.LastOrDefault(s => s.Type == "response")?.Content ?? "(no response)";

                // ── Detailed timeline (markdown with collapsible detail blocks) ──
                var timeline = new StringBuilder();
                foreach (var s in steps)
                {
                    var tool   = s.ToolName != null ? $" `{s.ToolName}`" : "";
                    var dur    = s.DeltaSeconds >= 0.1 ? $" **+{s.DeltaSeconds:F1}s**" : "";
                    var brief  = s.Content.Replace("\n", " ").Replace("\r", "");
                    if (brief.Length > 120) brief = brief[..120] + "…";

                    timeline.AppendLine($"### `{s.Timestamp:HH:mm:ss}` {s.Type.ToUpperInvariant()}{tool}{dur}");
                    timeline.AppendLine();
                    timeline.AppendLine(brief);
                    timeline.AppendLine();

                    if (!string.IsNullOrWhiteSpace(s.Detail) && s.Detail != s.Content)
                    {
                        var detailLabel = s.Type switch
                        {
                            "llm_reply"   => "Full LLM Response",
                            "tool_call"   => "Tool Arguments (JSON)",
                            "tool_result" => "Full Tool Output",
                            "response"    => "Final Response",
                            _             => "Detail"
                        };
                        timeline.AppendLine($"<details><summary>{detailLabel}</summary>");
                        timeline.AppendLine();
                        timeline.AppendLine("```");
                        timeline.AppendLine(s.Detail);
                        timeline.AppendLine("```");
                        timeline.AppendLine();
                        timeline.AppendLine("</details>");
                        timeline.AppendLine();
                    }
                }

                // ── LLM summary ───────────────────────────────────────────
                var summaryPrompt = $"""
You are a developer assistant. Analyze this AI agent run log and write a concise developer report in Markdown.

Include exactly these sections:
## Summary
One sentence describing what was accomplished.

## Actions Taken
Bullet list of each tool call and what it did.

## Outcome
Was the task completed successfully? Any errors?

## Metrics
- Total time: {totalSeconds:F1}s
- Tool calls: {toolCallCount}
- Errors: {errCount}

--- RAW TIMELINE ---
{timeline}

--- FINAL RESPONSE ---
{finalResponse}
--- END ---

Write the developer report now. Use markdown. Be concise.
""";

                string summary;
                try
                {
                    var msgs = new List<ChatMessage> { new("user", summaryPrompt) };
                    summary = await _ollama.ChatAsync(model, msgs, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "LLM summarization failed, using raw report");
                    summary = $"*(LLM summarization failed: {ex.Message})*";
                }

                // ── Final markdown file ───────────────────────────────────
                var report = new StringBuilder();
                report.AppendLine($"# Agent Run Report — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                report.AppendLine();
                report.AppendLine($"**Model:** {model}  ");
                report.AppendLine($"**Total Duration:** {totalSeconds:F1}s  ");
                report.AppendLine($"**Tool Calls:** {toolCallCount}  ");
                report.AppendLine($"**Errors:** {errCount}  ");
                report.AppendLine();
                report.AppendLine("## Task");
                report.AppendLine();
                report.AppendLine(userTask);
                report.AppendLine();
                report.AppendLine("## Developer Summary");
                report.AppendLine();
                report.AppendLine(summary);
                report.AppendLine();
                report.AppendLine("---");
                report.AppendLine();
                report.AppendLine("## Detailed Timeline");
                report.AppendLine();
                report.Append(timeline);

                await File.WriteAllTextAsync(filePath, report.ToString());
                Log.Information("Report saved to {FilePath}", filePath);
                var fileUri = new Uri(filePath).AbsoluteUri;
                AppendAgentLine("SYSTEM", $"📄 Report: {fileUri}", Color.FromArgb(180, 83, 9));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to generate run report");
            }
        }

        private void RtbAgent_LinkClicked(object? sender, LinkClickedEventArgs e)
        {
            var link = e.LinkText ?? string.Empty;
            try
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to open link: {Link}", link);
            }
        }

        private void AppendAgentLine(string label, string content, Color color)
        {
            var timestamp = DateTime.Now.ToString("HH:mm:ss");
            var line = $"[{timestamp}] {label}: {content}{Environment.NewLine}";

            rtbAgent.SelectionStart = rtbAgent.TextLength;
            rtbAgent.SelectionLength = 0;
            rtbAgent.SelectionColor = color;
            rtbAgent.AppendText(line);
            rtbAgent.SelectionColor = rtbAgent.ForeColor;
            rtbAgent.ScrollToCaret();
        }
    }
}
