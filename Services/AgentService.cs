using DesktopAgent.Services.Tools;
using DesktopAgent.Utils;
using System.Runtime.CompilerServices;
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
    }

    public class AgentService
    {
        private readonly OllamaClient _ollama;
        private readonly ToolRegistry _tools;

        private const string BaseSystemPrompt = @"Sen guclu bir yazilim gelistirme ajanisin.c#,html,css,jquery kullanabiliyorsun
Arac cagirmak icin JSON kullan:
{""tool"":""tool_name"",""args"":{...}}
Final yanitini [YANIT] etiketi ile bitir.";

        public AgentService(OllamaClient ollama, ToolRegistry tools)
        {
            _ollama = ollama;
            _tools = tools;
        }

        public async IAsyncEnumerable<AgentStep> RunAsync(string model, string task, [EnumeratorCancellation] CancellationToken ct = default)
        {
            var messages = new List<ChatMessage>
            {
                new("system", BuildSystemPrompt()),
                new("user", task)
            };

            while (!ct.IsCancellationRequested)
            {
                yield return new AgentStep { Type = "thinking", Content = "Dusunuyorum..." };
                var reply = await _ollama.ChatAsync(model, messages, ct);

                if (reply.Contains("[YANIT]", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new AgentStep
                    {
                        Type = "response",
                        Content = reply.Replace("[YANIT]", string.Empty).Trim()
                    };
                    break;
                }

                if (TryParseToolCall(reply, out var toolName, out var args))
                {
                    yield return new AgentStep
                    {
                        Type = "tool_call",
                        ToolName = toolName,
                        ToolArgs = args,
                        Content = $"Arac: {toolName}"
                    };

                    var result = await _tools.ExecuteAsync(toolName, args, ct);
                    var toolList = GetToolListCsv();
                    var toolFeedback = result.Success
                        ? $"OK: {NormalizeText(result.Output, "Tool calisti ama cikti bos.")}"
                        : $"ERR: {NormalizeText(result.Error, "Bilinmeyen tool hatasi.")}\nKURAL: Bu hata cozulmeden final verme. Mevcut tool'lar: {toolList}";

                    messages.Add(new ChatMessage("assistant", reply));
                    messages.Add(new ChatMessage("tool", toolFeedback));

                    var prefix = result.Success ? "OK" : "ERR";
                    var content = result.Success
                        ? NormalizeText(result.Output, "Tool calisti ama cikti bos.")
                        : NormalizeText(result.Error, "Bilinmeyen tool hatasi.");

                    yield return new AgentStep
                    {
                        Type = "tool_result",
                        ToolName = toolName,
                        Content = $"{prefix} {content}"
                    };
                    continue;
                }

                yield return new AgentStep { Type = "response", Content = reply };
                break;
            }
        }

        private string BuildSystemPrompt()
        {
            var toolList = GetToolListCsv();
            return BaseSystemPrompt + $@"

Varsayilan workspace klasoru: {WorkspaceContext.CurrentPath}
Mevcut tool'lar: {toolList}
Kural:
- Kullanici path/dizin belirtmezse soru sorma, varsayilan workspace ile devam et.
- Relative path'leri workspace'e gore coz.
- Sadece path belirsizligi nedeniyle gereksiz teyit sorusu sorma.
- Listede olmayan tool cagirma. Bilinmeyen tool cagirirsan hata alirsin.
- Tool JSON ciktilarinda gecerli JSON escape kurallarina uy.";
        }

        private string GetToolListCsv()
        {
            var names = _tools.GetToolNames()
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return names.Length == 0 ? "(bos)" : string.Join(", ", names);
        }

        private static string NormalizeText(string? value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private bool TryParseToolCall(string text, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            if (!TryExtractFirstJsonObject(text, out var jsonObject))
                return false;

            if (!TryParseToolObject(jsonObject, out tool, out args))
                return false;

            return true;
        }

        private static bool TryParseToolObject(string jsonObject, out string tool, out JsonElement args)
        {
            tool = string.Empty;
            args = default;

            try
            {
                using var doc = JsonDocument.Parse(jsonObject);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object)
                    return false;

                if (!root.TryGetProperty("tool", out var toolProp))
                    return false;

                tool = toolProp.GetString() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tool))
                    return false;

                if (!root.TryGetProperty("args", out var argsProp))
                {
                    args = JsonSerializer.Deserialize<JsonElement>("{}");
                    return true;
                }

                if (argsProp.ValueKind == JsonValueKind.Object)
                {
                    args = JsonSerializer.Deserialize<JsonElement>(argsProp.GetRawText());
                    return true;
                }

                if (argsProp.ValueKind == JsonValueKind.String)
                {
                    var raw = argsProp.GetString() ?? "{}";
                    if (TryParseArgsJson(raw, out args))
                        return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseArgsJson(string raw, out JsonElement args)
        {
            args = default;
            try
            {
                args = JsonSerializer.Deserialize<JsonElement>(raw);
                return true;
            }
            catch
            {
                var sanitized = Regex.Replace(raw, @"\\(?![""\\/bfnrtu])", @"\\\\");
                try
                {
                    args = JsonSerializer.Deserialize<JsonElement>(sanitized);
                    return true;
                }
                catch
                {
                    return false;
                }
            }
        }

        private static bool TryExtractFirstJsonObject(string text, out string jsonObject)
        {
            jsonObject = string.Empty;
            var start = text.IndexOf('{');
            if (start < 0)
                return false;

            var depth = 0;
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
                    depth++;
                    continue;
                }

                if (ch == '}')
                {
                    depth--;
                    if (depth == 0)
                    {
                        jsonObject = text.Substring(start, i - start + 1);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
