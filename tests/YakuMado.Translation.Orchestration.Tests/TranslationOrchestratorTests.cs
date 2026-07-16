using YakuMado.Core.Translation;
using YakuMado.Translation.Orchestration;

namespace YakuMado.Translation.Orchestration.Tests;

public sealed class FakeTranslator : ITranslator
{
    private readonly Func<string, LanguagePair, TranslationResult> _translate;
    public string EngineName { get; }
    public bool IsAvailable { get; set; } = true;
    public IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; set; }
    public int CallCount { get; private set; }

    public FakeTranslator(string engineName, LanguagePair supportedPair, Func<string, LanguagePair, TranslationResult>? translate = null)
    {
        EngineName = engineName;
        SupportedLanguagePairs = new[] { supportedPair };
        _translate = translate ?? ((text, pair) => new TranslationResult($"[{engineName}]{text}", engineName, TimeSpan.Zero));
    }

    public Task<TranslationResult> TranslateAsync(string sourceText, LanguagePair languagePair, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_translate(sourceText, languagePair));
    }
}

public sealed class ThrowingTranslator : ITranslator
{
    public string EngineName => "Throwing";
    public bool IsAvailable => true;
    public IReadOnlyCollection<LanguagePair> SupportedLanguagePairs { get; }

    public ThrowingTranslator(LanguagePair supportedPair) => SupportedLanguagePairs = new[] { supportedPair };

    public Task<TranslationResult> TranslateAsync(string sourceText, LanguagePair languagePair, CancellationToken cancellationToken)
        => throw new InvalidOperationException("翻訳エンジンが応答しない");
}

public class TranslationOrchestratorTests
{
    private static readonly LanguagePair EnJa = new("en", "ja");

    [Fact]
    public async Task TranslateAsync_uses_first_available_translator_in_priority_order()
    {
        var first = new FakeTranslator("First", EnJa);
        var second = new FakeTranslator("Second", EnJa);
        var orchestrator = new TranslationOrchestrator(
            new ITranslator[] { first, second }, new LruTranslationCache());

        var result = await orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.Equal("First", result.EngineName);
        Assert.Equal(0, second.CallCount);
    }

    [Fact]
    public async Task TranslateAsync_skips_translator_whose_IsAvailable_is_false()
    {
        var first = new FakeTranslator("First", EnJa) { IsAvailable = false };
        var second = new FakeTranslator("Second", EnJa);
        var orchestrator = new TranslationOrchestrator(
            new ITranslator[] { first, second }, new LruTranslationCache());

        var result = await orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.Equal("Second", result.EngineName);
    }

    [Fact]
    public async Task TranslateAsync_skips_translator_that_does_not_support_language_pair()
    {
        var first = new FakeTranslator("First", new LanguagePair("en", "fr"));
        var second = new FakeTranslator("Second", EnJa);
        var orchestrator = new TranslationOrchestrator(
            new ITranslator[] { first, second }, new LruTranslationCache());

        var result = await orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.Equal("Second", result.EngineName);
    }

    [Fact]
    public async Task TranslateAsync_falls_back_when_translator_throws()
    {
        var throwing = new ThrowingTranslator(EnJa);
        var second = new FakeTranslator("Second", EnJa);
        var orchestrator = new TranslationOrchestrator(
            new ITranslator[] { throwing, second }, new LruTranslationCache());

        var result = await orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.Equal("Second", result.EngineName);
    }

    [Fact]
    public async Task TranslateAsync_throws_NoTranslatorAvailableException_when_none_available()
    {
        var unavailable = new FakeTranslator("Unavailable", EnJa) { IsAvailable = false };
        var orchestrator = new TranslationOrchestrator(
            new ITranslator[] { unavailable }, new LruTranslationCache());

        await Assert.ThrowsAsync<NoTranslatorAvailableException>(
            () => orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None));
    }

    [Fact]
    public async Task TranslateAsync_returns_cached_result_without_calling_translator_again()
    {
        var translator = new FakeTranslator("First", EnJa);
        var cache = new LruTranslationCache();
        var orchestrator = new TranslationOrchestrator(new ITranslator[] { translator }, cache);

        await orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None);
        await orchestrator.TranslateAsync("hello", EnJa, CancellationToken.None);

        Assert.Equal(1, translator.CallCount);
    }
}
