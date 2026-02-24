using System.Text;
using System.Text.Json;

namespace Monroe.Services;

public class LlamaStreamingForwarder {
    private readonly IHttpClientFactory _factory;

    public LlamaStreamingForwarder(IHttpClientFactory factory) {
        _factory = factory;
    }
    public async Task StreamAsync(HttpContext context, string path, JsonElement payload, string routedModel) {
        var client = _factory.CreateClient("backend");

        var request = new HttpRequestMessage(HttpMethod.Post, path) {
            Content = new StringContent(payload.GetRawText(), Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.Headers.ContentType = "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        await using var backendStream = await response.Content.ReadAsStreamAsync(context.RequestAborted);
        using var reader = new StreamReader(backendStream);
        await using var writer = new StreamWriter(context.Response.Body);

        string? lastJsonChunk = null;

        // 🔥 1. Backend-Chunks 1:1 weiterleiten, aber letztes JSON-Chunk merken
        while (!reader.EndOfStream) {
            var line = await reader.ReadLineAsync();
            if (line is null)
                continue;

            if (line.StartsWith("data: {"))
                lastJsonChunk = line;

            await writer.WriteLineAsync(line);
            await writer.FlushAsync();
        }

        // 🔥 2. Jetzt erweitern wir das letzte Chunk um routed_model
        if (lastJsonChunk != null) {
            var json = lastJsonChunk.Substring("data: ".Length);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // Prüfen, ob Timings existieren
            JsonElement timings;
            bool hasTimings = root.TryGetProperty("timings", out timings);

            Dictionary<string, object> newTimings = new();

            if (hasTimings) {
                // existierende Timings übernehmen
                foreach (var p in timings.EnumerateObject())
                    newTimings[p.Name] = p.Value.ValueKind switch {
                        JsonValueKind.Number => p.Value.GetDouble(),
                        JsonValueKind.String => p.Value.GetString()!,
                        _ => p.Value.ToString()!
                    };
            }

            // 🔥 Routed Model hinzufügen
            newTimings["routed_model"] = routedModel;

            // Neues finales Chunk bauen
            var finalChunk = new {
                choices = new[]
                {
                new
                {
                    index = 0,
                    finish_reason = "stop",
                    delta = new { }
                }
            },
                created = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                id = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : "monroe-final",
                model = root.TryGetProperty("model", out var modelProp) ? modelProp.GetString() : routedModel,
                system_fingerprint = "monroe-router",
                @object = "chat.completion.chunk",
                timings = newTimings
            };

            await writer.WriteLineAsync("data: " + JsonSerializer.Serialize(finalChunk));
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        // 🔥 3. DONE senden
        await writer.WriteLineAsync("data: [DONE]");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
    }
    
}
