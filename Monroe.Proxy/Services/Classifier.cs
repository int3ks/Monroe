using Monroe.Config;
using System.Text.Json;

namespace Monroe.Services;

public class Classifier {
    // Aktuell: nur Tag-Routing
    // Später: hier kommt das echte Classifier-Modell rein
    public bool TryClassifyByTags(JsonElement payload, out string backend) {
        backend = "";

        if (!payload.TryGetProperty("tags", out var tags))
            return false;

        if (tags.ValueKind != JsonValueKind.Array)
            return false;

        foreach (var tag in tags.EnumerateArray()) {
            string t = tag.GetString() ?? "";

            if (t.Equals("vision", StringComparison.OrdinalIgnoreCase)) {
                backend = MonroeConfig.VisionUrl;
                return true;
            }

            if (t.Equals("coder", StringComparison.OrdinalIgnoreCase)) {
                backend = MonroeConfig.CoderUrl;
                return true;
            }

            if (t.Equals("general", StringComparison.OrdinalIgnoreCase)) {
                backend = !string.IsNullOrWhiteSpace(MonroeConfig.GeneralUrl)
                    ? MonroeConfig.GeneralUrl
                    : MonroeConfig.CoderUrl;
                return true;
            }

            if (t.Equals("unrestricted", StringComparison.OrdinalIgnoreCase)) {
                backend = MonroeConfig.UnrestrictedUrl;
                return true;
            }
        }

        return false;
    }
}