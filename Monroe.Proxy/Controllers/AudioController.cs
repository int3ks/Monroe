using Microsoft.AspNetCore.Mvc;
using Monroe.Services;

[ApiController]
[Route("v1/audio")]
public class AudioController : ControllerBase {
    private readonly IHttpClientFactory _factory;
    private readonly ModelRouter _router;

    public AudioController(IHttpClientFactory factory, ModelRouter router) {
        _factory = factory;
        _router = router;
    }


    [HttpPost("transcriptions")]
    public async Task<IActionResult> Transcriptions() {
        // Multipart-Formular lesen
        var form = await Request.ReadFormAsync();

        // Modell extrahieren (falls vorhanden)
        string model = form.TryGetValue("model", out var m)
            ? m.ToString()
            : "";

        // Routing-Entscheidung (du kannst später Regeln für Audio einbauen)
        string backend = _router.RouteFromModel(model);

        var client = _factory.CreateClient("backend");

        // Multipart 1:1 weiterleiten
        using var content = new MultipartFormDataContent();

        foreach (var field in form)
            content.Add(new StringContent(field.Value), field.Key);

        foreach (var file in form.Files) {
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            content.Add(fileContent, file.Name, file.FileName);
        }

        var response = await client.PostAsync(backend + "/v1/audio/transcriptions", content, HttpContext.RequestAborted);
        var bytes = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);

        return File(bytes, response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }

}