# DesktopAgent - Project Guide

## Overview
DesktopAgent is a Windows Forms based desktop AI agent application. It works with local LLM models through Ollama. The user provides tasks in natural language, and the agent executes them autonomously using tools such as file read/write, terminal command execution, and file search.

## Tech Stack
- **Framework:** .NET 9.0 (net9.0-windows)
- **UI:** Windows Forms (WinForms)
- **Language:** C# 12+ (nullable enabled, implicit usings)
- **LLM:** Ollama (default: `http://localhost:11434`)
- **Packages:** Serilog (logging), Newtonsoft.Json, System.Text.Json

## Project Structure
```text
DesktopAgent/
|-- Program.cs                    # App entry point, Serilog configuration
|-- AgentForm.cs                  # Main UI form, user interactions, settings panel
|-- AgentForm.Designer.cs         # WinForms designer code
|-- Services/
|   |-- AgentService.cs           # Agent run loop (agentic loop)
|   |-- ILLMClient.cs             # LLM client interface
|   |-- OllamaClient.cs           # Ollama HTTP API client
|   `-- Tools/
|       |-- BasicTools.cs         # All tool implementations (9 tools)
|       |-- ListToolsTool.cs      # Tool listing tool
|       `-- ToolRegistry.cs       # Tool registration and dispatch
|-- Utils/
|   |-- AppSettingsStore.cs       # Settings load/save (%APPDATA%)
|   |-- ProcessRunner.cs          # Command execution (180s timeout)
|   `-- WorkspaceContext.cs       # Working directory management
`-- DesktopAgent.csproj           # Project configuration
```

## Architecture
- **AgentForm** -> Receives user messages and sends them to AgentService. The settings panel (gear button) configures Ollama URL, model, workspace, and system prompt.
- **AgentService** -> Agentic loop: sends prompts to the LLM, parses replies, executes tool calls, and feeds tool output back. The loop ends when `[YANIT]` is returned. The system prompt can be changed externally via `SetSystemPrompt()`.
- **ToolRegistry** -> Registers and executes tools implementing the `ITool` interface.
- **OllamaClient** -> Communicates with Ollama REST API. URL can be changed with `SetBaseUrl()`. Available models can be listed with `ListModelsAsync()`.

## Key Conventions
- All UI text and default system prompt are Turkish.
- Tool calls use JSON format: `{"tool":"tool_name","args":{...}}`
- Final response ends with `[YANIT]`.
- Relative paths are resolved against the workspace directory (`PathHelper.Resolve`).
- Settings are stored in `%APPDATA%/OllamaWin/settings.json`.
- Model is selected in the settings panel (fallback: `qwen3-coder-32k`).
- System prompt is editable in the settings panel.
- Logs are written to `<AppDir>/logs/desktop-agent-YYYY-MM-DD.log`.

## Settings (AppSettings)
| Field | Default | Description |
|------|-----------|----------|
| `OllamaBaseUrl` | `http://localhost:11434` | Ollama server URL |
| `WorkspacePath` | `D:\AI\llmtest` | Default working directory |
| `SelectedModel` | `""` (falls back to `qwen3-coder-32k`) | LLM model to use |
| `SystemPrompt` | Turkish agent instructions | Base system prompt text for the agent |

## Settings Panel (Gear Button)
Clicking the gear button in the header opens an overlay panel:
- **Ollama URL** - TextBox + "List" button (fetches models from Ollama API)
- **Model** - ComboBox (DropDownList), populated from Ollama models
- **Workspace** - TextBox (ReadOnly) + "Browse..." button (FolderBrowserDialog)
- **System Prompt** - Multiline TextBox (100px, scrollbars enabled)
- **Save** - Persists all settings and applies them immediately
- **Close** - Closes the panel (also closes on overlay click)

## Available Tools
| Tool | Description |
|------|----------|
| `read_file` | Reads file content |
| `write_file` | Writes a file |
| `edit_file` | Find-and-replace in a file |
| `list_files` | Lists directory content |
| `run_terminal` | Executes terminal commands |
| `search_in_files` | Regex search across files |
| `create_directory` | Creates a directory |
| `list_tools` | Lists available tools |
| `get_diagnostics` | Diagnostic info (WinForms stub) |
| `get_open_file_info` | Active editor info (WinForms stub) |
| `insert_code` | Code insertion (VS Code only stub) |

## Build and Run
```bash
dotnet build DesktopAgent.csproj
dotnet run --project DesktopAgent.csproj
```
**Prerequisite:** Ollama must be running.

## UI Color Coding
- **THINKING:** Blue (3, 105, 161)
- **TOOL_CALL:** Indigo (79, 70, 229)
- **TOOL_RESULT:** Green (22, 101, 52)
- **RESPONSE:** Dark gray (30, 41, 59)
- **ERROR:** Red (185, 28, 28)
- **SYSTEM:** Orange (180, 83, 9)
