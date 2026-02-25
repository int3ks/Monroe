using System.Text;
using System.Text.Json;

namespace Monroe.Config;

public class ModelConfig {
    //appsettings.json properties
    public string Type { get; set; } = "";  
    public int Port { get; set; }
    public string ModelPath { get; set; } = "";
    public int ContextSize { get; set; }
    public string? RemoteHost { get; set; } // Optional, default: null
    public int RemotePort { get; set; } // Optional, default: 0
    public RoutingRules Rules { get; set; } = new();

    //public properties
    public string? RemoteModelName { get; set; }

    public string ApiUrl(string baseUrl) {
        if (RemoteHost == null) {
            return $"{baseUrl}:{Port}";
        }
         else {
            return $"{RemoteHost}:{RemotePort}";
        }
    }
    public string ModelName { get {
            if (RemoteModelName != null) {
                return RemoteModelName;
            }
            return ExtractModelName(ModelPath);
        } }
    
    public async Task<string?> GetActiveRemoteModelAsync() {
        using var client = new HttpClient();

        var requestBody = new {
            model = "ignore_this", 
            messages = new[]
            {
            new { role = "user", content = "ping" }
        },
            max_tokens = 1
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{RemoteHost}:{RemotePort}/v1/chat/completions", content);

        if (!response.IsSuccessStatusCode)
            return null;

        var responseJson = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        // Das ist der echte Modellname, den der Server gerade nutzt
        if (root.TryGetProperty("model", out var modelProp))
            return modelProp.GetString();

        return null;
    }

    public static string ExtractModelName(string path) {
        if (string.IsNullOrWhiteSpace(path))
            return "unknown";

        // 1. Nur Dateiname
        var file = Path.GetFileNameWithoutExtension(path);

        if (string.IsNullOrWhiteSpace(file))
            return "unknown";

        // 2. Typische Quantisierungs-Suffixe entfernen
        // Beispiele:
        // -q4_k_m
        // -q8_0
        // -q5_1
        // -f16
        // -fp16
        // -bf16
        var quantPatterns = new[]
        {
        "-q2", "-q3", "-q4", "-q5", "-q6", "-q8",
        "-f16", "-fp16", "-bf16"
    };

        foreach (var p in quantPatterns) {
            var idx = file.IndexOf(p, StringComparison.OrdinalIgnoreCase);
            if (idx > 0) {
                file = file[..idx];
                break;
            }
        }

        // 3. Falls noch ein quantisierungsähnliches Muster drin ist (z.B. q4_k_m)
        // alles nach dem letzten '-' entfernen, wenn es wie ein quant-suffix aussieht
        var lastDash = file.LastIndexOf('-');
        if (lastDash > 0) {
            var tail = file[(lastDash + 1)..];
            if (tail.StartsWith("q", StringComparison.OrdinalIgnoreCase) ||
                tail.StartsWith("f", StringComparison.OrdinalIgnoreCase) ||
                tail.StartsWith("bf", StringComparison.OrdinalIgnoreCase)) {
                file = file[..lastDash];
            }
        }

        return file.Trim();
    }

}

public class RoutingRules {
    public List<string> UseFor { get; set; } = new();
}
