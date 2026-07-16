namespace YakuMado.Core.Ocr;

public record OcrTextLine(string Text, ScreenRect BoundingBox);

public record OcrResult(IReadOnlyList<OcrTextLine> Lines);

public readonly record struct ScreenRect(int X, int Y, int Width, int Height);
