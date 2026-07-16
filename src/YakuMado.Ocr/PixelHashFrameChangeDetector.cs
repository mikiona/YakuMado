using System.Security.Cryptography;
using YakuMado.Core.Ocr;

namespace YakuMado.Ocr;

/// <summary>ピクセルデータのハッシュ比較によるフレーム変化検知。</summary>
public sealed class PixelHashFrameChangeDetector : IFrameChangeDetector
{
    public bool HasChanged(IScreenBitmap previous, IScreenBitmap current)
    {
        if (previous.Width != current.Width || previous.Height != current.Height)
        {
            return true;
        }

        var previousHash = SHA256.HashData(previous.GetPixelData());
        var currentHash = SHA256.HashData(current.GetPixelData());

        return !previousHash.AsSpan().SequenceEqual(currentHash);
    }
}
