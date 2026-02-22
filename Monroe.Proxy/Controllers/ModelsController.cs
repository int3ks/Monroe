using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/models")]
public class ModelsController : ControllerBase {
    [HttpGet]
    public IActionResult GetModels() {
        return Ok(new {
            data = new[]
            {
                new { id = "M.O.N.R.O.E", @object = "model" ,
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


