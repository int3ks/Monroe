using Microsoft.AspNetCore.Mvc;
using Monroe.Services;
using System.Reflection;
using System.Text.Json;

[ApiController]
[Route("v1/images")]
public class ImagesController(IConfiguration config, LlamaForwarder forwarder, ModelRouter router) : ControllerBase {
   

    [HttpPost("generations")]
    public async Task<IActionResult> Generations([FromBody] JsonElement payload) {
        var backend = await router.RouteAsync(payload);

        // Images sind IMMER non-streaming im OpenAI-Standard
        return await forwarder.ForwardAsync($"{backend.ApiUrl(config["BaseUrl"]!)}/v1/images/generations", payload);


    }
}