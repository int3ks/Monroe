using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1/images")]
public class ImagesController : ControllerBase {
    private readonly LlamaForwarder _forwarder;
    private readonly ModelRouter _router;

    public ImagesController(LlamaForwarder forwarder, ModelRouter router) {
        _forwarder = forwarder;
        _router = router;
    }


    [HttpPost("generations")]
    public async Task<IActionResult> Generations([FromBody] JsonElement payload) {
        string backend = _router.Route(payload);

        // Images sind IMMER non-streaming im OpenAI-Standard
        return await _forwarder.ForwardAsync(backend + "/v1/images/generations", payload);


    }
}