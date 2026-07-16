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
    private readonly string _bcp47LanguageTag;
    private readonly OcrEngine? _engine;

    public bool IsAvailable => _engine != null;

    public WindowsOcrEngine(string bcp47LanguageTag = "en")
    {
        _bcp47LanguageTag = bcp47LanguageTag;
        var language = new Language(bcp47LanguageTag);
        _engine = OcrEngine.TryCreateFromLanguage(language);
    }

    public async Task<CoreOcrResult> RecognizeAsync(IScreenBitmap bitmap, CancellationToken cancellationToken)
    {
        var engine = _engine ?? throw new InvalidOperationException(
            $"OCR言語 '{_bcp47LanguageTag}' が対応言語パックとしてインストールされていません。" +
            "設定 > 時刻と言語 > 言語と地域 から言語パックを追加してください。");

        using var softwareBitmap = new SoftwareBitmap(
            BitmapPixelFormat.Bgra8, bitmap.Width, bitmap.Height, BitmapAlphaMode.Premultiplied);
        softwareBitmap.CopyFromBuffer(bitmap.GetPixelData().AsBuffer());

        cancellationToken.ThrowIfCancellationRequested();
        var ocrResult = await engine.RecognizeAsync(softwareBitmap);

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
