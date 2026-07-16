namespace YakuMado.Core.TextAcquisition;

/// <summary>選択テキスト取得の3段階フォールバック(UI Automation→クリップボード→範囲指定OCR)を束ねる抽象化。</summary>
public interface ISelectionAcquisitionChain
{
    Task<string?> AcquireAsync(CancellationToken cancellationToken);
}
