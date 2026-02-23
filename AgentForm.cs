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

        public AgentForm()
        {
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
                new ListToolsTool(() => registry?.GetToolNames() ?? Array.Empty<string>())
            };
            registry = new ToolRegistry(toolList);
            _tools = registry;
            _agent = new AgentService(_ollama, _tools, _settings.SystemPrompt);
            InitializeComponent();
            DoubleBuffered = true;

            _selectedModel = _settings.SelectedModel;
            messageTextBox.KeyDown += txtTask_KeyDown;

            // Center settings panel on resize
            Resize += (_, _) => CenterSettingsPanel();

            Log.Information(
                "AgentForm initialized. WorkspacePath: {WorkspacePath}, OllamaBaseUrl: {OllamaBaseUrl}",
                _settings.WorkspacePath,
                _settings.OllamaBaseUrl);
        }

        private void CenterSettingsPanel()
        {
            if (settingsInnerPanel == null || settingsOverlayPanel == null) return;
            settingsInnerPanel.Left = (settingsOverlayPanel.ClientSize.Width - settingsInnerPanel.Width) / 2;
            settingsInnerPanel.Top = (settingsOverlayPanel.ClientSize.Height - settingsInnerPanel.Height) / 2;
        }

        // ── Settings Panel ──────────────────────────────────────────────

        private void settingsButton_Click(object? sender, EventArgs e)
        {
            // Populate current values
            ollamaUrlTextBox.Text = _settings.OllamaBaseUrl;
            workspaceTextBox.Text = _settings.WorkspacePath;
            systemPromptTextBox.Text = _settings.SystemPrompt;

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
                else if (modelComboBox.Items.Count > 0)
                    modelComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to fetch models from {Url}", url);
                MessageBox.Show(
                    $"Model listesi alinamadi:\n{ex.Message}",
                    "Hata",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            finally
            {
                fetchModelsButton.Text = "Listele";
                fetchModelsButton.Enabled = true;
            }
        }

        private void browseWorkspaceButton_Click(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Workspace dizinini secin",
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
                MessageBox.Show(urlError, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Validate workspace
            var workspace = AppSettingsStore.NormalizeWorkspacePath(workspaceTextBox.Text);

            // Get selected model
            var selectedModel = modelComboBox.SelectedItem?.ToString() ?? "";
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
                $"Ayarlar kaydedildi. Model: {selectedModel}, URL: {normalizedUrl}, Workspace: {workspace}",
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
                if (sendButton.Enabled)
                    sendButton.PerformClick();
            }
        }

        private async void sendButton_Click(object sender, EventArgs e)
        {
            // Use selected model, fallback to default
            string model = !string.IsNullOrWhiteSpace(_selectedModel)
                ? _selectedModel
                : "qwen3-coder-32k";

            var task = messageTextBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(task))
            {
                Log.Warning("Task submit ignored because input was empty");
                MessageBox.Show("Lutfen bir gorev yazin.", "Uyari", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Log.Information("Task queued. Model: {Model}, TaskLength: {TaskLength}", model, task.Length);

            _agentCts?.Cancel();
            _agentCts = new CancellationTokenSource();

            AppendAgentLine("SESSION", $"Agent calisiyor... (Model: {model})", Color.FromArgb(30, 64, 175));

            try
            {
                messageTextBox.Text = string.Empty;
                await foreach (var step in _agent.RunAsync(model, task, _agentCts.Token))
                {
                    if (step.Type is "tool_call" or "tool_result")
                    {
                        Log.Debug("Agent step {StepType}. Tool: {ToolName}", step.Type, step.ToolName ?? "-");
                    }

                    AppendAgent(step);
                }

                Log.Information("Task completed successfully");
            }
            catch (OperationCanceledException)
            {
                Log.Information("Task canceled by user");
                AppendAgentLine("SYSTEM", "Gorev iptal edildi.", Color.FromArgb(180, 83, 9));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Task execution failed");
                AppendAgentLine("ERROR", ex.Message, Color.FromArgb(185, 28, 28));
            }
        }

        private void AppendAgent(AgentStep step)
        {
            if (InvokeRequired)
            {
                Invoke(() => AppendAgent(step));
                return;
            }

            var label = step.Type.ToUpperInvariant();
            var color = step.Type switch
            {
                "thinking" => Color.FromArgb(3, 105, 161),
                "tool_call" => Color.FromArgb(79, 70, 229),
                "tool_result" => Color.FromArgb(22, 101, 52),
                "response" => Color.FromArgb(30, 41, 59),
                _ => Color.FromArgb(30, 41, 59)
            };

            var content = step.Content;
            if (!string.IsNullOrWhiteSpace(step.ToolName))
                content = $"{content} | Tool: {step.ToolName}";

            AppendAgentLine(label, content, color);
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
