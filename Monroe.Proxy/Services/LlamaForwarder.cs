using Microsoft.AspNetCore.Mvc;
using Monroe.Config;
using System.Text.Json;

namespace Monroe.Services;

public class LlamaForwarder {
    private readonly IHttpClientFactory _factory;

    public LlamaForwarder(IHttpClientFactory factory) {
        _factory = factory;
    }

    public async Task<IActionResult> ForwardAsync(string path, JsonElement payload) {
        var client = _factory.CreateClient("backend");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{path}") { //{MonroeConfig.LlamaUrl}
            Content = new StringContent(payload.GetRawText(), System.Text.Encoding.UTF8, "application/json")
        };

        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
        var text = await response.Content.ReadAsStringAsync();

        return new ContentResult {
            Content = text,
            ContentType = contentType,
            StatusCode = (int)response.StatusCode
        };
    }
}