# DesktopAgent

Ollama destekli, Windows masaüstü AI ajan uygulaması. Doğal dilde verilen görevleri anlayıp, dosya işlemleri, terminal komutları ve arama gibi araçları otonom şekilde kullanarak yerine getirir.

![.NET](https://img.shields.io/badge/.NET-9.0-blue)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)
![LLM](https://img.shields.io/badge/LLM-Ollama-green)

## Özellikler

- **Ajansal AI Döngüsü** — LLM, görevi tamamlayana kadar araçları iteratif olarak kullanır
- **11 Yerleşik Araç** — Dosya okuma/yazma/düzenleme, terminal, dosya arama, dizin listeleme ve daha fazlası
- **Gerçek Zamanlı Çıktı** — Renk kodlu çıktı ile düşünme, araç çağrısı, sonuç ve yanıt adımları
- **Yerel LLM** — Ollama ile tamamen yerel çalışır, veri dışarı çıkmaz
- **İptal Desteği** — Çalışan görevler kullanıcı tarafından iptal edilebilir
- **Ayarlar Paneli** — Uygulama içinden Ollama URL, model seçimi, workspace dizini ve system prompt yapılandırılabilir

## Gereksinimler

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Ollama](https://ollama.com/) (yerel olarak çalışır durumda)
- Windows 10/11

## Kurulum

```bash
# Repoyu klonla
git clone <repo-url>
cd DesktopAgent

# Derle
dotnet build DesktopAgent.csproj

# Çalıştır
dotnet run --project DesktopAgent.csproj
```

> Ollama'nın `http://localhost:11434` adresinde çalışır durumda olduğundan emin olun.

## Kullanım

1. Uygulamayı başlatın
2. Metin kutusuna görevinizi yazın (ör: *"Bir ASP.NET Web API projesi oluştur"*)
3. Enter'a basın veya **Gönder** butonuna tıklayın
4. Ajan düşünecek, gerekli araçları çağıracak ve sonucu gösterecektir

### Ayarlar Paneli

Sağ üst köşedeki **⚙** butonuna tıklayarak ayarlar panelini açabilirsiniz:

| Alan | Açıklama |
|------|----------|
| **Ollama URL** | Ollama sunucu adresi (ör: `http://localhost:11434`). "Listele" butonu ile bağlantıyı test edip modelleri çekebilirsiniz. |
| **Model** | Ollama'dan çekilen model listesinden seçim yapılır. Seçilen model tüm sonraki görevlerde kullanılır. |
| **Workspace** | Ajanın çalışacağı varsayılan dizin. "Gözat..." ile klasör seçilir. |
| **System Prompt** | Ajana verilen temel talimat metni. Ajanın davranışını, bildiği dilleri ve araç kullanım formatını belirler. |

Ayarlar **Kaydet** ile `%APPDATA%/OllamaWin/settings.json` dosyasına kalıcı olarak yazılır ve anında uygulanır.

### Çıktı Renk Kodları

| Renk | Anlam |
|------|-------|
| Mavi | Düşünme / Planlama |
| Indigo | Araç Çağrısı |
| Yeşil | Araç Sonucu |
| Koyu Gri | Final Yanıt |
| Kırmızı | Hata |
| Turuncu | Sistem Mesajı |

## Araçlar

| Araç | Açıklama | Parametreler |
|------|----------|-------------|
| `read_file` | Dosya içeriğini okur | `path` |
| `write_file` | Dosya yazar | `path`, `content` |
| `edit_file` | Bul-değiştir yapar | `path`, `search`, `replace` |
| `list_files` | Dizin içeriğini listeler | `path?`, `recursive?` |
| `run_terminal` | Terminal komutu çalıştırır | `command`, `cwd?` |
| `search_in_files` | Regex ile dosyalarda arar | `query`, `include?` |
| `create_directory` | Dizin oluşturur | `path` |
| `list_tools` | Mevcut araçları listeler | — |

## Yapılandırma

Ayarlar `%APPDATA%/OllamaWin/settings.json` dosyasında saklanır:

```json
{
  "OllamaBaseUrl": "http://localhost:11434",
  "WorkspacePath": "D:\\AI\\llmtest",
  "SelectedModel": "qwen3-coder-32k",
  "SystemPrompt": "Sen guclu bir yazilim gelistirme ajanisin..."
}
```

Tüm ayarlar uygulama içindeki ayarlar panelinden de değiştirilebilir.

## Mimari

```
Kullanıcı → AgentForm → AgentService → OllamaClient → Ollama LLM
                              ↕
                        ToolRegistry → BasicTools (dosya, terminal, arama...)
```

**Ajansal döngü:**
1. Kullanıcı görev girer
2. AgentService, system prompt + görev ile LLM'e istek atar
3. LLM araç çağrısı (`{"tool":"...", "args":{...}}`) veya final yanıt (`[YANIT]`) döner
4. Araç çağrısı varsa: ToolRegistry çalıştırır, sonucu LLM'e geri besler
5. `[YANIT]` gelene kadar döngü devam eder

## Proje Yapısı

```
DesktopAgent/
├── Program.cs                 # Giriş noktası, loglama
├── AgentForm.cs               # UI, kullanıcı etkileşimi ve ayarlar paneli
├── AgentForm.Designer.cs      # WinForms tasarımcı kodu
├── Services/
│   ├── AgentService.cs        # Ajansal döngü (system prompt dışarıdan ayarlanabilir)
│   ├── ILLMClient.cs          # LLM istemci arayüzü
│   ├── OllamaClient.cs        # Ollama API istemcisi
│   └── Tools/
│       ├── BasicTools.cs      # Araç implementasyonları
│       ├── ListToolsTool.cs   # Araç listeleme
│       └── ToolRegistry.cs    # Araç kaydı
├── Utils/
│   ├── AppSettingsStore.cs    # Ayar yönetimi (URL, model, workspace, system prompt)
│   ├── ProcessRunner.cs       # Komut çalıştırma
│   └── WorkspaceContext.cs    # Workspace yönetimi
└── DesktopAgent.csproj        # .NET 9.0 proje dosyası
```

## Lisans

Bu proje özel kullanım içindir.
