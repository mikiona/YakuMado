using System.Drawing;
using System.Windows.Forms;

namespace YakuMado.App;

/// <summary>
/// タスクトレイ常駐アイコン(F8)。System.Windows.Forms.NotifyIconを使用する
/// (WPFにはネイティブのトレイアイコン機構が無いため標準的な相互運用手法を採用)。
/// 実OS UIへの依存が強いため自動テスト対象外(手動/E2Eで検証)。
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private readonly NotifyIcon _notifyIcon;

    public event EventHandler? ExitRequested;
    public event EventHandler? SelectionTranslateRequested;
    public event EventHandler? OverlayTranslateToggleRequested;

    public TrayIconController()
    {
        var exitItem = new ToolStripMenuItem("終了(&X)");
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);

        var selectionItem = new ToolStripMenuItem("選択テキスト翻訳(&T)  Ctrl+Alt+T");
        selectionItem.Click += (_, _) => SelectionTranslateRequested?.Invoke(this, EventArgs.Empty);

        var overlayItem = new ToolStripMenuItem("画面オーバーレイ翻訳(&O)  Ctrl+Alt+O");
        overlayItem.Click += (_, _) => OverlayTranslateToggleRequested?.Invoke(this, EventArgs.Empty);

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add(selectionItem);
        contextMenu.Items.Add(overlayItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "YakuMado",
            Visible = true,
            ContextMenuStrip = contextMenu,
        };
    }

    public void ShowBalloonTip(string title, string text)
    {
        _notifyIcon.BalloonTipTitle = title;
        _notifyIcon.BalloonTipText = text;
        _notifyIcon.ShowBalloonTip(3000);
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }
}
