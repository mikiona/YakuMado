using YakuMado.Core.Ocr;
using YakuMado.Core.Overlay;
using YakuMado.Core.Translation;
using YakuMado.Translation.Orchestration;

namespace YakuMado.Overlay;

/// <summary>
/// OCR結果の各行を翻訳し、オーバーレイ表示ブロックへ変換する。空行や翻訳失敗行はスキップする。
/// 各行の翻訳は並列実行する(Phase 6性能ベンチマークで、未キャッシュの行が多い場合に逐次実行が
/// ボトルネックになると判明したため。docs/phase6-perf-benchmark.md参照)。
/// </summary>
public sealed class OcrOverlayContentBuilder
{
    private readonly ITranslationOrchestrator _orchestrator;

    public OcrOverlayContentBuilder(ITranslationOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public async Task<IReadOnlyList<OverlayTextBlock>> BuildAsync(
        OcrResult ocrResult,
        LanguagePair languagePair,
        CancellationToken cancellationToken)
    {
        var targetLines = ocrResult.Lines.Where(line => !string.IsNullOrWhiteSpace(line.Text)).ToList();

        var translationTasks = targetLines
            .Select(line => TranslateLineAsync(line, languagePair, cancellationToken))
            .ToArray();

        var results = await Task.WhenAll(translationTasks);

        return results.Where(r => r != null).Select(r => r!).ToList();
    }

    private async Task<OverlayTextBlock?> TranslateLineAsync(
        OcrTextLine line, LanguagePair languagePair, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _orchestrator.TranslateAsync(line.Text, languagePair, cancellationToken);
            return new OverlayTextBlock(result.TranslatedText, line.BoundingBox);
        }
        catch (Exception)
        {
            // この行の翻訳に失敗しても他の行の表示は継続する
            return null;
        }
    }
}
