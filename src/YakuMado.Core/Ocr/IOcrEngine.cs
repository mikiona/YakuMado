namespace YakuMado.Core.Ocr;

/// <summary>画面キャプチャ画像からテキストを認識する抽象化。一次実装はWindows.Media.Ocr.OcrEngineをラップする。</summary>
public interface IOcrEngine
{
    /// <summary>対象言語のOCR言語パックがOSにインストールされているかどうかの自己申告。</summary>
    bool IsAvailable { get; }

    Task<OcrResult> RecognizeAsync(IScreenBitmap bitmap, CancellationToken cancellationToken);
}

/// <summary>画面キャプチャ結果のビットマップ抽象化。実装はInfrastructure層のキャプチャAPIに依存する。</summary>
public interface IScreenBitmap
{
    int Width { get; }
    int Height { get; }

    /// <summary>フレーム変化検知等で使用する生ピクセルデータ。</summary>
    byte[] GetPixelData();
}
