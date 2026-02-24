using Microsoft.AspNetCore.Mvc;
using Monroe.Config;
using Monroe.Services;
using System.Buffers.Text;

[ApiController]
[Route("v1/audio")]
public class AudioController (IConfiguration config, IHttpClientFactory factory, List<ModelConfig> models) : ControllerBase {

    [HttpPost("transcriptions")]
    public async Task<IActionResult> Transcriptions() {
        // Multipart-Formular lesen
        var form = await Request.ReadFormAsync();

        // Modell extrahieren (falls vorhanden)
        string modelName = form.TryGetValue("model", out var m)
            ? m.ToString()
            : "";

        // Modell suchen (fallback: erstes Audio-fähiges Modell)
        var model = models.FirstOrDefault(x =>
            x.Type.Equals(modelName, StringComparison.OrdinalIgnoreCase)
        ) ?? models.FirstOrDefault(x => x.Type == "audio")
          ?? models.First(); // letzter Fallback

        // HTTP-Client für Backend
        var client = factory.CreateClient("backend");

        // Multipart 1:1 weiterleiten
        using var content = new MultipartFormDataContent();

        foreach (var field in form)
            content.Add(new StringContent(field.Value), field.Key);

        foreach (var file in form.Files) {
            var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            content.Add(fileContent, file.Name, file.FileName);
        }

        // Anfrage an das Modell weiterleiten
        var response = await client.PostAsync(
            $"{model.ApiUrl(config["BaseUrl"]!)}/v1/audio/transcriptions",
            content,
            HttpContext.RequestAborted
        );

        var bytes = await response.Content.ReadAsByteArrayAsync(HttpContext.RequestAborted);

        return File(bytes, response.Content.Headers.ContentType?.ToString() ?? "application/json");
    }
}
