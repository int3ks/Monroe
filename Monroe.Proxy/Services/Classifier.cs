using Monroe.Config;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace Monroe.Services;

public class Classifier {
    private readonly HttpClient http;
    private readonly List<ModelConfig> models;
    private readonly IConfiguration config;

    public ModelConfig Model;

    public Classifier(IConfiguration config, IHttpClientFactory factory, List<ModelConfig> models, ModelConfig classiferModel) {
        http = factory.CreateClient("llama");
        this.models = models;
        this.config = config;

        Model = classiferModel;
    }

    public async Task<ModelConfig> DecideBackendAsync(JsonElement payload) {
        // 1) User message extrahieren (robust)
        string userMessage = ExtractUserMessage(payload);
        var maxlen = 500;// Model.ContextSize * 3;
        if (userMessage.Length > maxlen) {
            var start = userMessage.Length - maxlen;
            userMessage = userMessage.Substring(start);
        }

        // 2) Modellliste für den Classifier bauen
        string modelList = BuildModelListForClassifier();

        // 3) Prompt für den Classifier
        string classifierPrompt = @$"
You are a User Prompt classifier.
Choose the most suitable model for the user prompt.
Return ONLY One model name from the list below.
Return ONLY the most suitable one.
Output EXACTLY one Word with the Name of the choosen Model.

{modelList}

User Prompt:
{userMessage}
".Trim();

        // 4) Classifier-Modell befragen
        var request = new {
            model = Path.GetFileNameWithoutExtension(Model.ModelName),
            temperature = 0,
            top_p = 1,
            top_k = 1,
            max_tokens = 5,
            messages = new[]
            {
                new { role = "user", content = classifierPrompt }
            }
        };

        var BaseUrl = config["BaseUrl"]!;
        var response = await http.PostAsJsonAsync(
            $"{Model.ApiUrl(BaseUrl)}/v1/chat/completions",
            request
        );

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var debugresult = await response.Content.ReadAsStringAsync();
        string modelname = "coder";
        try {
            string result = json
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString()?
                .Trim()
                .ToLower() ?? "";

            modelname = ExtractModelTypes(result);
        } catch (Exception ex) {
            Debug.WriteLine(ex.Message);
        }

        // 5) Passendes ModelConfig zurückgeben
        return models.FirstOrDefault(m =>
            m.Type.Equals(modelname, StringComparison.OrdinalIgnoreCase)
        ) ?? models.First(m => m.Type == "coder"); // Fallback
    }



    private string ExtractModelTypes(string raw) {
        raw = raw.ToLower();
        string[] refusalPatterns = {
            "i can't",
            "i cannot",
            "i cannot help",
            "i'm not allowed",
            "i am not allowed",
            "i cannot assist",
            "i'm unable",
            "i am unable",
            "i cannot comply",
            "i can't comply"
        };
        bool refused = refusalPatterns.Any(p =>
            raw.Contains(p, StringComparison.OrdinalIgnoreCase)
        );
        if (refused)
            return "nsfw";

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
            sb.AppendLine($"Modelname: {m.Type}");
            sb.AppendLine($"Select this model for: {string.Join(", ", m.Rules.UseFor)}.");
            sb.AppendLine();
        }
        return sb.ToString().Trim();

    }


}