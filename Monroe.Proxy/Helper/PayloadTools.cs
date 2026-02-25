using System.Text.Json;
using System.Text.Json.Nodes;

namespace Monroe.Proxy.Helper {
    public class PayloadTools {
        public static bool IsPayloadTooLarge(JsonElement payload, int maxContextTokens, int safetyMargin = 128) {
            if (!payload.TryGetProperty("messages", out var messagesElement))
                return false;

            int totalTokens = 0;

            foreach (var msg in messagesElement.EnumerateArray()) {
                string content = ExtractContent(msg);
                totalTokens += CountTokens(content);
            }

            return totalTokens > (maxContextTokens - safetyMargin);
        }

        public static JsonElement TrimPayloadToContext(JsonElement payload, int maxContextTokens, int safetyMargin = 128) {
            if (!payload.TryGetProperty("messages", out var messagesElement))
                return payload;

            var messages = messagesElement.EnumerateArray().ToList();

            // System-Prompt extrahieren
            JsonElement? systemMessage = messages
                .FirstOrDefault(m => m.GetProperty("role").GetString() == "system");

            int systemTokens = systemMessage.HasValue
                ? CountTokens(ExtractContent(systemMessage.Value))
                : 0;

            // Alle anderen Nachrichten
            var chatMessages = messages
                .Where(m => m.GetProperty("role").GetString() != "system")
                .ToList();

            int target = maxContextTokens - safetyMargin;

            var finalMessages = new List<JsonElement>();

            if (systemMessage.HasValue)
                finalMessages.Add(systemMessage.Value);

            int totalTokens = systemTokens;

            // Von hinten nach vorne kürzen
            for (int i = chatMessages.Count - 1; i >= 0; i--) {
                var msg = chatMessages[i];
                int msgTokens = CountTokens(ExtractContent(msg));

                if (totalTokens + msgTokens > target)
                    break;

                finalMessages.Add(msg);
                totalTokens += msgTokens;
            }

            finalMessages.Reverse();

            // neuen Payload bauen
            using var doc = JsonDocument.Parse(payload.GetRawText());
            var rootObj = doc.RootElement.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

            rootObj["messages"] = JsonSerializer.SerializeToElement(finalMessages);

            return JsonSerializer.SerializeToElement(rootObj);
        }

        // 1) Token-Schätzung (1 Token ≈ 4 Zeichen)
        public static int CountTokens(string text) {
            if (string.IsNullOrEmpty(text))
                return 0;

            return (int)Math.Ceiling(text.Length / 3.0);
        }

        // 2) Content robust extrahieren (String, Array, Object)
        public static string ExtractContent(JsonElement msg) {
            if (!msg.TryGetProperty("content", out var content))
                return "";

            return content.ValueKind switch {
                JsonValueKind.String => content.GetString() ?? "",
                JsonValueKind.Array => string.Join(" ", content.EnumerateArray().Select(ExtractContentFromPart)),
                JsonValueKind.Object => ExtractContentFromPart(content),
                _ => ""
            };
        }

        // 3) Einzelne Content-Parts extrahieren (für multimodale Messages)
        public static string ExtractContentFromPart(JsonElement part) {
            if (part.TryGetProperty("text", out var textProp))
                return textProp.GetString() ?? "";

            if (part.TryGetProperty("content", out var contentProp) &&
                contentProp.ValueKind == JsonValueKind.String)
                return contentProp.GetString() ?? "";

            return "";
        }

        public static JsonElement RemoveImages(JsonElement payload) {
            // 1) JsonElement → JsonNode (verlustfrei)
            JsonNode? node = JsonNode.Parse(payload.GetRawText());
            if (node == null)
                return payload;

            // 2) Chirurgisch Bildteile entfernen
            RemoveImagesInNode(node);

            // 3) JsonNode → JsonElement (verlustfrei)
            var json = node.ToJsonString();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        
        }

        public static bool ContainsBase64Image(JsonElement payload) {
            if (!payload.TryGetProperty("messages", out var messages))
                return false;

            if (messages.ValueKind != JsonValueKind.Array)
                return false;

            foreach (var msg in messages.EnumerateArray()) {
                if (!msg.TryGetProperty("content", out var content))
                    continue;

                string text = content.ToString();

                if (text.Contains("data:image/", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static bool IsImageRequest(JsonElement payload) {
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
        private static void RemoveImagesInNode(JsonNode node) {
            if (node is not JsonObject obj)
                return;

            if (!obj.TryGetPropertyValue("messages", out var messagesNode))
                return;

            if (messagesNode is not JsonArray messages)
                return;

            foreach (var msgNode in messages) {
                if (msgNode is not JsonObject msgObj)
                    continue;

                if (!msgObj.TryGetPropertyValue("content", out var contentNode))
                    continue;

                // content = string → nichts tun
                if (contentNode is JsonValue)
                    continue;

                // content = Array → Bildteile entfernen
                if (contentNode is JsonArray contentArray) {
                    for (int i = contentArray.Count - 1; i >= 0; i--) {
                        if (contentArray[i] is not JsonObject part)
                            continue;

                        // 1) Typ-basiert: input_image / image_url
                        if (part.TryGetPropertyValue("type", out var typeNode)) {
                            var type = typeNode?.ToString();
                            if (type == "input_image" || type == "image_url") {
                                contentArray.RemoveAt(i);
                                continue;
                            }
                        }

                        // 2) Fallback: alles mit image_url / image_base64-Property
                        if (part.TryGetPropertyValue("image_url", out _) ||
                            part.TryGetPropertyValue("image_base64", out _)) {
                            contentArray.RemoveAt(i);
                        }
                    }

                    if (contentArray.Count == 0) {
                        msgObj["content"] = "";
                    }
                }
            }
        }
        }
    }
