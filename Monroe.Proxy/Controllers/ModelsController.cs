using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("v1/models")]
public class ModelsController : ControllerBase {
    [HttpGet]
    public IActionResult GetModels() {
        return Ok(new {
            data = new[]
            {
                new { id = "monroe-backend", @object = "model" }
            }
        });
    }
}