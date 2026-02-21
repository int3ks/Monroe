using Monroe.Config;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Monroe.Services;

public class ModelRouter {
    private readonly Classifier _classifier;

    public ModelRouter(Classifier classifier) {
        _classifier = classifier;
    }

    public string Route(JsonElement payload) {
        string prompt = ExtractPrompt(payload);

        return MonroeConfig.ClassifierUrl;

        // 0) CLASSIFIER (Tags, später LLM)
        if (_classifier.TryClassifyByTags(payload, out string backend))
            return backend;

        // 1) Vision NUR wenn ein Bild vorhanden ist
        if (ContainsBase64Image(payload))
            return MonroeConfig.VisionUrl;

        // 2) Coder-Erkennung
        if (IsCoderPrompt(prompt))
            return MonroeConfig.CoderUrl;

        // 3) General (falls gesetzt)
        if (!string.IsNullOrWhiteSpace(MonroeConfig.GeneralUrl))
            return MonroeConfig.GeneralUrl;

        // 4) Fallback → Coder
        return MonroeConfig.CoderUrl;
    }

    private bool TryRouteByTags(JsonElement payload, out string backend) {
        backend = "";

        if (!payload.TryGetProperty("tags", out var tags))
            return false;

        if (tags.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var tag in tags.EnumerateArray()) {
            string t = tag.GetString() ?? "";

            if (t.Equals("vision", StringComparison.OrdinalIgnoreCase)) {
                backend = MonroeConfig.VisionUrl;
                return true;
            }

            if (t.Equals("coder", StringComparison.OrdinalIgnoreCase)) {
                backend = MonroeConfig.CoderUrl;
                return true;
            }

            if (t.Equals("general", StringComparison.OrdinalIgnoreCase)) {
                backend = !string.IsNullOrWhiteSpace(MonroeConfig.GeneralUrl)
                    ? MonroeConfig.GeneralUrl
                    : MonroeConfig.CoderUrl; // fallback
                return true;
            }

            if (t.Equals("unrestricted", StringComparison.OrdinalIgnoreCase)) {
                backend = MonroeConfig.UnrestrictedUrl;
                return true;
            }
        }

        return false;
    }

    private bool IsCoderPrompt(string prompt) {
        string[] codeKeywords =
        {
            "code", "programmieren", "source code", "snippet",
            "function", "class", "api", "debug", "stacktrace",
            "c#", "python", "javascript", "java", "rust", "go"
        };

        foreach (var key in codeKeywords) {
            if (prompt.Contains(key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }



    // ------------------------------------------------------------
    // VISION-ERKENNUNG
    // ------------------------------------------------------------
    private bool IsVisionPrompt(JsonElement payload, string prompt) {
        // 1) Base64-Bilder erkennen (OpenWebUI Vision)
        if (ContainsBase64Image(payload))
            return true;

        // 2) Vision-Keywords
        string[] visionKeywords =
        {
            "screenshot", "screen shot", "photo", "picture", "image",
            "what is in this", "what's in this", "describe this",
            "ocr", "text in the image", "analyse the screenshot",
            "auf dem bild", "auf dem screenshot", "was ist auf dem bild",
            "erkennst du", "siehst du", "foto"
        };

        foreach (var key in visionKeywords) {
            if (prompt.Contains(key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private bool ContainsBase64Image(JsonElement payload) {
        if (!payload.TryGetProperty("messages", out var messages))
            return false;

        foreach (var msg in messages.EnumerateArray()) {
            if (!msg.TryGetProperty("content", out var content))
                continue;

            string text = content.ToString();

            // Base64-Bild-Erkennung
            if (Regex.IsMatch(text, @"data:image\/(png|jpg|jpeg);base64,", RegexOptions.IgnoreCase))
                return true;
        }

        return false;
    }

    // ------------------------------------------------------------
    // Bildgenerierung (optional)
    // ------------------------------------------------------------
    private bool IsImageGenerationPrompt(string prompt) {
        string[] imageGenKeywords =
        {
            "generate an image", "erstelle ein bild", "male mir",
            "zeichne", "render", "create an image", "logo design"
        };

        foreach (var key in imageGenKeywords) {
            if (prompt.Contains(key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }


    public string RouteFromModel(string model) {
        if (string.IsNullOrWhiteSpace(model))
            return "http://localhost:8080";

        if (model.Contains("whisper", StringComparison.OrdinalIgnoreCase))
            return "http://localhost:8083"; // Beispiel: Whisper-Backend

        return "http://localhost:8080";
    }

    private string ExtractPrompt(JsonElement payload) {
        if (!payload.TryGetProperty("messages", out var messages))
            return "";

        if (messages.ValueKind != JsonValueKind.Array)
            return "";

        var last = messages.EnumerateArray().LastOrDefault();

        if (last.ValueKind == JsonValueKind.Object &&
            last.TryGetProperty("content", out var content)) {
            return content.GetString() ?? "";
        }

        return "";
    }
}