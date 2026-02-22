using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Monroe.Services;

public class LlamaForwarder(IHttpClientFactory factory) {

    public async Task<IActionResult> ForwardAsync(string path, JsonElement payload) {
        var client = factory.CreateClient("backend");

        var request = new HttpRequestMessage(HttpMethod.Post, $"{path}") { 
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