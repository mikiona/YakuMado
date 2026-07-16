namespace YakuMado.TextAcquisition;

/// <summary>クリップボード操作の抽象化。実装差し替えでユニットテスト可能にする。</summary>
public interface IClipboardAccessor
{
    bool TryGetText(out string? text);

    void SetText(string text);

    void Clear();
}

/// <summary>Ctrl+C相当のコピー操作を対象ウィンドウへ送出する抽象化。</summary>
public interface ICopyCommandSender
{
    void SendCopy();
}
