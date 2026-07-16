using System.Windows.Automation;
using YakuMado.Core.TextAcquisition;

namespace YakuMado.TextAcquisition;

/// <summary>
/// UI Automation TextPattern.GetSelection()による選択テキスト取得。
/// フォーカス中の要素がTextPatternに対応していない場合はnullを返し、呼び出し側でのフォールバックに委ねる。
/// 実OSウィンドウへの依存が強いため自動テスト対象外(手動/E2Eで検証、PoC1で実機確認済み)。
/// </summary>
public sealed class UiAutomationSelectionProvider : ITextSelectionProvider
{
    public string ProviderName => "UIAutomation";

    public Task<string?> TryGetSelectedTextAsync(CancellationToken cancellationToken)
    {
        try
        {
            var focused = AutomationElement.FocusedElement;
            if (focused == null) return Task.FromResult<string?>(null);

            if (!focused.TryGetCurrentPattern(TextPattern.Pattern, out var patternObj))
            {
                return Task.FromResult<string?>(null);
            }

            var textPattern = (TextPattern)patternObj;
            var selection = textPattern.GetSelection();
            if (selection.Length == 0) return Task.FromResult<string?>(null);

            var text = selection[0].GetText(-1);
            return Task.FromResult<string?>(string.IsNullOrEmpty(text) ? null : text);
        }
        catch (Exception)
        {
            // 非対応コントロールでの例外(InvalidOperationException等)はフォールバック対象として扱う
            return Task.FromResult<string?>(null);
        }
    }
}
