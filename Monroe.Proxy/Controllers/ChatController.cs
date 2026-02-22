using Microsoft.AspNetCore.Mvc;
using Monroe.Proxy.Helper;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1/chat")]
public class ChatController(IConfiguration config, LlamaForwarder forwarder, LlamaStreamingForwarder streamer, ModelRouter router) : ControllerBase {


    [HttpPost("completions")]
    public async Task<IActionResult> ChatCompletions([FromBody] JsonElement payload) {
        
        // 1. Routing-Entscheidung
        var backend = await router.RouteAsync(payload);

        // 2. Streaming?
        if (payload.TryGetProperty("stream", out var s) && s.ValueKind == JsonValueKind.True) {

            if (!backend.Type.Equals("vision") && PayloadTools.ContainsBase64Image(payload)) {
                payload = PayloadTools.RemoveImages(payload);
            }
            await streamer.StreamAsync(HttpContext, $"{config["BaseUrl"]}:{backend.Port}/v1/chat/completions", payload, backend.ModelName);

            return new EmptyResult();
        }

        // 3. Non-Streaming
        return await forwarder.ForwardAsync($"{config["BaseUrl"]}:{backend.Port}/v1/chat/completions", payload);

    }
}