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

        string? lastContentChunk = null;

        // 🔥 1. Backend-Chunks 1:1 weiterleiten, aber letztes content-Chunk merken
        while (!reader.EndOfStream) {
            var line = await reader.ReadLineAsync();

            if (line is not null) {
                // Nur Zeilen merken, die JSON enthalten
                if (line.StartsWith("data: {"))
                    lastContentChunk = line;

                await writer.WriteLineAsync(line);
                await writer.FlushAsync();
            }
        }

        // 🔥 2. Zusatz in das letzte content-Chunk injizieren (statt eigenes Event)
        if (lastContentChunk != null) {
            var text = $"\n\n---\nRouted model: {routedModel}";

            // JSON extrahieren
            var json = lastContentChunk.Substring("data: ".Length);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            // existierenden content holen
            var delta = root.GetProperty("choices")[0].GetProperty("delta");
            var existing = delta.TryGetProperty("content", out var c)
                ? c.GetString() ?? ""
                : "";

            // neuen content bauen
            var merged = existing + text;

            var newJson = JsonSerializer.Serialize(new {
                choices = new[]
                {
                    new
                    {
                        delta = new
                        {
                            role = "tool",
                            content = $"Routed model: {routedModel}"
                        }
                    }
                }
            });

            // neuen finalen Chunk senden
            await writer.WriteLineAsync($"data: {newJson}");
            await writer.WriteLineAsync();
            await writer.FlushAsync();
        }

        // 🔥 3. DONE senden
        await writer.WriteLineAsync("data: [DONE]");
        await writer.WriteLineAsync();
        await writer.FlushAsync();
    }
}
