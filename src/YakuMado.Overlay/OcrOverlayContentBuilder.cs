using YakuMado.Core.Ocr;
using YakuMado.Core.Overlay;
using YakuMado.Core.Translation;
using YakuMado.Translation.Orchestration;

namespace YakuMado.Overlay;

/// <summary>OCR結果の各行を翻訳し、オーバーレイ表示ブロックへ変換する。空行や翻訳失敗行はスキップする。</summary>
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
        var blocks = new List<OverlayTextBlock>();

        foreach (var line in ocrResult.Lines)
        {
            if (string.IsNullOrWhiteSpace(line.Text)) continue;

            try
            {
                var result = await _orchestrator.TranslateAsync(line.Text, languagePair, cancellationToken);
                blocks.Add(new OverlayTextBlock(result.TranslatedText, line.BoundingBox));
            }
            catch (Exception)
            {
                // この行の翻訳に失敗しても他の行の表示は継続する
            }
        }

        return blocks;
    }
}
