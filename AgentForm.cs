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
            _agent = new AgentService(_ollama, _tools);
            InitializeComponent();
            DoubleBuffered = true;

            messageTextBox.KeyDown += txtTask_KeyDown;
            Log.Information(
                "AgentForm initialized. WorkspacePath: {WorkspacePath}, OllamaBaseUrl: {OllamaBaseUrl}",
                _settings.WorkspacePath,
                _settings.OllamaBaseUrl);
        }

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
            string model = "qwen3-coder-32k";
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

            AppendAgentLine("SESSION", "Agent calisiyor...", Color.FromArgb(30, 64, 175));

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
