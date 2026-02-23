# DesktopAgent - Project Guide

## Overview
Windows Forms tabanlı bir AI masaüstü ajan uygulaması. Ollama üzerinden yerel LLM modelleri ile çalışır. Kullanıcı doğal dilde görev verir, ajan araçları (tool) kullanarak dosya okuma/yazma, terminal komutu çalıştırma, dosya arama gibi işlemleri otonom şekilde yürütür.

## Tech Stack
- **Framework:** .NET 9.0 (net9.0-windows)
- **UI:** Windows Forms (WinForms)
- **Dil:** C# 12+ (nullable enabled, implicit usings)
- **LLM:** Ollama (varsayılan: http://localhost:11434)
- **Paketler:** Serilog (loglama), Newtonsoft.Json, System.Text.Json

## Project Structure
```
DesktopAgent/
├── Program.cs                    # Uygulama giriş noktası, Serilog yapılandırması
├── AgentForm.cs                  # Ana UI formu ve kullanıcı etkileşimi
├── AgentForm.Designer.cs         # WinForms tasarımcı kodu
├── Services/
│   ├── AgentService.cs           # Ajanın çalışma döngüsü (agentic loop)
│   ├── ILLMClient.cs             # LLM istemci arayüzü
│   ├── OllamaClient.cs           # Ollama HTTP API istemcisi
│   └── Tools/
│       ├── BasicTools.cs         # Tüm araç implementasyonları (9 araç)
│       ├── ListToolsTool.cs      # Araç listeleme aracı
│       └── ToolRegistry.cs       # Araç kaydı ve yönlendirmesi
├── Utils/
│   ├── AppSettingsStore.cs       # Ayar yükleme/kaydetme (%APPDATA%)
│   ├── ProcessRunner.cs          # Komut çalıştırma (180s timeout)
│   └── WorkspaceContext.cs       # Çalışma dizini yönetimi
└── DesktopAgent.csproj           # Proje yapılandırması
```

## Architecture
- **AgentForm** → Kullanıcı mesajı alır, AgentService'e gönderir
- **AgentService** → Agentic loop: LLM'e mesaj gönderir, yanıtı parse eder, araç çağrısı varsa çalıştırır, sonucu geri besler. `[YANIT]` etiketiyle döngü biter.
- **ToolRegistry** → ITool arayüzünü implemente eden araçları kaydeder ve çalıştırır
- **OllamaClient** → Ollama REST API ile iletişim kurar

## Key Conventions
- Tüm UI metinleri ve system prompt Türkçe
- Araç çağrıları JSON formatında: `{"tool":"tool_name","args":{...}}`
- Final yanıt `[YANIT]` etiketi ile biter
- Relative path'ler workspace dizinine göre çözümlenir (PathHelper.Resolve)
- Ayarlar `%APPDATA%/OllamaWin/settings.json` dosyasında saklanır
- Varsayılan model: `qwen3-coder-32k` (AgentForm.cs:61 - hardcoded)
- Loglar: `<AppDir>/logs/desktop-agent-YYYY-MM-DD.log`

## Available Tools
| Araç | Açıklama |
|------|----------|
| `read_file` | Dosya içeriğini okur |
| `write_file` | Dosya yazar |
| `edit_file` | Dosyada bul-değiştir |
| `list_files` | Dizin içeriğini listeler |
| `run_terminal` | Terminal komutu çalıştırır |
| `search_in_files` | Regex ile dosyalarda arar |
| `create_directory` | Dizin oluşturur |
| `list_tools` | Mevcut araçları listeler |
| `get_diagnostics` | Diagnostik bilgisi (WinForms stub) |
| `get_open_file_info` | Açık dosya bilgisi (WinForms stub) |
| `insert_code` | Kod ekleme (VS Code only stub) |

## Build & Run
```bash
dotnet build DesktopAgent.csproj
dotnet run --project DesktopAgent.csproj
```
**Ön koşul:** Ollama'nın çalışır durumda olması gerekir.

## UI Color Coding
- **THINKING:** Mavi (3, 105, 161)
- **TOOL_CALL:** Indigo (79, 70, 229)
- **TOOL_RESULT:** Yeşil (22, 101, 52)
- **RESPONSE:** Koyu gri (30, 41, 59)
- **ERROR:** Kırmızı (185, 28, 28)
- **SYSTEM:** Turuncu (180, 83, 9)
