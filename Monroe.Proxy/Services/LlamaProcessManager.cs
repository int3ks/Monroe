using Monroe.Config;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Monroe.Services;

public class LlamaProcessManager(List<ModelConfig> models, IConfiguration config) {
    private readonly List<Process> _processes = new();

    public void Start() {
        string exe = config["LlamaExecutable"]!;
        string root = config["ModelsRoot"]!;
        bool flash = config.GetValue<bool>("FlashAttention");
        bool noMmap = config.GetValue<bool>("NoMmap");
        string cache = config["CacheType"] ?? "q8_0";

        foreach (var model in models) {
            var fullPath = Path.Combine(root, model.ModelPath);

            var dir = Path.GetDirectoryName(fullPath);
            var visionModel = Directory.GetFiles(dir).FirstOrDefault(f => f.Contains("mmproj") && f.EndsWith(".gguf"));


            var args = new List<string>
            {
                $"--host 0.0.0.0",
                $"--port {model.Port}",
                $"--model \"{fullPath}\"",
                $"-c {model.ContextSize} --keep {Math.Min(1024,model.ContextSize/2)}",
                $"--cache-type-k {cache} --cache-type-v {cache}",
                $"--context-shift"
            };

            if (!string.IsNullOrEmpty(visionModel) && File.Exists(visionModel)) {
                args.Add($" --mmproj {visionModel}");
            }

            if (flash)
                args.Add("--flash-attn on");

            if (noMmap)
                args.Add("--no-mmap");

            //args.Add("--embedding");

            var psi = new ProcessStartInfo {
                FileName = exe,
                Arguments = string.Join(" ", args),
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var proc = FindOrStartProcess(psi);
            if (proc != null) {
                _processes.Add(proc);
                Debug.WriteLine($"[MONROE] Started model '{model.Type}' on port {model.Port}");
            }
        }
    }

    public void StartClassifier() {
        string exe = config["LlamaExecutable"]!;
        string root = config["ModelsRoot"]!;

        bool flash = config.GetValue<bool>("FlashAttention");
        bool noMmap = config.GetValue<bool>("NoMmap");
        string cache = config["CacheType"] ?? "q8_0";


        var fullPath = Path.Combine(root, config["ClassifierModel"]!);

        var args = new List<string>
        {
                $"--host 0.0.0.0",
                $"--port {config["ClassifierPort"]!}",
                $"--model \"{fullPath}\"",
                $"-c {config["ClassifierContextSize"]!}",
                $"--cache-type-k {cache} --cache-type-v {cache}",
                $"--context-shift"
            };

        if (flash)
            args.Add("--flash-attn on");

        if (noMmap)
            args.Add("--no-mmap");



        var psi = new ProcessStartInfo {
            FileName = exe,
            Arguments = string.Join(" ", args),
            UseShellExecute = true,
            CreateNoWindow = false
        };

        var proc = FindOrStartProcess(psi);
        if (proc != null) {
            _processes.Add(proc);
            Debug.WriteLine($"[MONROE] Started classifier '{config["ClassifierModel"]!}' on port {config["ClassifierPort"]!}");
        }

    }

    public static Process? FindOrStartProcess(ProcessStartInfo psi) {
        // 1. Versuche laufenden Prozess zu finden
        var existing = FindMatchingProcess(psi);

        if (existing != null)
            return existing;

        // 2. Wenn keiner gefunden → neuen starten
        return Process.Start(psi);
    }
    private static string GetCommandLine(Process p) {
        return ProcessCommandLine.GetCommandLine(p.Id);
    }

    private static Process? FindMatchingProcess(ProcessStartInfo psi) {
        string exeName = Path.GetFileNameWithoutExtension(psi.FileName);
        var procs = Process.GetProcessesByName(exeName);

        foreach (var proc in procs) {
            try {

                string cmd = GetCommandLine(proc);
                if (string.IsNullOrEmpty(cmd)) {
                    continue;
                }
                string psiFileName = psi.FileName.Replace("\\", "/").Replace("\"", "");
                string cmdargs = cmd.Replace("\\", "/").Replace("\"", "").Replace(psiFileName, "", StringComparison.InvariantCultureIgnoreCase).Trim();

                string psiargs = psi.Arguments.Replace("\\", "/").Replace("\"", "").Trim();
                var cmdHostNport = ParseLlamaServerArgs(cmdargs);
                var psiHostNPort = ParseLlamaServerArgs(psiargs);

                if (cmdargs.Equals(psiargs)) {
                    if (proc.MainWindowHandle == IntPtr.Zero) {
                        //richtige args aber kein fenster mehr kill zombie
                        CloseProcess(proc);
                        return null;
                    }

                    return proc;
                } else if (cmdHostNport.Equals(psiHostNPort)) {
                    CloseProcess(proc);
                    return null;
                }


            } catch { }
        }

        return null;
    }

    public static (string Host, int Port) ParseLlamaServerArgs(string args) {
        string host = "127.0.0.1";
        int port = 11434;

        var tokens = args.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < tokens.Length; i++) {
            string t = tokens[i];

            if (t == "--host" && i + 1 < tokens.Length)
                host = tokens[i + 1];

            else if (t == "--port" && i + 1 < tokens.Length && int.TryParse(tokens[i + 1], out int p))
                port = p;

            else if ((t == "--listen" || t == "--address") && i + 1 < tokens.Length) {
                var parts = tokens[i + 1].Split(':');
                if (parts.Length == 2) {
                    host = parts[0];
                    if (int.TryParse(parts[1], out int p2))
                        port = p2;
                }
            }
        }

        return (host, port);
    }

