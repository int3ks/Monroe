using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/models")]
public class ModelsController (IConfiguration config): ControllerBase {
    [HttpGet]
    public IActionResult GetModels() {
        var ModelName = config["ModelName"] ?? "M.O.N.R.O.E";

        return Ok(new {
            data = new[]
            {
                new { id = ModelName, @object = "model" ,
                capabilities = new
                    {
                        chat = true,
                        vision = true,
                        audio = true,
                        image = true,
                        embeddings = true,
                       // tools = true,
                       // function_calling = true,
                        reasoning = true,
                       // realtime = false
                    }}
            }
        });
    }
}


