using System.Text.Json;
using System.Text.Json.Nodes;

namespace Monroe.Proxy.Helper {
    public class PayloadTools {
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
