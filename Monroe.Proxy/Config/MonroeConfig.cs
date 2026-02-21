namespace Monroe.Config;

public static class MonroeConfig {
    public static string LlamaExecutable = @"C:\\llama.cpp.vk\\llama-server.exe";
    public static string ModelPath = @"C:\Users\Admin\.lmstudio\models\lmstudio-community\Qwen3-1.7B-GGUF\Qwen3-1.7B-Q4_K_M.gguf";
    public static int ProxyPort = 8123;

    // NEU: Entscheider / Classifier Backend
    public static int ClassifierPort = 8146; // z.B. kleines 1B Modell
    public static string ClassifierUrl => $"http://localhost:{ClassifierPort}";


    // ------------------------------------------------------------
    // BACKENDS (4 getrennte Modelle / Instanzen)
    // ------------------------------------------------------------

    // 1) Unrestricted / Default
    public static int UnrestrictedPort = 8142;
    public static string UnrestrictedUrl => $"http://localhost:{UnrestrictedPort}";

    // 2) Vision (Screenshots, OCR, Bilderkennung)
    public static int VisionPort = 8143;
    public static string VisionUrl => $"http://localhost:{VisionPort}";

    // 3) Coder (Code-Modelle)
    public static int CoderPort = 8144;
    public static string CoderUrl => $"http://localhost:{CoderPort}";

    // 4) General (normale Chat-Modelle)
    public static int GeneralPort = 8145;
    public static string GeneralUrl => $"http://localhost:{GeneralPort}";

}