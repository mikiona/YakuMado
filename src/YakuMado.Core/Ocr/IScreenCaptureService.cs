namespace YakuMado.Core.Ocr;

/// <summary>画面領域のキャプチャ抽象化。一次実装はWindows.Graphics.Captureを使用する(BitBltは黒画面問題があり不採用)。</summary>
public interface IScreenCaptureService
{
    Task<IScreenBitmap> CaptureAsync(ScreenRect region, CancellationToken cancellationToken);
}

/// <summary>連続キャプチャ画像間の変化検知抽象化。再翻訳の要否判定に用いる。</summary>
public interface IFrameChangeDetector
{
    bool HasChanged(IScreenBitmap previous, IScreenBitmap current);
}
