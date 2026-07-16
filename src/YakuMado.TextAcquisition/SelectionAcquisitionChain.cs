using YakuMado.Core.TextAcquisition;

namespace YakuMado.TextAcquisition;

/// <summary>
/// 選択テキスト取得の3段階フォールバックを束ねる。各プロバイダを順に試行し、
/// null/空文字/例外のいずれの場合も次のプロバイダへフォールバックする。
/// </summary>
public sealed class SelectionAcquisitionChain : ISelectionAcquisitionChain
{
    private readonly IReadOnlyList<ITextSelectionProvider> _providers;

    public SelectionAcquisitionChain(IReadOnlyList<ITextSelectionProvider> providers)
    {
        _providers = providers;
    }

    public async Task<string?> AcquireAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string? result;
            try
            {
                result = await provider.TryGetSelectedTextAsync(cancellationToken);
            }
            catch (Exception)
            {
                continue;
            }

            if (!string.IsNullOrEmpty(result))
            {
                return result;
            }
        }

        return null;
    }
}
