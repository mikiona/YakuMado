using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using YakuMado.Core.Ocr;
using CoreOcrResult = YakuMado.Core.Ocr.OcrResult;

namespace YakuMado.Ocr;

/// <summary>
/// Windows.Media.Ocr.OcrEngineをラップする一次実装。Windows 10 10.0.10240.0
/// (UniversalApiContract v1.0)から利用可能(tech-research-screen-translation.mdで一次ソース確認済み)。
/// 実OS APIへの依存が強いため自動テスト対象外(手動/E2Eで検証)。
/// </summary>
public sealed class WindowsOcrEngine : IOcrEngine
{
    private readonly OcrEngine _engine;

    public WindowsOcrEngine(string bcp47LanguageTag = "en")
    {
        var language = new Language(bcp47LanguageTag);
        _engine = OcrEngine.TryCreateFromLanguage(language)
            ?? throw new InvalidOperationException(
                $"OCR言語 '{bcp47LanguageTag}' が対応言語パックとしてインストールされていません。");
    }

    public async Task<CoreOcrResult> RecognizeAsync(IScreenBitmap bitmap, CancellationToken cancellationToken)
    {
        using var softwareBitmap = new SoftwareBitmap(
            BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(bitmap.GetPixelData().AsBuffer());

        cancellationToken.ThrowIfCancellationRequested();
        var ocrResult = await _engine.RecognizeAsync(softwareBitmap);

        var lines = ocrResult.Lines
            .Select(line => new OcrTextLine(line.Text, ComputeBoundingRect(line)))
            .ToList();

        return new CoreOcrResult(lines);
    }

    private static ScreenRect ComputeBoundingRect(OcrLine line)
    {
        if (line.Words.Count == 0) return new ScreenRect(0, 0, 0, 0);

        var left = line.Words.Min(w => w.BoundingRect.Left);
        var top = line.Words.Min(w => w.BoundingRect.Top);
        var right = line.Words.Max(w => w.BoundingRect.Right);
        var bottom = line.Words.Max(w => w.BoundingRect.Bottom);

        return new ScreenRect((int)left, (int)top, (int)(right - left), (int)(bottom - top));
    }
}
