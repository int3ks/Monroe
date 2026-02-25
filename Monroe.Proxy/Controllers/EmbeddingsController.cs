using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Text.Json;

[ApiController]
[Route("v1/embeddings")]
public class EmbeddingsController(IConfiguration config, LlamaForwarder forwarder, ModelRouter router) : ControllerBase {

    [HttpPost]
    public async Task<IActionResult> Embeddings([FromBody] JsonElement payload) {
        var backend = await router.RouteAsync(payload);

        return await forwarder.ForwardAsync($"{backend.ApiUrl(config["BaseUrl"]!)}/v1/embeddings", payload);
    }
}