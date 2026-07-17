using YakuMado.Core.TextAcquisition;

namespace YakuMado.TextAcquisition;

/// <summary>
/// クリップボード方式による選択テキスト取得。Ctrl+C相当のコピー操作を送出し、
/// クリップボードの内容を読み取った後、元の内容へ必ず復元する。
/// </summary>
public sealed class ClipboardSelectionProvider : ITextSelectionProvider
{
    private readonly IClipboardAccessor _clipboard;
    private readonly ICopyCommandSender _copyCommandSender;
    private readonly int _pollIntervalMs;
    private readonly int _maxPollAttempts;

    public string ProviderName => "Clipboard";

    public ClipboardSelectionProvider(
        IClipboardAccessor clipboard,
        ICopyCommandSender copyCommandSender,
        int pollIntervalMs = 50,
        int maxPollAttempts = 10)
    {
        _clipboard = clipboard;
        _copyCommandSender = copyCommandSender;
        _pollIntervalMs = pollIntervalMs;
        _maxPollAttempts = maxPollAttempts;
    }

    public async Task<string?> TryGetSelectedTextAsync(CancellationToken cancellationToken)
    {
        _clipboard.TryGetText(out var originalText);
        Console.WriteLine($"[Clipboard] 元のクリップボード内容: {(originalText == null ? "なし" : $"あり(文字数{originalText.Length})")}");

        try
        {
            _clipboard.Clear();
            _copyCommandSender.SendCopy();
            Console.WriteLine("[Clipboard] コピーコマンドを送出し、ポーリングを開始します。");

            for (int i = 0; i < _maxPollAttempts; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (_clipboard.TryGetText(out var copiedText) && !string.IsNullOrEmpty(copiedText))
                {
                    Console.WriteLine($"[Clipboard] {i + 1}回目のポーリングで取得成功(文字数{copiedText.Length})");
                    return copiedText;
                }
                await Task.Delay(_pollIntervalMs, cancellationToken);
            }

            Console.WriteLine($"[Clipboard] {_maxPollAttempts}回ポーリングしても新しいテキストを検出できませんでした。");
            return null;
        }
        finally
        {
            if (originalText != null)
            {
                _clipboard.SetText(originalText);
            }
            else
            {
                _clipboard.Clear();
            }
        }
    }
}
