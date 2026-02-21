using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1/chat")]
public class ChatController : ControllerBase {
    private readonly LlamaForwarder _forwarder;
    private readonly LlamaStreamingForwarder _streamer;
    private readonly ModelRouter _router;

    public ChatController(
        LlamaForwarder forwarder,
        LlamaStreamingForwarder streamer,
        ModelRouter router) {
        _forwarder = forwarder;
        _streamer = streamer;
        _router = router;
    }


    [HttpPost("completions")]
    public async Task<IActionResult> ChatCompletions([FromBody] JsonElement payload) {
        //if (payload.TryGetProperty("stream", out var s) && s.ValueKind == JsonValueKind.True) {
        //    await _streamer.StreamAsync(HttpContext, "/v1/chat/completions", payload);
        //    return new EmptyResult();
        //}

        //return await _forwarder.ForwardAsync("/v1/chat/completions", payload);

        // 1. Routing-Entscheidung
        string backend = _router.Route(payload);

        // 2. Streaming?
        if (payload.TryGetProperty("stream", out var s) && s.ValueKind == JsonValueKind.True) {
            await _streamer.StreamAsync(HttpContext, backend + "/v1/chat/completions", payload);
            return new EmptyResult();
        }

        // 3. Non-Streaming
        return await _forwarder.ForwardAsync(backend + "/v1/chat/completions", payload);



    }
}