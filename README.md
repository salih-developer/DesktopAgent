# DesktopAgent

DesktopAgent is an Ollama-powered Windows desktop AI agent. It understands natural language tasks and can complete them autonomously by using tools such as file operations, terminal commands, and search.

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![LLM](https://img.shields.io/badge/LLM-Ollama-green)

## Features

- **Agentic AI Loop** - The LLM uses tools iteratively until the task is completed.
- **11 Built-in Tools** - File read/write/edit, terminal, file search, directory listing, and more.
- **Real-time Output** - Color-coded output for thinking, tool call, tool result, and final response steps.
- **Local LLM** - Runs fully local with Ollama; your data does not leave your machine.
- **Cancellation Support** - Running tasks can be canceled by the user.
- **Settings Panel** - Configure Ollama URL, model selection, workspace path, and system prompt from the app.

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
3. Press Enter or click the **Send** button.
4. The agent will think, call required tools, and show the result.

### Settings Panel

You can open the settings panel from the top-right **gear** button.

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
|------|----------|-------------|
| `read_file` | Reads file content | `path` |
| `write_file` | Writes a file | `path`, `content` |
| `edit_file` | Performs find-and-replace | `path`, `search`, `replace` |
| `list_files` | Lists directory contents | `path?`, `recursive?` |
| `run_terminal` | Runs a terminal command | `command`, `cwd?` |
| `search_in_files` | Searches files with regex | `query`, `include?` |
| `create_directory` | Creates a directory | `path` |
| `list_tools` | Lists available tools | - |

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
                       -> ToolRegistry -> BasicTools (file, terminal, search...)
```

**Agentic loop:**
1. User enters a task.
2. AgentService sends a request to the LLM with system prompt + task.
3. The LLM returns either a tool call (`{"tool":"...", "args":{...}}`) or a final response (`[YANIT]`).
4. If there is a tool call: ToolRegistry executes it and sends the result back to the LLM.
5. The loop continues until `[YANIT]` is produced.

## Project Structure

```text
DesktopAgent/
|-- Program.cs                 # Entry point, logging
|-- AgentForm.cs               # UI, user interaction, and settings panel
|-- AgentForm.Designer.cs      # WinForms designer code
|-- Services/
|   |-- AgentService.cs        # Agentic loop (system prompt can be set externally)
|   |-- ILLMClient.cs          # LLM client interface
|   |-- OllamaClient.cs        # Ollama API client
|   `-- Tools/
|       |-- BasicTools.cs      # Tool implementations
|       |-- ListToolsTool.cs   # Tool listing
|       `-- ToolRegistry.cs    # Tool registry
|-- Utils/
|   |-- AppSettingsStore.cs    # Settings management (URL, model, workspace, system prompt)
|   |-- ProcessRunner.cs       # Command execution
|   `-- WorkspaceContext.cs    # Workspace management
`-- DesktopAgent.csproj        # .NET 9.0 project file
```

## License

This project is for private use.
