using YakuMado.TextAcquisition;

namespace YakuMado.TextAcquisition.Tests;

/// <summary>テスト用のフェイククリップボード。実際のOSクリップボードには触れない。</summary>
public sealed class FakeClipboardAccessor : IClipboardAccessor
{
    private string? _content;
    public int SetTextCallCount { get; private set; }
    public int ClearCallCount { get; private set; }

    public FakeClipboardAccessor(string? initialContent) => _content = initialContent;

    public bool TryGetText(out string? text)
    {
        text = _content;
        return _content != null;
    }

    public void SetText(string text)
    {
        _content = text;
        SetTextCallCount++;
    }

    public void Clear()
    {
        _content = null;
        ClearCallCount++;
    }

    /// <summary>テストから直接クリップボードの中身を変える(SendCopy後の状態を模擬するため)。</summary>
    public void SimulateExternalWrite(string? text) => _content = text;
}

public sealed class FakeCopyCommandSender : ICopyCommandSender
{
    private readonly Action _onSendCopy;
    public int SendCopyCallCount { get; private set; }

    public FakeCopyCommandSender(Action onSendCopy) => _onSendCopy = onSendCopy;

    public void SendCopy()
    {
        SendCopyCallCount++;
        _onSendCopy();
    }
}

public class ClipboardSelectionProviderTests
{
    [Fact]
    public async Task TryGetSelectedTextAsync_returns_text_copied_via_SendCopy()
    {
        var clipboard = new FakeClipboardAccessor(initialContent: null);
        var copySender = new FakeCopyCommandSender(() => clipboard.SimulateExternalWrite("selected text"));
        var provider = new ClipboardSelectionProvider(clipboard, copySender, pollIntervalMs: 1, maxPollAttempts: 5);

        var result = await provider.TryGetSelectedTextAsync(CancellationToken.None);

        Assert.Equal("selected text", result);
    }

    [Fact]
    public async Task TryGetSelectedTextAsync_restores_original_clipboard_content_after_success()
    {
        var clipboard = new FakeClipboardAccessor(initialContent: "original clipboard content");
        var copySender = new FakeCopyCommandSender(() => clipboard.SimulateExternalWrite("selected text"));
        var provider = new ClipboardSelectionProvider(clipboard, copySender, pollIntervalMs: 1, maxPollAttempts: 5);

        await provider.TryGetSelectedTextAsync(CancellationToken.None);

        clipboard.TryGetText(out var restored);
        Assert.Equal("original clipboard content", restored);
    }

    [Fact]
    public async Task TryGetSelectedTextAsync_clears_clipboard_on_restore_when_there_was_no_original_content()
    {
        var clipboard = new FakeClipboardAccessor(initialContent: null);
        var copySender = new FakeCopyCommandSender(() => clipboard.SimulateExternalWrite("selected text"));
        var provider = new ClipboardSelectionProvider(clipboard, copySender, pollIntervalMs: 1, maxPollAttempts: 5);

        await provider.TryGetSelectedTextAsync(CancellationToken.None);

        Assert.True(clipboard.ClearCallCount >= 1);
        clipboard.TryGetText(out var restored);
        Assert.Null(restored);
    }

    [Fact]
    public async Task TryGetSelectedTextAsync_restores_original_content_even_when_copy_yields_nothing()
    {
        var clipboard = new FakeClipboardAccessor(initialContent: "original");
        // SendCopyしても何も変化しない(選択なし)状況を模擬
        var copySender = new FakeCopyCommandSender(() => { });
        var provider = new ClipboardSelectionProvider(clipboard, copySender, pollIntervalMs: 1, maxPollAttempts: 3);

        var result = await provider.TryGetSelectedTextAsync(CancellationToken.None);

        Assert.Null(result);
        clipboard.TryGetText(out var restored);
        Assert.Equal("original", restored);
    }

    [Fact]
    public async Task TryGetSelectedTextAsync_polls_until_new_content_appears()
    {
        // SendCopy直後は反映されず、数回のポーリングを経て初めて反映される遅延コピーを模擬する
        var delayedClipboard = new DelayedFakeClipboardAccessor(appearAfterPolls: 3, contentWhenAppeared: "delayed text");
        var provider = new ClipboardSelectionProvider(delayedClipboard, new FakeCopyCommandSender(() => { }), pollIntervalMs: 1, maxPollAttempts: 10);

        var result = await provider.TryGetSelectedTextAsync(CancellationToken.None);

        Assert.Equal("delayed text", result);
    }
}

/// <summary>N回ポーリングされてから初めてテキストが現れる状況を模擬するフェイク。</summary>
public sealed class DelayedFakeClipboardAccessor : IClipboardAccessor
{
    private readonly int _appearAfterPolls;
    private readonly string _contentWhenAppeared;
    private int _getCallCount;
    private bool _cleared = true;

    public DelayedFakeClipboardAccessor(int appearAfterPolls, string contentWhenAppeared)
    {
        _appearAfterPolls = appearAfterPolls;
        _contentWhenAppeared = contentWhenAppeared;
    }

    public bool TryGetText(out string? text)
    {
        _getCallCount++;
        if (_cleared && _getCallCount >= _appearAfterPolls)
        {
            text = _contentWhenAppeared;
            return true;
        }
        text = null;
        return false;
    }

    public void SetText(string text) { }

    public void Clear() => _cleared = true;
}
