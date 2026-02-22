using Monroe.Config;
using System.Text.Json;

namespace Monroe.Services;

public class ModelRouter {
    private readonly Classifier _classifier;
    private readonly List<ModelConfig> _models;

    public ModelRouter(Classifier classifier, List<ModelConfig> models) {
        _classifier = classifier;
        _models = models;
    }

    public async Task<ModelConfig> RouteAsync(JsonElement payload) {
        // 1) Vision-Hardcheck: Wenn ein Bild drin ist → Vision-Modell erzwingen
        var visionModel = _models.FirstOrDefault(m => m.Type.Equals("vision"));
        // if (visionModel != null && ContainsBase64Image(payload))
        //    return visionModel;
        

        if (IsImageRequest(payload)) {
            return visionModel;

        }

        // 2) Classifier entscheidet dynamisch
        var model = await _classifier.DecideBackendAsync(payload);

        // 3) Fallback (sollte nie passieren)
        return model ?? _models.First(m => m.Type == "coder");
    }

    public static JsonElement RemoveImages(JsonElement payload) {
        using var doc = JsonDocument.Parse(payload.GetRawText());
        var root = doc.RootElement;

        var messages = new List<object>();

        foreach (var msg in root.GetProperty("messages").EnumerateArray()) {
            var role = msg.GetProperty("role").GetString();

            // content kann String ODER Array sein
            if (msg.TryGetProperty("content", out var content)) {
                if (content.ValueKind == JsonValueKind.String) {
                    // Klassischer Text → unverändert übernehmen
                    messages.Add(new {
                        role,
                        content = content.GetString()
                    });
                } else if (content.ValueKind == JsonValueKind.Array) {
                    // Multimodal → Bildteile entfernen
                    var newParts = new List<object>();

                    foreach (var part in content.EnumerateArray()) {
                        if (part.TryGetProperty("type", out var typeProp)) {
                            var type = typeProp.GetString();

                            if (type == "input_image") {
                                // Bild entfernen
                                continue;
                            }

                            // Text oder andere Teile übernehmen
                            newParts.Add(new {
                                type,
                                text = part.TryGetProperty("text", out var textProp)
                                    ? textProp.GetString()
                                    : null
                            });
                        }
                    }

                    // Wenn nach dem Entfernen keine Teile übrig sind → content = ""
                    if (newParts.Count == 0) {
                        messages.Add(new {
                            role,
                            content = ""
                        });
                    } else {
                        messages.Add(new {
                            role,
                            content = newParts
                        });
                    }
                } else {
                    // Unbekannter Typ → unverändert übernehmen
                    messages.Add(new {
                        role,
                        content = content.GetRawText()
                    });
                }
            } else {
                // Falls keine content-Property existiert → unverändert übernehmen
                messages.Add(new {
                    role,
                    content = ""
                });
            }
        }

        // Neues Payload bauen
        var newPayload = new {
            model = root.TryGetProperty("model", out var modelProp) ? modelProp.GetString() : null,
            messages = messages
        };

        // In JsonElement zurückwandeln
        var json = JsonSerializer.Serialize(newPayload);
        using var finalDoc = JsonDocument.Parse(json);
        return finalDoc.RootElement.Clone();
    }

    bool IsImageRequest(JsonElement payload) {
        if (!payload.TryGetProperty("messages", out var messages) ||
            messages.ValueKind != JsonValueKind.Array)
            return false;

        var last = messages[messages.GetArrayLength() - 1];

        if (!last.TryGetProperty("role", out var role) ||
            role.GetString() != "user")
            return false;

        if (!last.TryGetProperty("content", out var content))
            return false;

        // Fall 1: klassischer Text
        if (content.ValueKind == JsonValueKind.String)
            return false;

        // Fall 2: multimodal
        // Multimodal → Array prüfen
        if (content.ValueKind == JsonValueKind.Array) {
            foreach (var part in content.EnumerateArray()) {
                // 1) Typ-basiert
                if (part.TryGetProperty("type", out var typeProp)) {
                    var type = typeProp.GetString();
                    if (type == "input_image" || type == "image_url")
                        return true;
                }

                // 2) Fallback: Properties erkennen
                if (part.TryGetProperty("image_url", out _) ||
                    part.TryGetProperty("image_base64", out _)) {
                    return true;
                }
            }
        }

        return false;
    }
    
}