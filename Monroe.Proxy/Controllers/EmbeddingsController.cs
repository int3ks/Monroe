using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1/embeddings")]
public class EmbeddingsController(LlamaForwarder forwarder) : ControllerBase {

    [HttpPost]
    public async Task<IActionResult> Embeddings([FromBody] JsonElement payload) {
        return await forwarder.ForwardAsync("/v1/embeddings", payload);
    }
}