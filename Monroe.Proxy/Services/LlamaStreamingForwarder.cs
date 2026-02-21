using System.Text.Json;
using Monroe.Config;

namespace Monroe.Services;

public class LlamaStreamingForwarder {
    private readonly IHttpClientFactory _factory;

    public LlamaStreamingForwarder(IHttpClientFactory factory) {
        _factory = factory;
    }

    public async Task StreamAsync(HttpContext context, string path, JsonElement payload) {
        var client = _factory.CreateClient("backend");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{path}") { //{MonroeConfig.LlamaUrl}
            Content = new StringContent(payload.GetRawText(), System.Text.Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);

        // Status + Header 1:1 übernehmen
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.Headers.ContentType = response.Content.Headers.ContentType?.ToString() ?? "text/event-stream";
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers.Connection = "keep-alive";

        // Body 1:1 durchleiten
        await using var backendStream = await response.Content.ReadAsStreamAsync(context.RequestAborted);
        await backendStream.CopyToAsync(context.Response.Body, context.RequestAborted);
        await context.Response.Body.FlushAsync(context.RequestAborted);
    }
}