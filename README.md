# DesktopAgent

DesktopAgent is an Ollama-powered Windows desktop AI agent. It understands natural language tasks and can complete them autonomously by using tools such as file operations, terminal commands, and search.

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![LLM](https://img.shields.io/badge/LLM-Ollama-green)

## Features

- **Agentic AI Loop** - The LLM uses tools iteratively until the task is completed.
- **12 Built-in Tools** - File read/write/edit, terminal, file search, directory listing, browser launcher, and more.
- **Real-time Output** - Color-coded output for thinking, tool call, tool result, and final response steps.
- **Local LLM** - Runs fully local with Ollama; your data does not leave your machine.
- **Cancellation Support** - Running tasks can be canceled by the user.
- **Settings Panel** - Configure Ollama URL, model selection, workspace path, and system prompt from the app.
- **Checkpointing** - Active runs are checkpointed after every step. If a run is cancelled or fails, the next run offers to resume from where it left off.
- **Run History** - Every completed, cancelled, or failed run is recorded in a local SQLite database (`data/agent.db`). Recent runs are injected into the system prompt as cross-session memory.
- **Context Reset** - The **↺** button in the header clears the active checkpoint, soft-deletes run history from context, and resets cross-session memory so the agent starts completely fresh.
- **Auto-generated Reports** - A Markdown report is automatically generated after each run and saved to `reports/`.
- **Background Process Launch** - `dotnet run` calls are automatically intercepted and converted to a non-blocking background start (PowerShell `Start-Process`), killing any existing instance of the same project first.

## Requirements

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Ollama](https://ollama.com/) (running locally)
- Windows 10/11

## Installation

```bash
# Clone the repository
git clone <repo-url>
cd DesktopAgent

# Build
dotnet build DesktopAgent.csproj

# Run
dotnet run --project DesktopAgent.csproj
```

> Make sure Ollama is running at `http://localhost:11434`.

## Usage

1. Start the application.
2. Enter your task in the message box (for example: *"Create an ASP.NET Web API project"*).
3. Press **Enter** or click the **Send** button.
4. The agent will think, call required tools, and show the result.

### Header Buttons

| Button | Description |
|--------|-------------|
| **↺** | Clear context — deletes the active checkpoint, soft-deletes run history, and resets cross-session memory. The agent starts fresh on the next task. |
| **⚙** | Open the settings panel. |

### Checkpointing & Resume

After every tool exchange the agent saves a checkpoint to `checkpoints/active.json`. When you start a new task, the app checks for an existing checkpoint and asks whether to resume or start fresh.

- **Resume** — restores the full message history and continues from where it left off.
- **New** — deletes the checkpoint and starts from the beginning.

On successful completion the checkpoint is deleted automatically. On cancellation or error it is kept for the next session.

### Cross-Session Memory

The 5 most recent runs (status, duration, task preview) are injected into the system prompt at the start of each task. This lets the agent know which project was worked on last, avoiding "which project?" questions.

Press **↺** to clear this memory and the run history for a fully clean start.

### Settings Panel

You can open the settings panel from the top-right **⚙** button.

| Field | Description |
|------|----------|
| **Ollama URL** | Ollama server address (for example: `http://localhost:11434`). Use the "List" button to test the connection and fetch models. |
| **Model** | Select a model from the Ollama model list. The selected model is used for all next tasks. |
| **Workspace** | Default working directory for the agent. Select a folder with "Browse...". |
| **System Prompt** | Base instruction text given to the agent. It defines behavior, known languages, and tool usage format. |

Settings are saved persistently to `%APPDATA%/OllamaWin/settings.json` when you click **Save**, and applied immediately.

### Output Color Codes

| Color | Meaning |
|------|-------|
| Blue | Thinking / Planning |
| Indigo | Tool Call |
| Green | Tool Result |
| Dark Gray | Final Response |
| Red | Error |
| Orange | System Message |

## Tools

| Tool | Description | Parameters |
|------|-------------|------------|
| `read_file` | Reads file content | `path` |
| `write_file` | Writes a file | `path`, `content` |
| `edit_file` | Performs find-and-replace | `path`, `search`, `replace` |
| `list_files` | Lists directory contents | `path?`, `recursive?` |
| `run_terminal` | Runs a terminal command | `command`, `cwd?` |
| `search_in_files` | Searches files with regex | `query`, `include?` |
| `create_directory` | Creates a directory | `path` |
| `open_browser` | Opens a URL in the default browser | `url` |
| `list_tools` | Lists available tools | — |

### `run_terminal` — Safe Background Start

Calling `dotnet run` directly would block for up to 180 seconds (timeout). The tool automatically intercepts any `dotnet run` command and:

1. Kills any existing running instance of the same project exe (`taskkill /F`).
2. Starts the process in the background via PowerShell `Start-Process` (no inherited pipe handles).
3. Returns immediately with a status message.

The agent then waits ~3 seconds and opens the browser to the URL from `Properties/launchSettings.json`.

## Data Files

| Path | Description |
|------|-------------|
| `data/agent.db` | SQLite database — permanent run history (`run_history` table). |
| `checkpoints/active.json` | Active checkpoint for the current/last run. Deleted on success; kept on cancel/error. |
| `reports/report-*.md` | Auto-generated Markdown report for each completed run. |
| `logs/desktop-agent-*.log` | Serilog structured log files (daily rotation). |

### `run_history` Schema

```sql
id               INTEGER  PRIMARY KEY AUTOINCREMENT
task_id          TEXT     unique run identifier (GUID)
task             TEXT     task description
model            TEXT     Ollama model used
started_at       TEXT     ISO-8601 start time
completed_at     TEXT     ISO-8601 end time (nullable)
duration_seconds REAL     wall-clock duration (nullable)
step_count       INTEGER  number of agentic steps
tool_calls       INTEGER  total tool calls made
errors           INTEGER  number of tool errors
status           TEXT     "completed" | "cancelled" | "error"
report_path      TEXT     path to the generated Markdown report (nullable)
deleted_at       TEXT     soft-delete timestamp — NULL = visible in context (nullable)
```

Soft-deleted records (`deleted_at IS NOT NULL`) remain in the database but are excluded from cross-session memory injection. Press **↺** to soft-delete all current records.

## Configuration

Settings are stored in `%APPDATA%/OllamaWin/settings.json`:

```json
{
  "OllamaBaseUrl": "http://localhost:11434",
  "WorkspacePath": "D:\\AI\\llmtest",
  "SelectedModel": "qwen3-coder-32k",
  "SystemPrompt": "You are a capable software development agent..."
}
```

All settings can also be changed from the in-app settings panel.

## Architecture

```text
User -> AgentForm -> AgentService -> OllamaClient -> Ollama LLM
                       |
                       -> ToolRegistry -> BasicTools (file, terminal, browser...)
                       |
                       -> CheckpointStore (checkpoints/active.json)
                       -> AgentDatabase   (data/agent.db — SQLite)
```

**Agentic loop:**
1. User enters a task.
2. If a checkpoint exists, AgentForm asks to resume or start fresh.
3. Recent run history is injected into the system prompt as cross-session memory.
4. AgentService sends a request to the LLM with system prompt + task (or resumed message history).
5. The LLM returns either a tool call (`{"tool":"...", "args":{...}}`) or a final response (`[DONE]`).
6. If there is a tool call: ToolRegistry executes it, checkpoint is saved, result goes back to the LLM.
7. The loop continues until `[DONE]` is produced.
8. On completion: checkpoint deleted, run saved to SQLite, Markdown report generated.

## Project Structure

```text
DesktopAgent/
|-- Program.cs                 # Entry point, logging
|-- AgentForm.cs               # UI, user interaction, settings, checkpoint/history logic
|-- AgentForm.Designer.cs      # WinForms designer code
|-- Services/
|   |-- AgentService.cs        # Agentic loop, system prompt, JSON parsing, sanitization
|   |-- ILLMClient.cs          # LLM client interface
|   |-- OllamaClient.cs        # Ollama API client
|   `-- Tools/
|       |-- BasicTools.cs      # Tool implementations (incl. auto background-start guard)
|       |-- ListToolsTool.cs   # Tool listing
|       `-- ToolRegistry.cs    # Tool registry and dispatch
|-- Utils/
|   |-- AgentDatabase.cs       # SQLite run history (SaveRunHistory, GetRecentRuns, SoftDeleteAllRuns)
|   |-- AppSettingsStore.cs    # Settings management
|   |-- CheckpointStore.cs     # Active checkpoint load/save/delete (JSON)
|   |-- ProcessRunner.cs       # Command execution (180s timeout, stdout+stderr capture)
|   `-- WorkspaceContext.cs    # Workspace path management
`-- DesktopAgent.csproj        # .NET 9.0 project file
```

## License

This project is for private use.
