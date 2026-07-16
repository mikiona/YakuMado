using System.Net;
using YakuMado.Core.Translation;
using YakuMado.Translation.Cloud;

namespace YakuMado.Translation.Cloud.Tests;

/// <summary>実ネットワーク呼び出しを行わない契約テスト用のフェイクハンドラ。</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public HttpRequestMessage? CapturedRequest { get; private set; }
    public string? CapturedRequestBody { get; private set; }
    private readonly HttpStatusCode _statusCode;
    private readonly string _responseBody;

    public FakeHttpMessageHandler(string responseBody, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        _responseBody = responseBody;
        _statusCode = statusCode;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        CapturedRequest = request;
        if (request.Content != null)
        {
            CapturedRequestBody = await request.Content.ReadAsStringAsync(cancellationToken);
        }

        return new HttpResponseMessage(_statusCode)
        {
            Content = new StringContent(_responseBody),
        };
    }
}

public class AzureTranslatorTests
{
    private static readonly LanguagePair EnJa = new("en", "ja");

    [Fact]
    public void IsAvailable_is_false_when_api_key_is_null_or_empty()
    {
        var translator = new AzureTranslator(new HttpClient(), apiKey: null, region: "japaneast");

        Assert.False(translator.IsAvailable);
    }

    [Fact]
    public void IsAvailable_is_true_when_api_key_is_set()
    {
        var translator = new AzureTranslator(new HttpClient(), apiKey: "dummy-key", region: "japaneast");

        Assert.True(translator.IsAvailable);
    }

    [Fact]
    public async Task TranslateAsync_sends_correct_headers_and_request_body()
    {
        var responseJson = """[{"translations":[{"text":"こんにちは","to":"ja"}]}]""";
        var handler = new FakeHttpMessageHandler(responseJson);
        var httpClient = new HttpClient(handler);
        var translator = new AzureTranslator(httpClient, apiKey: "dummy-key", region: "japaneast");

        await translator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.NotNull(handler.CapturedRequest);
        Assert.Equal("dummy-key", handler.CapturedRequest!.Headers.GetValues("Ocp-Apim-Subscription-Key").First());
        Assert.Equal("japaneast", handler.CapturedRequest!.Headers.GetValues("Ocp-Apim-Subscription-Region").First());
        Assert.Contains("to=ja", handler.CapturedRequest.RequestUri!.ToString());
        Assert.Contains("hello", handler.CapturedRequestBody);
    }

    [Fact]
    public async Task TranslateAsync_parses_translated_text_from_response()
    {
        var responseJson = """[{"translations":[{"text":"こんにちは","to":"ja"}]}]""";
        var handler = new FakeHttpMessageHandler(responseJson);
        var translator = new AzureTranslator(new HttpClient(handler), apiKey: "dummy-key", region: "japaneast");

        var result = await translator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.Equal("こんにちは", result.TranslatedText);
        Assert.Equal("AzureTranslator", result.EngineName);
    }

    [Fact]
    public async Task TranslateAsync_throws_when_api_key_not_set()
    {
        var translator = new AzureTranslator(new HttpClient(), apiKey: null, region: null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => translator.TranslateAsync("hello", EnJa, CancellationToken.None));
    }

    [Fact]
    public async Task TranslateAsync_throws_when_http_call_fails()
    {
        var handler = new FakeHttpMessageHandler("error", HttpStatusCode.InternalServerError);
        var translator = new AzureTranslator(new HttpClient(handler), apiKey: "dummy-key", region: "japaneast");

        await Assert.ThrowsAsync<HttpRequestException>(
            () => translator.TranslateAsync("hello", EnJa, CancellationToken.None));
    }
}
