using YakuMado.Core.Ocr;
using YakuMado.Ocr;

namespace YakuMado.Ocr.Tests;

public sealed class FakeScreenBitmap : IScreenBitmap
{
    private readonly byte[] _pixelData;
    public int Width { get; }
    public int Height { get; }

    public FakeScreenBitmap(int width, int height, byte[] pixelData)
    {
        Width = width;
        Height = height;
        _pixelData = pixelData;
    }

    public byte[] GetPixelData() => _pixelData;
}

public class PixelHashFrameChangeDetectorTests
{
    [Fact]
    public void HasChanged_returns_false_for_identical_pixel_data()
    {
        var detector = new PixelHashFrameChangeDetector();
        var a = new FakeScreenBitmap(2, 2, new byte[] { 1, 2, 3, 4 });
        var b = new FakeScreenBitmap(2, 2, new byte[] { 1, 2, 3, 4 });

        Assert.False(detector.HasChanged(a, b));
    }

    [Fact]
    public void HasChanged_returns_true_for_different_pixel_data()
    {
        var detector = new PixelHashFrameChangeDetector();
        var a = new FakeScreenBitmap(2, 2, new byte[] { 1, 2, 3, 4 });
        var b = new FakeScreenBitmap(2, 2, new byte[] { 1, 2, 3, 9 });

        Assert.True(detector.HasChanged(a, b));
    }

    [Fact]
    public void HasChanged_returns_true_when_dimensions_differ()
    {
        var detector = new PixelHashFrameChangeDetector();
        var a = new FakeScreenBitmap(2, 2, new byte[] { 1, 2, 3, 4 });
        var b = new FakeScreenBitmap(3, 2, new byte[] { 1, 2, 3, 4, 5, 6 });

        Assert.True(detector.HasChanged(a, b));
    }
}
