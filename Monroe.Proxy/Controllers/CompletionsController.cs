using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Reflection;
using System.Text.Json;

[ApiController]
[Route("v1")]
public class CompletionsController(IConfiguration config, LlamaForwarder forwarder, LlamaStreamingForwarder streamer, ModelRouter router) : ControllerBase {
   
    [HttpPost("completions")]
    public async Task<IActionResult> Completions([FromBody] JsonElement payload) {
        
        var backend = await router.RouteAsync(payload);

        if (payload.TryGetProperty("stream", out var s) && s.ValueKind == JsonValueKind.True) {
            await streamer.StreamAsync(HttpContext, $"{backend.ApiUrl(config["BaseUrl"]!)}:{backend.Port}/v1/completions", payload, backend.ModelName);
            return new EmptyResult();
        }

        return await forwarder.ForwardAsync($"{backend.ApiUrl(config["BaseUrl"]!)}/v1/completions", payload);


    }
}