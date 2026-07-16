namespace YakuMado.Core.TextAcquisition;

/// <summary>選択中テキストを取得する単一方式の抽象化(UI Automation・クリップボード・OCR等の各実装が担う)。</summary>
public interface ITextSelectionProvider
{
    string ProviderName { get; }

    Task<string?> TryGetSelectedTextAsync(CancellationToken cancellationToken);
}
