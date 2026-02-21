using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace DesktopAgent.Services
{
    public record ChatMessage(string role, string content);

    public class OllamaClient : ILLMClient
    {
        private readonly HttpClient _http;
        private string _baseUrl = "http://localhost:11434";
        private readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public OllamaClient(string baseUrl = "http://localhost:11434")
        {
            _http = new HttpClient();
            SetBaseUrl(baseUrl);
        }

        public string BaseUrl => _baseUrl;

        public void SetBaseUrl(string baseUrl)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
                throw new ArgumentException("Geçerli bir URL girin. Örn: http://localhost:11434", nameof(baseUrl));

            if (uri.Scheme is not ("http" or "https"))
                throw new ArgumentException("URL http:// veya https:// ile başlamalı.", nameof(baseUrl));

            _baseUrl = uri.GetLeftPart(UriPartial.Authority);
        }

        public async Task<bool> CheckStatusAsync()
        {
            try
            {
                var resp = await _http.GetAsync(BuildUri("/api/tags"));
                return resp.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string[]> ListModelsAsync()
        {
            var res = await _http.GetFromJsonAsync<JsonElement>(BuildUri("/api/tags"), _json);
            if (!res.TryGetProperty("models", out var models)) return Array.Empty<string>();
            return models.EnumerateArray()
                .Select(m => m.GetProperty("name").GetString() ?? string.Empty)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToArray();
        }

        public async Task<string> ChatAsync(string model, IEnumerable<ChatMessage> messages, CancellationToken ct = default)
        {
            var messageList = messages?.ToList() ?? new List<ChatMessage>();
            var request = new
            {
                model,
                messages = messageList,
                stream = false
            };

            var endpoint = BuildUri("/api/chat");
            Log.Information("Sending Ollama chat request. Endpoint: {Endpoint}, Model: {Model}, MessageCount: {MessageCount}", endpoint, model, messageList.Count);
            Log.Debug("Ollama chat request payload: {@Request}", request);

            using var resp = await _http.PostAsJsonAsync(endpoint, request, _json, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var errorBody = await resp.Content.ReadAsStringAsync(ct);
                Log.Error("Ollama chat request failed. StatusCode: {StatusCode}, Body: {Body}", (int)resp.StatusCode, errorBody);
                resp.EnsureSuccessStatusCode();
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(options: _json, cancellationToken: ct);
            return json.GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
        }

        private Uri BuildUri(string path)
        {
            var normalizedPath = path.StartsWith('/') ? path : "/" + path;
            return new Uri($"{_baseUrl}{normalizedPath}");
        }
    }

}