    public static void CloseProcess(Process p) {
        try {
            if (p.MainWindowHandle != IntPtr.Zero) {
                p.CloseMainWindow();
                Thread.Sleep(200);
            }
            if (!p.HasExited) {
                p.Kill();
            }
        } catch { }
    }


    public static class ProcessCommandLine {
        private const int PROCESS_QUERY_INFORMATION = 0x0400;
        private const int PROCESS_VM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(int access, bool inherit, int pid);

        [DllImport("kernel32.dll")]
        static extern bool CloseHandle(IntPtr h);

        [DllImport("ntdll.dll")]
        static extern int NtQueryInformationProcess(
            IntPtr processHandle,
            int processInformationClass,
            ref PROCESS_BASIC_INFORMATION processInformation,
            int processInformationLength,
            out int returnLength);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(
            IntPtr hProcess,
            IntPtr lpBaseAddress,
            byte[] lpBuffer,
            int dwSize,
            out int lpNumberOfBytesRead);

        [StructLayout(LayoutKind.Sequential)]
        struct PROCESS_BASIC_INFORMATION {
            public IntPtr Reserved1;
            public IntPtr PebBaseAddress;
            public IntPtr Reserved2_0;
            public IntPtr Reserved2_1;
            public IntPtr UniqueProcessId;
            public IntPtr Reserved3;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct UNICODE_STRING {
            public ushort Length;
            public ushort MaximumLength;
            public IntPtr Buffer;
        }

        public static string GetCommandLine(int pid) {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
            if (hProcess == IntPtr.Zero)
                return "";

            try {
                // PEB auslesen
                PROCESS_BASIC_INFORMATION pbi = new PROCESS_BASIC_INFORMATION();
                NtQueryInformationProcess(hProcess, 0, ref pbi, Marshal.SizeOf(pbi), out _);

                // Adresse der ProcessParameters holen
                IntPtr pebAddress = pbi.PebBaseAddress + 0x20; // offset: ProcessParameters
                byte[] procParamsPtr = new byte[IntPtr.Size];
                ReadProcessMemory(hProcess, pebAddress, procParamsPtr, procParamsPtr.Length, out _);

                IntPtr processParameters = (IntPtr.Size == 8)
                    ? (IntPtr)BitConverter.ToInt64(procParamsPtr, 0)
                    : (IntPtr)BitConverter.ToInt32(procParamsPtr, 0);

                // UNICODE_STRING CommandLine auslesen
                IntPtr commandLineAddress = processParameters + 0x70; // offset: CommandLine
                byte[] unicodeStringData = new byte[Marshal.SizeOf(typeof(UNICODE_STRING))];
                ReadProcessMemory(hProcess, commandLineAddress, unicodeStringData, unicodeStringData.Length, out _);

                UNICODE_STRING cmdLine = new UNICODE_STRING {
                    Length = BitConverter.ToUInt16(unicodeStringData, 0),
                    MaximumLength = BitConverter.ToUInt16(unicodeStringData, 2),
                    Buffer = (IntPtr)(IntPtr.Size == 8
                        ? BitConverter.ToInt64(unicodeStringData, 8)
                        : BitConverter.ToInt32(unicodeStringData, 4))
                };

                // Jetzt den eigentlichen String lesen
                byte[] buffer = new byte[cmdLine.Length];
                ReadProcessMemory(hProcess, cmdLine.Buffer, buffer, buffer.Length, out _);

                return Encoding.Unicode.GetString(buffer);
            } finally {
                CloseHandle(hProcess);
            }
        }
    }
}