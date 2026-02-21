using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1")]
public class CompletionsController : ControllerBase {
    private readonly LlamaForwarder _forwarder;
    private readonly LlamaStreamingForwarder _streamer;
    private readonly ModelRouter _router;

    public CompletionsController(
        LlamaForwarder forwarder,
        LlamaStreamingForwarder streamer,
        ModelRouter router) {
        _forwarder = forwarder;
        _streamer = streamer;
        _router = router;
    }


    [HttpPost("completions")]
    public async Task<IActionResult> Completions([FromBody] JsonElement payload) {
        //if (payload.TryGetProperty("stream", out var s) && s.ValueKind == JsonValueKind.True) {
        //    await _streamer.StreamAsync(HttpContext, "/v1/completions", payload);
        //    return new EmptyResult();
        //}

        //return await _forwarder.ForwardAsync("/v1/completions", payload);


        string backend = _router.Route(payload);

        if (payload.TryGetProperty("stream", out var s) && s.ValueKind == JsonValueKind.True) {
            await _streamer.StreamAsync(HttpContext, backend + "/v1/completions", payload);
            return new EmptyResult();
        }

        return await _forwarder.ForwardAsync(backend + "/v1/completions", payload);


    }
}