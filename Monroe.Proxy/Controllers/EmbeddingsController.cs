using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1/embeddings")]
public class EmbeddingsController : ControllerBase {
    private readonly LlamaForwarder _forwarder;

    public EmbeddingsController(LlamaForwarder forwarder) {
        _forwarder = forwarder;
    }

    [HttpPost]
    public async Task<IActionResult> Embeddings([FromBody] JsonElement payload) {
        return await _forwarder.ForwardAsync("/v1/embeddings", payload);
    }
}