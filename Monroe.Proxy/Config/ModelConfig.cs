namespace Monroe.Config;

public class ModelConfig {
   
    public string Type { get; set; } = "";      // coder, vision, unrestricted, classifier
    public int Port { get; set; }
    public string ModelPath { get; set; } = "";
    public int ContextSize { get; set; }

  

    public RoutingRules Rules { get; set; } = new();

    public string ModelName => ExtractModelName(ModelPath);

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
    public List<string> NeverFor { get; set; } = new();
}
