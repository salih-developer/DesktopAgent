using DesktopAgent.Services.Tools;
using DesktopAgent.Utils;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DesktopAgent.Services
{
    public class AgentStep
    {
        public string Type { get; set; } = "thinking";
        public string Content { get; set; } = string.Empty;
        public string? ToolName { get; set; }
        public object? ToolArgs { get; set; }
        /// <summary>Full/raw content for reports — not shown in UI.</summary>
        public string? Detail { get; set; }
    }

    public class AgentService
    {
        private readonly OllamaClient _ollama;
        private readonly ToolRegistry _tools;
        private string _baseSystemPrompt;

        public AgentService(OllamaClient ollama, ToolRegistry tools, string? baseSystemPrompt = null)
        {
            _ollama = ollama;
            _tools = tools;
            _baseSystemPrompt = baseSystemPrompt ?? @"You are a powerful software development agent. You can use C#, HTML, CSS, and jQuery.
                    Use JSON to call tools:
                    {""tool"":""tool_name"",""args"":{...}}
                    End your final answer with the [DONE] tag.";
        }

        public void SetSystemPrompt(string prompt)
        {
            _baseSystemPrompt = prompt;
        }

        public async Task RunAsync(string model, string task, Action<AgentStep> onStep, CancellationToken ct = default)
        {
            var messages = new List<ChatMessage>
            {
                new("system", BuildSystemPrompt()),
                new("user", task)
            };

            var iteration = 0;
            while (!ct.IsCancellationRequested)
            {
                iteration++;
                onStep(new AgentStep
                {
                    Type = "thinking",
                    Content = $"Thinking... (step {iteration})"
                });

                var reply = await _ollama.ChatAsync(model, messages, ct);

                // Emit raw LLM reply for report capture (not shown in UI).
                var replyPreview = reply.Length > 160 ? reply[..160] + "…" : reply;
                onStep(new AgentStep
                {
                    Type    = "llm_reply",
                    Content = replyPreview,
                    Detail  = reply
                });

                if (TryParseAllToolCalls(reply, out var toolCalls))
                {
                    var feedbackParts = new List<string>();
                    var toolList = GetToolListCsv();

                    foreach (var (toolName, args) in toolCalls)
                    {
                        var argsJson = JsonSerializer.Serialize(args, new JsonSerializerOptions { WriteIndented = true });
                        onStep(new AgentStep
                        {
                            Type     = "tool_call",
                            ToolName = toolName,
                            ToolArgs = args,
                            Content  = $"Tool: {toolName}",
                            Detail   = argsJson
                        });

                        var result = await _tools.ExecuteAsync(toolName, args, ct);

                        var prefix  = result.Success ? "OK" : "ERR";
                        var content = result.Success
                            ? NormalizeText(result.Output, "Tool executed but output was empty.")
                            : NormalizeText(result.Error, "Unknown tool error.");

                        onStep(new AgentStep
                        {
                            Type     = "tool_result",
                            ToolName = toolName,
                            Content  = $"{prefix} {content}",
                            Detail   = result.Success ? result.Output : result.Error
                        });

                        var feedback = result.Success
                            ? $"OK: {NormalizeText(result.Output, "Tool executed but output was empty.")}"
                            : $"ERR: {NormalizeText(result.Error, "Unknown tool error.")}\nRULE: Resolve this error before continuing. Available tools: {toolList}";

                        feedbackParts.Add($"Tool result for '{toolName}':\n{feedback}");
                    }

                    messages.Add(new ChatMessage("assistant", reply));
                    messages.Add(new ChatMessage("user", string.Join("\n\n", feedbackParts)));
                    continue;
                }

                if (reply.Contains("[DONE]", StringComparison.OrdinalIgnoreCase))
                {
                    var finalText = reply.Replace("[DONE]", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    onStep(new AgentStep
                    {
                        Type    = "response",
                        Content = finalText,
                        Detail  = finalText
                    });
                    break;
                }

                messages.Add(new ChatMessage("assistant", reply));

                if (LooksLikeToolIntent(reply))
                {
                    var correction = $"FORMAT ERROR: Tool call JSON could not be parsed.\n" +
                                     $"Return ONLY valid JSON in one of these forms:\n" +
                                     $"{{\"tool\":\"tool_name\",\"args\":{{...}}}}\n" +
                                     $"or\n" +
                                     $"{{\"name\":\"tool_name\",\"arguments\":{{...}}}}\n" +
                                     $"Available tools: {GetToolListCsv()}\n" +
                                     $"If you are done, provide the final response and include [DONE].";

                    messages.Add(new ChatMessage("user", correction));
                    onStep(new AgentStep
                    {
                        Type = "thinking",
                        Content = "Model reply looked like a tool call but JSON was invalid. Requesting corrected tool JSON."
                    });
                    continue;
                }

                onStep(new AgentStep
                {
                    Type = "response",
                    Content = reply
                });
                break;
            }
        }

        private string BuildSystemPrompt()
        {
            var toolList = GetToolListCsv();
            return _baseSystemPrompt + $@"

Default workspace directory: {WorkspaceContext.CurrentPath}
Available tools: {toolList}

TOOL CALL FORMAT — output exactly one JSON object per response:
  {{""tool"":""run_terminal"",""args"":{{""command"":""dotnet --version"",""cwd"":""{WorkspaceContext.CurrentPath}""}}}}

Tool signatures (? = optional):
  read_file(path)
  write_file(path, content)
  edit_file(path, search, replace)  ← search is the EXACT existing text to find, replace is the new text
  list_files(path?, recursive?)
  run_terminal(command, cwd?)
  search_in_files(query, include?)
  create_directory(path)
  list_tools()

Rules:
- CRITICAL: Every tool call MUST include ALL required args inside the args object. NEVER call a tool without its required arguments.
- run_terminal ALWAYS needs the command argument. Without it the call will fail.
- create_directory, read_file, write_file, edit_file ALWAYS need the path argument.
- Make ONLY ONE tool call per response. Wait for the result before calling the next tool.
- Do NOT include [Done] in the same response as a tool call.
- IMPORTANT: Before creating new files or directories, ALWAYS list_files first to check what already exists in the workspace.
- Use existing files and directories whenever possible instead of creating new ones.
- If the user does not specify a path/directory, do not ask - use the default workspace.
- Resolve relative paths against the workspace.
- Do not ask unnecessary confirmation questions just because the path is ambiguous.
- Do not call tools that are not in the list. Calling an unknown tool will result in an error.
- Follow valid JSON escape rules in tool JSON outputs.
- For run_terminal, always set cwd to an absolute path that already exists. If unsure, omit cwd to use the default workspace.
- If list_files returns empty directory, that directory exists but has no files - do not treat it as an error.
- Keep answers short and concise, except for JSON outputs.";
        }

        private string GetToolListCsv()
        {
            var names = _tools.GetToolNames()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return names.Length == 0 ? "(none)" : string.Join(", ", names);
        }

        private static string NormalizeText(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private bool TryParseAllToolCalls(string text, out List<(string Tool, JsonElement Args)> calls)
        {
            calls = new List<(string, JsonElement)>();

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var availableTools = _tools.GetToolNames();

            // Collect all valid tool-call JSON objects found in the text.
            foreach (var candidate in ExtractJsonCandidates(text))
            {
                if (TryParseToolObject(candidate, out var t, out var a) &&
                    availableTools.Contains(t, StringComparer.OrdinalIgnoreCase))
                {
                    calls.Add((t, a));
                }
            }

            // Fallback: function-style [tool_name(...)] if no JSON calls found.
            if (calls.Count == 0 && TryParseFunctionStyleToolCall(text, out var fTool, out var fArgs))
                calls.Add((fTool, fArgs));

            return calls.Count > 0;
        }

        // Kept for LooksLikeToolIntent usage; TryParseAllToolCalls is the primary path.
        private bool TryParseToolCall(string text, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            var availableTools = _tools.GetToolNames();

            foreach (var candidate in ExtractJsonCandidates(text))
            {
                if (TryParseToolObject(candidate, out tool, out args))
                {
                    if (availableTools.Contains(tool, StringComparer.OrdinalIgnoreCase))
                        return true;
                    tool = string.Empty;
                    args = default;
                }
            }

            if (TryParseFunctionStyleToolCall(text, out tool, out args))
                return true;

            return false;
        }

        // Tools that can be called with no arguments (bracket-only format is accepted for these).
        private static readonly HashSet<string> NoRequiredArgTools = new(StringComparer.OrdinalIgnoreCase)
        {
            "list_files", "list_tools", "get_diagnostics", "get_open_file_info",
        };

        // Maps each tool's positional parameters (in order) so positional string args can be named correctly.
        private static readonly Dictionary<string, string[]> ToolPositionalParams = new(StringComparer.OrdinalIgnoreCase)
        {
            ["create_directory"] = ["path"],
            ["list_files"]       = ["path"],
            ["read_file"]        = ["path"],
            ["write_file"]       = ["path", "content"],
            ["edit_file"]        = ["path", "search", "replace"],
            ["run_terminal"]     = ["command", "cwd"],
            ["search_in_files"]  = ["query", "include"],
            ["insert_code"]      = ["path"],
        };

        private bool TryParseFunctionStyleToolCall(string text, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            if (string.IsNullOrWhiteSpace(text))
                return false;

            var availableTools = _tools.GetToolNames();

            // format: tool_name(...) or [tool_name(...)]
            foreach (Match match in Regex.Matches(text, @"(?is)\[?\s*(?<name>[a-z_][a-z0-9_]*)\s*\((?<args>.*?)\)\s*\]?", RegexOptions.IgnoreCase))
            {
                var name = match.Groups["name"].Value.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!availableTools.Contains(name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!TryParseFunctionArgs(match.Groups["args"].Value, out args, name))
                    continue;

                tool = name;
                return true;
            }

            // format: [tool_name] — bracket only, no parentheses
            // Only accepted for tools that have no required arguments.
            foreach (Match match in Regex.Matches(text, @"\[([a-z_][a-z0-9_]*)\]", RegexOptions.IgnoreCase))
            {
                var name = match.Groups[1].Value.Trim();
                if (!availableTools.Contains(name, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!NoRequiredArgTools.Contains(name, StringComparer.OrdinalIgnoreCase))
                    continue;

                args = CreateEmptyArgs();
                tool = name;
                return true;
            }

            return false;
        }

        private static bool TryParseFunctionArgs(string rawArgs, out JsonElement args, string? toolName = null)
        {
            args = default;
            if (string.IsNullOrWhiteSpace(rawArgs))
            {
                args = CreateEmptyArgs();
                return true;
            }

            // Pure JSON object: tool_name({"key":"value",...})
            var trimmed = rawArgs.Trim();
            if (trimmed.StartsWith('{') && TryParseArgsJson(trimmed, out args))
                return true;

            // Mixed / positional format:
            //   tool_name("value")
            //   tool_name("value", {"key":"val"})
            //   tool_name(key="value", ...)
            string[]? positionalParams = toolName != null && ToolPositionalParams.TryGetValue(toolName, out var pp) ? pp : null;
            var positionalIndex = 0;
            var parts = new List<string>(); // raw JSON fragments: "key":value

            foreach (var token in SplitArguments(rawArgs))
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                var tok = token.Trim();

                // JSON object token — extract and merge its properties
                if (tok.StartsWith('{'))
                {
                    if (!TryParseArgsJson(tok, out var jsonPart))
                        return false;

                    foreach (var prop in jsonPart.EnumerateObject())
                        parts.Add($"{JsonSerializer.Serialize(prop.Name)}:{prop.Value.GetRawText()}");

                    continue;
                }

                // key=value pair
                var eqIdx = tok.IndexOf('=');
                if (eqIdx > 0 && eqIdx < tok.Length - 1)
                {
                    var key = tok[..eqIdx].Trim().Trim('"', '\'');
                    var rawValue = tok[(eqIdx + 1)..].Trim();
                    if (!string.IsNullOrWhiteSpace(key))
                    {
                        parts.Add($"{JsonSerializer.Serialize(key)}:{JsonSerializer.Serialize(ParseScalar(rawValue))}");
                        continue;
                    }
                }

                // Positional string argument — map to named parameter
                if (positionalParams != null && positionalIndex < positionalParams.Length)
                {
                    var paramName = positionalParams[positionalIndex++];
                    parts.Add($"{JsonSerializer.Serialize(paramName)}:{JsonSerializer.Serialize(ParseScalar(tok))}");
                    continue;
                }

                // Cannot map this token
                return false;
            }

            if (parts.Count == 0)
            {
                args = CreateEmptyArgs();
                return true;
            }

            var json = "{" + string.Join(",", parts) + "}";
            return TryParseArgsJson(json, out args);
        }

        private static IEnumerable<string> SplitArguments(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            var start = 0;
            var depth = 0;
            var inString = false;
            var escaped = false;
            var quote = '\0';

            for (var i = 0; i < text.Length; i++)
            {
                var ch = text[i];

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (ch == '\\')
                    {
                        escaped = true;
                    }
                    else if (ch == quote)
                    {
                        inString = false;
                        quote = '\0';
                    }

                    continue;
                }

                if (ch is '"' or '\'')
                {
                    inString = true;
                    quote = ch;
                    continue;
                }

                if (ch is '(' or '[' or '{')
                {
                    depth++;
                    continue;
                }

                if (ch is ')' or ']' or '}')
                {
                    depth = Math.Max(0, depth - 1);
                    continue;
                }

                if (ch == ',' && depth == 0)
                {
                    yield return text[start..i].Trim();
                    start = i + 1;
                }
            }

            if (start < text.Length)
                yield return text[start..].Trim();
        }

        private static object? ParseScalar(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            if ((raw.StartsWith('"') && raw.EndsWith('"')) ||
                (raw.StartsWith('\'') && raw.EndsWith('\'')))
            {
                var unwrapped = raw[1..^1];
                return unwrapped
                    .Replace("\\\"", "\"", StringComparison.Ordinal)
                    .Replace("\\'", "'", StringComparison.Ordinal)
                    .Replace("\\\\", "\\", StringComparison.Ordinal);
            }

            if (raw.Equals("null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (bool.TryParse(raw, out var boolValue))
                return boolValue;

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intValue))
                return intValue;

            if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
                return doubleValue;

            return raw;
        }

        private static bool TryParseToolObject(string json, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            try
            {
                using var doc = JsonDocument.Parse(json);
                return TryParseToolElement(doc.RootElement, out tool, out args);
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseToolElement(JsonElement element, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    if (TryParseToolElement(item, out tool, out args))
                        return true;
                }

                return false;
            }

            if (element.ValueKind != JsonValueKind.Object)
                return false;

            if (TryParseToolFromObject(element, out tool, out args))
                return true;

            if (element.TryGetProperty("tool_call", out var toolCallProp) &&
                TryParseToolElement(toolCallProp, out tool, out args))
            {
                return true;
            }

            if (element.TryGetProperty("function", out var functionProp) &&
                TryParseToolElement(functionProp, out tool, out args))
            {
                return true;
            }

            return false;
        }

        private static bool TryParseToolFromObject(JsonElement root, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            if (!TryGetStringProperty(root, "tool", out tool) &&
                !TryGetStringProperty(root, "name", out tool))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(tool))
                return false;

            if (!TryGetArgsProperty(root, out args))
            {
                args = CreateEmptyArgs();
            }

            return true;
        }

        private static bool TryGetArgsProperty(JsonElement root, out JsonElement args)
        {
            args = default;
            var keys = new[] { "args", "arguments", "parameters", "input" };
            foreach (var key in keys)
            {
                if (!root.TryGetProperty(key, out var prop))
                    continue;

                if (prop.ValueKind == JsonValueKind.Object)
                {
                    args = JsonSerializer.Deserialize<JsonElement>(prop.GetRawText());
                    return true;
                }

                if (prop.ValueKind == JsonValueKind.String)
                {
                    var raw = prop.GetString() ?? "{}";
                    if (TryParseArgsJson(raw, out args))
                        return true;
                }
            }

            return false;
        }

        private static bool TryGetStringProperty(JsonElement root, string name, out string value)
        {
            value = string.Empty;
            if (!root.TryGetProperty(name, out var prop))
                return false;

            if (prop.ValueKind != JsonValueKind.String)
                return false;

            value = prop.GetString() ?? string.Empty;
            return true;
        }

        private static JsonElement CreateEmptyArgs()
        {
            return JsonSerializer.Deserialize<JsonElement>("{}");
        }

        private static bool TryParseArgsJson(string raw, out JsonElement args)
        {
            args = default;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            // Try progressively more aggressive sanitization passes.
            var candidates = new Func<string>[]
            {
                () => raw,
                () => Regex.Replace(raw, @"\\(?![""\\/bfnrtu])", @"\\\\"),
                () => SanitizeJsonControlChars(Regex.Replace(raw, @"\\(?![""\\/bfnrtu])", @"\\\\")),
            };

            foreach (var candidate in candidates)
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<JsonElement>(candidate());
                    if (parsed.ValueKind == JsonValueKind.Object)
                    {
                        args = parsed;
                        return true;
                    }
                }
                catch { /* try next */ }
            }

            return false;
        }

        /// <summary>
        /// Escapes literal control characters (newline, carriage return, tab) that appear
        /// inside JSON string values. The model sometimes outputs multi-line code blocks
        /// as raw newlines inside a JSON string, which makes the JSON invalid.
        /// </summary>
        private static string SanitizeJsonControlChars(string raw)
        {
            var sb = new StringBuilder(raw.Length + 64);
            bool inString = false;
            bool escaped = false;

            foreach (char c in raw)
            {
                if (escaped)
                {
                    escaped = false;
                    sb.Append(c);
                    continue;
                }

                if (c == '\\' && inString)
                {
                    escaped = true;
                    sb.Append(c);
                    continue;
                }

                if (c == '"')
                {
                    inString = !inString;
                    sb.Append(c);
                    continue;
                }

                if (inString)
                {
                    switch (c)
                    {
                        case '\n': sb.Append("\\n");  continue;
                        case '\r': sb.Append("\\r");  continue;
                        case '\t': sb.Append("\\t");  continue;
                        case '\b': sb.Append("\\b");  continue;
                        case '\f': sb.Append("\\f");  continue;
                    }
                }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static IEnumerable<string> ExtractJsonCandidates(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            foreach (Match match in Regex.Matches(text, @"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase))
            {
                var body = match.Groups[1].Value;
                foreach (var json in ExtractJsonValues(body))
                    yield return json;
            }

            foreach (var json in ExtractJsonValues(text))
                yield return json;
        }

        private static IEnumerable<string> ExtractJsonValues(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                yield break;

            for (var start = 0; start < text.Length; start++)
            {
                var ch = text[start];
                if (ch != '{' && ch != '[')
                    continue;

                if (TryFindJsonEnd(text, start, out var end))
                {
                    yield return text.Substring(start, end - start + 1);
                    start = end;
                }
            }
        }

        private static bool TryFindJsonEnd(string text, int start, out int end)
        {
            end = -1;
            var objectDepth = 0;
            var arrayDepth = 0;
            var inString = false;
            var escaped = false;

            for (var i = start; i < text.Length; i++)
            {
                var ch = text[i];

                if (inString)
                {
                    if (escaped)
                    {
                        escaped = false;
                    }
                    else if (ch == '\\')
                    {
                        escaped = true;
                    }
                    else if (ch == '"')
                    {
                        inString = false;
                    }
                    continue;
                }

                if (ch == '"')
                {
                    inString = true;
                    continue;
                }

                if (ch == '{')
                {
                    objectDepth++;
                }
                else if (ch == '}')
                {
                    objectDepth--;
                    if (objectDepth < 0)
                        return false;
                }
                else if (ch == '[')
                {
                    arrayDepth++;
                }
                else if (ch == ']')
                {
                    arrayDepth--;
                    if (arrayDepth < 0)
                        return false;
                }

                if (objectDepth == 0 && arrayDepth == 0)
                {
                    end = i;
                    return true;
                }
            }

            return false;
        }

        private static bool LooksLikeToolIntent(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Contains("\"tool\"", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("\"name\"", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("tool_call", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("```json", StringComparison.OrdinalIgnoreCase)
                   || Regex.IsMatch(text, @"(?is)\[?\s*[a-z_][a-z0-9_]*\s*\(.*\)\s*\]?");
        }
    }
}
