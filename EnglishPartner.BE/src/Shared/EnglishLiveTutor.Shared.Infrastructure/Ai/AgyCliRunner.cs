using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Exceptions;
using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class AgyCliRunner : IAgyCliRunner
{
    private static readonly Regex CodeFenceRegex = new(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Compiled);
    // Deny every agent tool (file read/write, terminal, MCP, URL fetch): grading/generation only needs text in,
    // text out. Verified with agy.exe on 2026-11-02: outside-workspace read_file succeeded with only
    // allowNonWorkspaceAccess=false + --sandbox, and is blocked by these deny rules.
    private const string SandboxedSettings =
        """{"allowNonWorkspaceAccess":false,"permissions":{"allow":[],"deny":["read_file(*)","write_file(*)","command(*)","mcp(*)","read_url(*)"]}}""";
    private static readonly string[] EnvironmentAllowlist =
    [
        "PATH",
        "SystemRoot",
        "windir",
        "ComSpec",
        "PATHEXT",
        "TEMP",
        "TMP",
        "USERPROFILE",
        "HOMEDRIVE",
        "HOMEPATH",
        "APPDATA",
        "LOCALAPPDATA",
        "ProgramData",
        "ProgramFiles",
        "ProgramFiles(x86)",
        "NUMBER_OF_PROCESSORS",
        "PROCESSOR_ARCHITECTURE",
        "OS",
        "HOME"
    ];

    private readonly IOptions<AntigravityCliOptions> _options;
    private readonly ILogger<AgyCliRunner> _logger;
    private readonly string _executablePath;

    public AgyCliRunner(
        IOptions<AntigravityCliOptions> options,
        ILogger<AgyCliRunner> logger)
    {
        _options = options;
        _logger = logger;
        _executablePath = ResolveAgyExecutablePath(options.Value.ExecutablePath);
    }

    public bool IsEnabled => _options.Value.Enabled;

    public IReadOnlyCollection<string> AllowedModels =>
        _options.Value.AllowedModels is { Count: > 0 }
            ? _options.Value.AllowedModels.AsReadOnly()
            : Array.Empty<string>();

    public bool IsAgyModel(string? modelCode)
    {
        if (!IsEnabled) return false;
        if (string.IsNullOrWhiteSpace(modelCode)) return false;
        string cleaned = modelCode.Trim();
        if (cleaned.StartsWith("agy:", StringComparison.OrdinalIgnoreCase)) return true;

        var allowed = _options.Value.AllowedModels;
        if (allowed != null)
        {
            return allowed.Any(m => string.Equals(m, cleaned, StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    public string NormalizeModel(string? modelCode)
    {
        string defaultModel = !string.IsNullOrWhiteSpace(_options.Value.DefaultModel)
            ? _options.Value.DefaultModel.Trim()
            : "gemini-3.8-flash-medium";

        if (string.IsNullOrWhiteSpace(modelCode)) return defaultModel;

        string cleaned = modelCode.Trim();
        if (cleaned.StartsWith("agy:", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[4..].Trim();
        }

        var allowed = _options.Value.AllowedModels;
        if (allowed != null)
        {
            var match = allowed.FirstOrDefault(m => string.Equals(m, cleaned, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(match))
            {
                return match;
            }
        }

        _logger.LogInformation("Requested model '{ModelCode}' not in AGY allowed models config. Defaulting to '{DefaultModel}'.", modelCode, defaultModel);
        return defaultModel;
    }

    public async Task<string> ExecutePromptAsync(
        string prompt,
        string? modelCode = null,
        CancellationToken ct = default)
    {
        if (!IsEnabled)
        {
            throw new ForbiddenException("agy_local_disabled");
        }

        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
        }

        string effectiveModel = NormalizeModel(modelCode);
        string workingDirectory = CreateWorkingDirectory(out bool deleteAfterRun);
        try
        {
            var psi = BuildProcessStartInfo(_executablePath, effectiveModel, prompt, workingDirectory);
            _logger.LogInformation("Spawning Antigravity CLI with model {Model}...", effectiveModel);

            using var process = new Process { StartInfo = psi };

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data != null) stdoutBuilder.AppendLine(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data != null) stderrBuilder.AppendLine(e.Data);
            };

            int timeoutSec = _options.Value.TimeoutSeconds > 0 ? _options.Value.TimeoutSeconds : 600;

            try
            {
                if (!process.Start())
                {
                    throw new InvalidOperationException("Không thể khởi động tiến trình Antigravity CLI.");
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSec));

                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                KillProcessSafely(process);
                throw new TimeoutException($"Antigravity CLI quá thời gian phản hồi ({timeoutSec} giây).");
            }
            catch (Exception)
            {
                KillProcessSafely(process);
                throw;
            }

            string stdout = stdoutBuilder.ToString().Trim();
            string stderr = stderrBuilder.ToString().Trim();

            if (process.ExitCode != 0 && string.IsNullOrWhiteSpace(stdout))
            {
                _logger.LogError(
                    "Antigravity CLI failed with exit code {ExitCode}. Stderr: {Stderr}",
                    process.ExitCode,
                    TruncateForLog(stderr));
                throw new InvalidOperationException($"Antigravity CLI trả về lỗi (exit code {process.ExitCode}).");
            }

            return ParseAgyResponse(stdout, stderr);
        }
        finally
        {
            if (deleteAfterRun)
            {
                TryDeleteDirectory(workingDirectory);
            }
        }
    }

    private string ParseAgyResponse(string stdout, string stderr)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            _logger.LogError("Antigravity CLI returned no output. Stderr: {Stderr}", TruncateForLog(stderr));
            throw new InvalidOperationException("Antigravity CLI không trả về kết quả.");
        }

        try
        {
            var node = JsonNode.Parse(stdout);
            string? status = node?["status"]?.GetValue<string>();
            if (!string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                string? providerError = node?["error"]?.GetValue<string>();
                _logger.LogError(
                    "Antigravity CLI returned an unsuccessful response. Error: {Error}; Stderr: {Stderr}",
                    TruncateForLog(providerError ?? string.Empty),
                    TruncateForLog(stderr));
                throw new InvalidOperationException("Antigravity CLI phản hồi không thành công.");
            }

            string? responseText = node?["response"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(responseText))
            {
                throw new InvalidOperationException("Antigravity CLI trả về phản hồi rỗng.");
            }

            // Strip markdown code fences if wrapped
            var match = CodeFenceRegex.Match(responseText);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }

            return responseText.Trim();
        }
        catch (System.Text.Json.JsonException)
        {
            // If output was raw text instead of JSON envelope
            var match = CodeFenceRegex.Match(stdout);
            if (match.Success)
            {
                return match.Groups[1].Value.Trim();
            }
            return stdout.Trim();
        }
    }

    internal static ProcessStartInfo BuildProcessStartInfo(
        string executablePath,
        string effectiveModel,
        string prompt,
        string workingDirectory)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.Environment.Clear();
        foreach (string key in EnvironmentAllowlist)
        {
            string? value = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrEmpty(value))
            {
                psi.Environment[key] = value;
            }
        }

        psi.ArgumentList.Add("--model");
        psi.ArgumentList.Add(effectiveModel);
        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("json");
        psi.ArgumentList.Add("--disable-slash-commands");
        psi.ArgumentList.Add("--sandbox");
        psi.ArgumentList.Add("--settings");
        psi.ArgumentList.Add(SandboxedSettings);
        psi.ArgumentList.Add("--print");
        psi.ArgumentList.Add(prompt);

        return psi;
    }

    // Always a fresh, empty directory per call that is deleted afterwards: a shared workspace would let one
    // prompt-injected run plant files (e.g. GEMINI.md) that steer every later run for other learners.
    private static string CreateWorkingDirectory(out bool deleteAfterRun)
    {
        string temporaryDirectory = Path.Combine(
            Path.GetTempPath(),
            "englishpartner-agy",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporaryDirectory);
        deleteAfterRun = true;
        return temporaryDirectory;
    }

    private void TryDeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not remove temporary Antigravity CLI working directory.");
        }
    }

    private static string TruncateForLog(string value) =>
        value.Length <= 500 ? value : value[..500];

    private static void KillProcessSafely(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Suppress errors during cleanup
        }
    }

    private static string ResolveAgyExecutablePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath) && File.Exists(configuredPath))
        {
            return configuredPath;
        }

        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string defaultPath = Path.Combine(localAppData, "agy", "bin", "agy.exe");
        if (File.Exists(defaultPath))
        {
            return defaultPath;
        }

        string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string geminiPath = Path.Combine(userProfile, ".gemini", "antigravity-cli", "bin", "agy.exe");
        if (File.Exists(geminiPath))
        {
            return geminiPath;
        }

        // Fallback to searching PATH via executable name
        return "agy.exe";
    }
}
