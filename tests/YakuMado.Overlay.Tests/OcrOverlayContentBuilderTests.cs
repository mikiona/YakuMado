using YakuMado.Core.Ocr;
using YakuMado.Core.Translation;
using YakuMado.Overlay;
using YakuMado.Translation.Orchestration;

namespace YakuMado.Overlay.Tests;

public sealed class FakeOrchestrator : ITranslationOrchestrator
{
    private readonly Func<string, TranslationResult> _translate;
    public List<string> TranslatedInputs { get; } = new();

    public FakeOrchestrator(Func<string, TranslationResult>? translate = null)
    {
        _translate = translate ?? (text => new TranslationResult($"[JA]{text}", "Fake", TimeSpan.Zero));
    }

    public Task<TranslationResult> TranslateAsync(string sourceText, LanguagePair languagePair, CancellationToken cancellationToken)
    {
        TranslatedInputs.Add(sourceText);
        return Task.FromResult(_translate(sourceText));
    }
}

public class OcrOverlayContentBuilderTests
{
    private static readonly LanguagePair EnJa = new("en", "ja");

    [Fact]
    public async Task BuildAsync_translates_each_line_and_preserves_bounding_box()
    {
        var orchestrator = new FakeOrchestrator();
        var builder = new OcrOverlayContentBuilder(orchestrator);
        var ocrResult = new OcrResult(new[]
        {
            new OcrTextLine("Hello", new ScreenRect(0, 0, 100, 20)),
            new OcrTextLine("World", new ScreenRect(0, 30, 100, 20)),
        });

        var blocks = await builder.BuildAsync(ocrResult, EnJa, CancellationToken.None);

        Assert.Equal(2, blocks.Count);
        Assert.Equal("[JA]Hello", blocks[0].TranslatedText);
        Assert.Equal(new ScreenRect(0, 0, 100, 20), blocks[0].BoundingBox);
        Assert.Equal("[JA]World", blocks[1].TranslatedText);
        Assert.Equal(new ScreenRect(0, 30, 100, 20), blocks[1].BoundingBox);
    }

    [Fact]
    public async Task BuildAsync_skips_blank_lines_without_calling_translator()
    {
        var orchestrator = new FakeOrchestrator();
        var builder = new OcrOverlayContentBuilder(orchestrator);
        var ocrResult = new OcrResult(new[]
        {
            new OcrTextLine("", new ScreenRect(0, 0, 100, 20)),
            new OcrTextLine("   ", new ScreenRect(0, 30, 100, 20)),
            new OcrTextLine("Real text", new ScreenRect(0, 60, 100, 20)),
        });

        var blocks = await builder.BuildAsync(ocrResult, EnJa, CancellationToken.None);

        Assert.Single(blocks);
        Assert.Equal("[JA]Real text", blocks[0].TranslatedText);
        Assert.Single(orchestrator.TranslatedInputs);
    }

    [Fact]
    public async Task BuildAsync_skips_line_when_translation_throws()
    {
        var orchestrator = new FakeOrchestrator(text =>
            text == "bad" ? throw new NoTranslatorAvailableException("失敗") : new TranslationResult($"[JA]{text}", "Fake", TimeSpan.Zero));
        var builder = new OcrOverlayContentBuilder(orchestrator);
        var ocrResult = new OcrResult(new[]
        {
            new OcrTextLine("bad", new ScreenRect(0, 0, 100, 20)),
            new OcrTextLine("good", new ScreenRect(0, 30, 100, 20)),
        });

        var blocks = await builder.BuildAsync(ocrResult, EnJa, CancellationToken.None);

        Assert.Single(blocks);
        Assert.Equal("[JA]good", blocks[0].TranslatedText);
    }

    [Fact]
    public async Task BuildAsync_returns_empty_list_when_no_lines()
    {
        var orchestrator = new FakeOrchestrator();
        var builder = new OcrOverlayContentBuilder(orchestrator);
        var ocrResult = new OcrResult(Array.Empty<OcrTextLine>());

        var blocks = await builder.BuildAsync(ocrResult, EnJa, CancellationToken.None);

        Assert.Empty(blocks);
    }

    [Fact]
    public async Task BuildAsync_translates_lines_concurrently_not_sequentially()
    {
        var orchestrator = new DelayedFakeOrchestrator(delay: TimeSpan.FromMilliseconds(100));
        var builder = new OcrOverlayContentBuilder(orchestrator);
        var ocrResult = new OcrResult(Enumerable.Range(0, 5)
            .Select(i => new OcrTextLine($"line{i}", new ScreenRect(0, i * 20, 100, 20)))
            .ToArray());

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var blocks = await builder.BuildAsync(ocrResult, EnJa, CancellationToken.None);
        sw.Stop();

        Assert.Equal(5, blocks.Count);
        // 直列実行なら5*100ms=500ms以上かかるはず。並列実行であれば単発の100ms程度で完了する。
        Assert.True(sw.Elapsed < TimeSpan.FromMilliseconds(400),
            $"並列実行されていない可能性があります(所要時間: {sw.Elapsed.TotalMilliseconds}ms)");
    }

    [Fact]
    public async Task BuildAsync_preserves_original_line_order_even_with_concurrent_translation()
    {
        var orchestrator = new DelayedFakeOrchestrator(delay: TimeSpan.FromMilliseconds(10));
        var builder = new OcrOverlayContentBuilder(orchestrator);
        var ocrResult = new OcrResult(new[]
        {
            new OcrTextLine("first", new ScreenRect(0, 0, 100, 20)),
            new OcrTextLine("second", new ScreenRect(0, 20, 100, 20)),
            new OcrTextLine("third", new ScreenRect(0, 40, 100, 20)),
        });

        var blocks = await builder.BuildAsync(ocrResult, EnJa, CancellationToken.None);

        Assert.Equal("[JA]first", blocks[0].TranslatedText);
        Assert.Equal("[JA]second", blocks[1].TranslatedText);
        Assert.Equal("[JA]third", blocks[2].TranslatedText);
    }
}

public sealed class DelayedFakeOrchestrator : ITranslationOrchestrator
{
    private readonly TimeSpan _delay;

    public DelayedFakeOrchestrator(TimeSpan delay) => _delay = delay;

    public async Task<TranslationResult> TranslateAsync(string sourceText, LanguagePair languagePair, CancellationToken cancellationToken)
    {
        await Task.Delay(_delay, cancellationToken);
        return new TranslationResult($"[JA]{sourceText}", "Fake", _delay);
    }
}
