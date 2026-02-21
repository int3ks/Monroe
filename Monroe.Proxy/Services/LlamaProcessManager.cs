using System.Diagnostics;
using Monroe.Config;

namespace Monroe.Services;

public class LlamaProcessManager {
    private Process? _process;

    public void Start() {
        if (IsRunning())
            return;

        var args =
            $"--model \"{MonroeConfig.ModelPath}\" " +
            $"--port {MonroeConfig.ClassifierPort} " +
            $"--ctx-size 4096";


        args = $"--host 0.0.0.0 --port {MonroeConfig.ClassifierPort} --model C:\\Users\\Admin\\.lmstudio\\models\\lmstudio-community\\Qwen3-1.7B-GGUF\\Qwen3-1.7B-Q4_K_M.gguf -c 2048 --keep 1024 --no-mmap --direct-io --flash-attn on --cache-type-k q8_0 --cache-type-v q8_0 --context-shift";

        _process = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = MonroeConfig.LlamaExecutable,
                Arguments = args,
                //UseShellExecute = false,
                //RedirectStandardOutput = true,
                //RedirectStandardError = true,
                //CreateNoWindow = true
            }
        };

        //_process.OutputDataReceived += (_, e) => {
        //    if (!string.IsNullOrWhiteSpace(e.Data))
        //        Console.WriteLine("[llama.cpp] " + e.Data);
        //};

        //_process.ErrorDataReceived += (_, e) => {
        //    if (!string.IsNullOrWhiteSpace(e.Data))
        //        Console.WriteLine("[llama.cpp ERROR] " + e.Data);
        //};

        _process.Start();
        //_process.BeginOutputReadLine();
        //_process.BeginErrorReadLine();

        Console.WriteLine($"llama.cpp gestartet auf Port {MonroeConfig.ClassifierPort}");
    }

    public bool IsRunning() {
        return _process != null && !_process.HasExited;
    }
}