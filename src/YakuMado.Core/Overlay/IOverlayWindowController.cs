namespace YakuMado.Core.Overlay;

public readonly record struct ScreenPoint(int X, int Y);

public record OverlayTextBlock(string TranslatedText, Ocr.ScreenRect BoundingBox);

/// <summary>画面オーバーレイ翻訳の表示制御抽象化。クリックスルー(WS_EX_TRANSPARENT等)は実装側の責務。</summary>
public interface IOverlayWindowController
{
    void ShowOverlay(IReadOnlyCollection<OverlayTextBlock> textBlocks);

    void ClearOverlay();
}

/// <summary>選択テキスト翻訳結果のポップアップ表示制御抽象化。</summary>
public interface ISelectionPopupController
{
    void ShowPopup(string translatedText, ScreenPoint anchor);
}
