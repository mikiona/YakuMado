namespace YakuMado.TextAcquisition;

/// <summary>
/// System.Windows.Clipboardを用いた実装。PoC1で確認した通り、単発のSetText/GetTextは
/// 他プロセスとの競合で失敗することがあるため、SetDataObjectのリトライ機構を使用する。
/// STAスレッドからの呼び出しが前提。
/// </summary>
public sealed class WpfClipboardAccessor : IClipboardAccessor
{
    public bool TryGetText(out string? text)
    {
        try
        {
            if (System.Windows.Clipboard.ContainsText())
            {
                text = System.Windows.Clipboard.GetText();
                return true;
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            // 他プロセスがクリップボードを占有している場合は「未取得」として扱う
        }
        text = null;
        return false;
    }

    public void SetText(string text)
    {
        // System.Windows.Clipboardにはリトライ付きオーバーロードが無いため、
        // PoC1で確認した競合(他プロセスによる一時占有)に対応するため手動でリトライする。
        const int maxRetries = 10;
        const int retryDelayMs = 50;
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                System.Windows.Clipboard.SetText(text);
                return;
            }
            catch (System.Runtime.InteropServices.COMException) when (i < maxRetries - 1)
            {
                System.Threading.Thread.Sleep(retryDelayMs);
            }
        }
    }

    public void Clear()
    {
        try
        {
            System.Windows.Clipboard.Clear();
        }
        catch (System.Runtime.InteropServices.COMException)
        {
        }
    }
}
