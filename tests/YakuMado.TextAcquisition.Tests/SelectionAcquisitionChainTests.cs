using YakuMado.Core.TextAcquisition;
using YakuMado.TextAcquisition;

namespace YakuMado.TextAcquisition.Tests;

public sealed class FakeTextSelectionProvider : ITextSelectionProvider
{
    private readonly string? _result;
    public string ProviderName { get; }
    public int CallCount { get; private set; }

    public FakeTextSelectionProvider(string providerName, string? result)
    {
        ProviderName = providerName;
        _result = result;
    }

    public Task<string?> TryGetSelectedTextAsync(CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}

public class SelectionAcquisitionChainTests
{
    [Fact]
    public async Task AcquireAsync_returns_result_from_first_provider_that_succeeds()
    {
        var uia = new FakeTextSelectionProvider("UIAutomation", "uia result");
        var clipboard = new FakeTextSelectionProvider("Clipboard", "clipboard result");
        var chain = new SelectionAcquisitionChain(new ITextSelectionProvider[] { uia, clipboard });

        var result = await chain.AcquireAsync(CancellationToken.None);

        Assert.Equal("uia result", result);
        Assert.Equal(0, clipboard.CallCount);
    }

    [Fact]
    public async Task AcquireAsync_falls_back_to_next_provider_when_first_returns_null()
    {
        var uia = new FakeTextSelectionProvider("UIAutomation", null);
        var clipboard = new FakeTextSelectionProvider("Clipboard", "clipboard result");
        var chain = new SelectionAcquisitionChain(new ITextSelectionProvider[] { uia, clipboard });

        var result = await chain.AcquireAsync(CancellationToken.None);

        Assert.Equal("clipboard result", result);
        Assert.Equal(1, uia.CallCount);
    }

    [Fact]
    public async Task AcquireAsync_falls_back_when_first_returns_empty_string()
    {
        var uia = new FakeTextSelectionProvider("UIAutomation", "");
        var clipboard = new FakeTextSelectionProvider("Clipboard", "clipboard result");
        var chain = new SelectionAcquisitionChain(new ITextSelectionProvider[] { uia, clipboard });

        var result = await chain.AcquireAsync(CancellationToken.None);

        Assert.Equal("clipboard result", result);
    }

    [Fact]
    public async Task AcquireAsync_returns_null_when_all_providers_fail()
    {
        var uia = new FakeTextSelectionProvider("UIAutomation", null);
        var clipboard = new FakeTextSelectionProvider("Clipboard", null);
        var chain = new SelectionAcquisitionChain(new ITextSelectionProvider[] { uia, clipboard });

        var result = await chain.AcquireAsync(CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task AcquireAsync_continues_to_next_provider_when_one_throws()
    {
        var throwing = new ThrowingTextSelectionProvider();
        var clipboard = new FakeTextSelectionProvider("Clipboard", "clipboard result");
        var chain = new SelectionAcquisitionChain(new ITextSelectionProvider[] { throwing, clipboard });

        var result = await chain.AcquireAsync(CancellationToken.None);

        Assert.Equal("clipboard result", result);
    }
}

public sealed class ThrowingTextSelectionProvider : ITextSelectionProvider
{
    public string ProviderName => "Throwing";

    public Task<string?> TryGetSelectedTextAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException("非対応コントロール");
}
