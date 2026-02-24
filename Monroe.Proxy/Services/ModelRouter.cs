using Monroe.Config;
using Monroe.Proxy.Helper;
using System.Diagnostics;
using System.Text.Json;

namespace Monroe.Services;

public class ModelRouter(Classifier classifier, List<ModelConfig> models) {
   

    public async Task<ModelConfig> RouteAsync(JsonElement payload) {
        // 1) Vision-Hardcheck: Wenn ein Bild drin ist → Vision-Modell erzwingen
        var visionModel = models.FirstOrDefault(m => m.Type.Equals("vision"));
        
        if (PayloadTools.IsImageRequest(payload)) {
            return visionModel;
        }

        // 2) Classifier entscheidet dynamisch
        var model = await classifier.DecideBackendAsync(payload);
        Console.WriteLine($"\u001b[36m  >>>Classifier result:\u001b[0m {model.ModelName}");
        Console.WriteLine($"\u001b[36m  >>>Classifier result:\u001b[0m {model.Type}");
        Console.WriteLine($"\u001b[36m  >>>Classifier result:\u001b[0m Rules->{string.Join(", ", model.Rules.UseFor)}");

        // 3) Fallback (sollte nie passieren)
        return model ?? models.First(m => m.Type == "coder");
    }

}