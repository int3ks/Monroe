using Monroe.Config;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Monroe.Services;

public class Classifier {
    private readonly HttpClient http;
    private readonly List<ModelConfig> models;
    private readonly IConfiguration config;

    public Classifier(IConfiguration config, IHttpClientFactory factory, List<ModelConfig> models) {
        http = factory.CreateClient("llama");
        this.models = models;
        this.config = config;
    }

    public async Task<ModelConfig> DecideBackendAsync(JsonElement payload) {
        // 1) User message extrahieren (robust)
        string userMessage = ExtractUserMessage(payload);

        // 2) Modellliste für den Classifier bauen
        string modelList = BuildModelListForClassifier();

        // 3) Prompt für den Classifier
        string classifierPrompt = @$"
You are a routing classifier.
Return ONLY the model name from the list below.
No explanations. No sentences. No punctuation.
Valid model names:
{modelList}

User request:
{userMessage}
".Trim();

        // 4) Classifier-Modell befragen
        string chosenName = await AskClassifierAsync(classifierPrompt);

        // 5) Passendes ModelConfig zurückgeben
        return models.FirstOrDefault(m =>
            m.Type.Equals(chosenName, StringComparison.OrdinalIgnoreCase)
        ) ?? models.First(m => m.Type == "coder"); // Fallback
    }




    private string ExtractModelTypes(string raw) {
        raw = raw.ToLower();

        foreach (var m in models) {
            if (raw.Contains(m.Type.ToLower()))
                return m.Type;
        }

        return models.First(m => m.Type == "coder").Type;
    }
    private string ExtractUserMessage(JsonElement payload) {
        if (!payload.TryGetProperty("messages", out var messages))
            return "";

        JsonElement lastMessage;

        if (messages.ValueKind == JsonValueKind.Array) {
            int len = messages.GetArrayLength();
            lastMessage = messages[len - 1];
        } else {
            lastMessage = messages;
        }

        if (!lastMessage.TryGetProperty("content", out var content))
            return "";

        // 🔥 Fall 1: content ist ein String
        if (content.ValueKind == JsonValueKind.String)
            return content.GetString() ?? "";

        // 🔥 Fall 2: content ist ein Array (z.B. Text + Screenshot)
        if (content.ValueKind == JsonValueKind.Array) {
            foreach (var part in content.EnumerateArray()) {
                if (part.TryGetProperty("type", out var typeProp) &&
                    typeProp.GetString() == "input_text" &&
                    part.TryGetProperty("text", out var textProp)) {
                    return textProp.GetString() ?? "";
                }
            }
        }

        return "";
    }

    private string BuildModelListForClassifier() {
        var sb = new StringBuilder();

        // 1. Modellnamen (ohne classifier)
        foreach (var m in models) {
            sb.AppendLine(m.Type);
        }

        sb.AppendLine();
        sb.AppendLine("Routing rules:");

        // 2. Regeln pro Modell
        foreach (var m in models) {
            // UseFor
            if (m.Rules.UseFor.Any()) {
                sb.AppendLine(
                    $"Use \"{m.Type}\" only for: {string.Join(", ", m.Rules.UseFor)}.");
            }

            // NeverFor
            if (m.Rules.NeverFor.Any()) {
                sb.AppendLine(
                    $"Never use \"{m.Type}\" for: {string.Join(", ", m.Rules.NeverFor)}.");
            }

            sb.AppendLine();
        }

        return sb.ToString().Trim();
    }

    private async Task<string> AskClassifierAsync(string prompt) {
        var request = new {
            model = "classifier",
            messages = new[]
            {
                new { role = "system", content = "Return ONLY the model name." },
                new { role = "user", content = prompt }
            }
        };

        var BaseUrl = config["BaseUrl"];
        var classifierPort = config["ClassifierPort"];
        var response = await http.PostAsJsonAsync(
            $"{BaseUrl}:{classifierPort}/v1/chat/completions",
            request
        );

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        string result = json
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?
            .Trim()
            .ToLower() ?? "";

        var modelname = ExtractModelTypes(result);
        return modelname;
    }
}