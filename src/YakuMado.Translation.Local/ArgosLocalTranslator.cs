using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using YakuMado.Core.Translation;

namespace YakuMado.Translation.Local;

/// <summary>
/// Argos Translate(CTranslate2ベース)を用いた実験的ローカルNMT翻訳エンジン。
///
/// 【重要】Argos Translateのen-jaモデルは商用利用ライセンスが未確定
/// (GitHub Issue #507でREADME記載なしと確認済み、開発者本人も法的保証はしていない。
/// 詳細は docs/prd.md セクション6、runtime/local-nmt/README.md を参照)。
/// そのため既定では無効であり、環境変数 YAKUMADO_ENABLE_EXPERIMENTAL_LOCAL_NMT を
/// "1" または "true" に設定した場合のみ有効化される。商用配布前には法務確認が必須。
/// </summary>
public sealed class ArgosLocalTranslator : ITranslator, IDisposable
{
    public const string EnabledEnvVarName = "YAKUMADO_ENABLE_EXPERIMENTAL_LOCAL_NMT";

    private readonly string _pythonExecutablePath;
    private readonly string _serverScriptPath;
    private readonly SemaphoreSlim _requestLock = new(1, 1);
    private readonly object _processLock = new();
    private Process? _process;

    public string EngineName => "ArgosLocalTranslator(実験的機能)";

    public IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; } = new[] { new LanguagePair("en", "ja") };

    public bool IsAvailable =>
        IsEnabledByEnvVar() && File.Exists(_pythonExecutablePath) && File.Exists(_serverScriptPath);

    public ArgosLocalTranslator(string pythonExecutablePath, string serverScriptPath)
    {
        _pythonExecutablePath = pythonExecutablePath;
        _serverScriptPath = serverScriptPath;
    }

    internal static bool IsEnabledByEnvVar()
    {
        var value = Environment.GetEnvironmentVariable(EnabledEnvVarName);
        return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<TranslationResult> TranslateAsync(
        string sourceText,
        LanguagePair languagePair,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException(
                $"実験的ローカルNMT機能が無効です。環境変数 {EnabledEnvVarName} を設定し、Pythonランタイムが配置されていることを確認してください。");
        }

        EnsureProcessStarted();

        var requestJson = JsonSerializer.Serialize(new TranslateRequest(sourceText, languagePair.SourceCode, languagePair.TargetCode));

        await _requestLock.WaitAsync(cancellationToken);
        try
        {
            var stopwatch = Stopwatch.StartNew();

            await _process!.StandardInput.WriteLineAsync(requestJson);
            await _process.StandardInput.FlushAsync();

            var responseLine = await _process.StandardOutput.ReadLineAsync(cancellationToken)
                ?? throw new InvalidOperationException("ローカルNMTサーバーから応答がありませんでした(プロセスが終了した可能性があります)。");

            var response = JsonSerializer.Deserialize<TranslateResponse>(responseLine)
                ?? throw new InvalidOperationException("ローカルNMTサーバーの応答を解析できませんでした。");

            if (response.Error != null)
            {
                throw new InvalidOperationException($"ローカルNMTサーバーエラー: {response.Error}");
            }

            stopwatch.Stop();
            return new TranslationResult(response.TranslatedText!, EngineName, stopwatch.Elapsed);
        }
        finally
        {
            _requestLock.Release();
        }
    }

    private void EnsureProcessStarted()
    {
        if (_process is { HasExited: false }) return;

        lock (_processLock)
        {
            if (_process is { HasExited: false }) return;

            var psi = new ProcessStartInfo(_pythonExecutablePath, $"\"{_serverScriptPath}\"")
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            _process = Process.Start(psi)
                ?? throw new InvalidOperationException("ローカルNMTサーバープロセスの起動に失敗しました。");
        }
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false } process)
            {
                process.Kill(entireProcessTree: true);
                // venv経由のpython.exeはランチャー・実体プロセスの2段構成になる場合があり、
                // Kill直後は子孫プロセスの終了が完了していないことがあるため短時間待機する(実機検証で確認)。
                process.WaitForExit(3000);
            }
        }
        catch (InvalidOperationException)
        {
        }
        _process?.Dispose();
        _requestLock.Dispose();
    }

    private sealed record TranslateRequest(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("from")] string From,
        [property: JsonPropertyName("to")] string To);

    private sealed record TranslateResponse(
        [property: JsonPropertyName("translatedText")] string? TranslatedText,
        [property: JsonPropertyName("elapsedMs")] double? ElapsedMs,
        [property: JsonPropertyName("error")] string? Error);
}
