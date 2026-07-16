using System.Drawing;
using YakuMado.Core.Ocr;

namespace YakuMado.Ocr;

/// <summary>
/// GDI(Graphics.CopyFromScreen、内部的にBitBlt)による画面キャプチャの暫定実装。
///
/// 【既知の制約】tech-research-screen-translation.md および architecture.md では
/// Windows.Graphics.Captureの採用を推奨している(BitBltは一部アプリで黒画面問題が
/// 報告されているため)。Windows.Graphics.Captureの実装(Direct3D11相互運用を含む)は
/// 実装コストが高く、本フェーズでは動作確認が容易なGDI版を暫定実装として先行させた。
/// Direct3D/DirectComposition描画を行う一部アプリ(動画再生・一部のハードウェア
/// アクセラレーション使用アプリ等)では黒画面になる可能性がある。将来的な置き換えが必要。
/// </summary>
public sealed class GdiScreenCaptureService : IScreenCaptureService
{
    public Task<IScreenBitmap> CaptureAsync(ScreenRect region, CancellationToken cancellationToken)
    {
        var bitmap = new Bitmap(region.Width, region.Height);
        using (var graphics = Graphics.FromImage(bitmap))
        {
            graphics.CopyFromScreen(region.X, region.Y, 0, 0, new Size(region.Width, region.Height));
        }

        IScreenBitmap result = new GdiScreenBitmap(bitmap);
        return Task.FromResult(result);
    }
}
