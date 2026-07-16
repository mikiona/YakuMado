using System.Drawing;
using System.Drawing.Imaging;
using YakuMado.Core.Ocr;

namespace YakuMado.Ocr;

/// <summary>System.Drawing.Bitmapをラップし、BGRA32の生ピクセルデータを提供する。</summary>
public sealed class GdiScreenBitmap : IScreenBitmap, IDisposable
{
    private readonly Bitmap _bitmap;

    public int Width => _bitmap.Width;
    public int Height => _bitmap.Height;

    public GdiScreenBitmap(Bitmap bitmap)
    {
        _bitmap = bitmap;
    }

    public byte[] GetPixelData()
    {
        var rect = new Rectangle(0, 0, _bitmap.Width, _bitmap.Height);
        var bitmapData = _bitmap.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            var byteCount = bitmapData.Stride * bitmapData.Height;
            var buffer = new byte[byteCount];
            System.Runtime.InteropServices.Marshal.Copy(bitmapData.Scan0, buffer, 0, byteCount);
            return buffer;
        }
        finally
        {
            _bitmap.UnlockBits(bitmapData);
        }
    }

    public void SaveToFile(string path) => _bitmap.Save(path, ImageFormat.Png);

    public void Dispose() => _bitmap.Dispose();
}
