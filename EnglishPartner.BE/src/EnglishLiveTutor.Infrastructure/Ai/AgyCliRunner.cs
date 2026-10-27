using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using EnglishLiveTutor.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EnglishLiveTutor.Infrastructure.Ai;

public sealed class AgyCliRunner : IAgyCliRunner
{
    private static readonly Regex CodeFenceRegex = new(@"```(?:json)?\s*([\s\S]*?)\s*```", RegexOptions.Compiled);

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

    public bool IsAgyModel(string? modelCode)
    {
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
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty.", nameof(prompt));
        }

        string effectiveModel = NormalizeModel(modelCode);

        var psi = new ProcessStartInfo
        {
            FileName = _executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        psi.ArgumentList.Add("--model");
        psi.ArgumentList.Add(effectiveModel);
        psi.ArgumentList.Add("--output-format");
        psi.ArgumentList.Add("json");
        psi.ArgumentList.Add("--disable-slash-commands");
        psi.ArgumentList.Add("--print");
        psi.ArgumentList.Add(prompt);

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

        int timeoutSec = _options.Value.TimeoutSeconds > 0 ? _options.Value.TimeoutSeconds : 120;

        try
        {
            if (!process.Start())
            {
                throw new InvalidOperationException($"Không thể khởi động tiến trình Antigravity CLI tại: {_executablePath}");
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
            _logger.LogError("Antigravity CLI failed with exit code {ExitCode}. Stderr: {Stderr}", process.ExitCode, stderr);
            throw new InvalidOperationException($"Antigravity CLI trả về lỗi (exit code {process.ExitCode}): {stderr}");
        }

        return ParseAgyResponse(stdout, stderr);
    }

    private string ParseAgyResponse(string stdout, string stderr)
    {
        if (string.IsNullOrWhiteSpace(stdout))
        {
            throw new InvalidOperationException($"Antigravity CLI không trả về kết quả. Stderr: {stderr}");
        }

        try
        {
            var node = JsonNode.Parse(stdout);
            string? status = node?["status"]?.GetValue<string>();
            if (!string.Equals(status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
            {
                string? errorMsg = node?["error"]?.GetValue<string>() ?? stderr;
                throw new InvalidOperationException($"Antigravity CLI phản hồi không thành công: {errorMsg}");
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
