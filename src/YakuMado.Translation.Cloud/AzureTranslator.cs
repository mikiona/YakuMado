using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using YakuMado.Core.Translation;

namespace YakuMado.Translation.Cloud;

/// <summary>
/// Azure Translator (REST API v3.0) を用いたクラウド翻訳エンジン実装。
/// APIキーはハードコードせず、コンストラクタ引数(コンポジションルートで環境変数から読み込む)として受け取る。
/// </summary>
public sealed class AzureTranslator : ITranslator
{
    private const string Endpoint = "https://api.cognitive.microsofttranslator.com/translate?api-version=3.0";

    private readonly HttpClient _httpClient;
    private readonly string? _apiKey;
    private readonly string? _region;

    public string EngineName => "AzureTranslator";

    public bool IsAvailable => !string.IsNullOrEmpty(_apiKey);

    public IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; } = new[]
    {
        new LanguagePair("en", "ja"),
    };

    public AzureTranslator(HttpClient httpClient, string? apiKey, string? region)
    {
        _httpClient = httpClient;
        _apiKey = apiKey;
        _region = region;
    }

    public async Task<TranslationResult> TranslateAsync(
        string sourceText,
        LanguagePair languagePair,
        CancellationToken cancellationToken)
    {
        if (!IsAvailable)
        {
            throw new InvalidOperationException("Azure Translatorのapi keyが設定されていません。");
        }

        var stopwatch = Stopwatch.StartNew();

        var url = $"{Endpoint}&from={languagePair.SourceCode}&to={languagePair.TargetCode}";
        var requestBody = JsonSerializer.Serialize(new[] { new AzureTranslateRequestItem(sourceText) });

        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json"),
        };
        request.Headers.Add("Ocp-Apim-Subscription-Key", _apiKey);
        if (!string.IsNullOrEmpty(_region))
        {
            request.Headers.Add("Ocp-Apim-Subscription-Region", _region);
        }

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<AzureTranslateResponseItem[]>(responseJson)
            ?? throw new InvalidOperationException("Azure Translatorの応答を解析できませんでした。");

        var translatedText = parsed.FirstOrDefault()?.Translations?.FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("Azure Translatorの応答に翻訳結果が含まれていません。");

        stopwatch.Stop();
        return new TranslationResult(translatedText, EngineName, stopwatch.Elapsed);
    }

    private sealed record AzureTranslateRequestItem([property: JsonPropertyName("Text")] string Text);

    private sealed record AzureTranslateResponseItem(
        [property: JsonPropertyName("translations")] AzureTranslationItem[] Translations);

    private sealed record AzureTranslationItem(
        [property: JsonPropertyName("text")] string Text,
        [property: JsonPropertyName("to")] string To);
}
