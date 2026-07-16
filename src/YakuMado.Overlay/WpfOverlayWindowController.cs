using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using YakuMado.Core.Overlay;

namespace YakuMado.Overlay;

/// <summary>
/// WPFウィンドウによる画面オーバーレイ表示。WS_EX_LAYERED|TRANSPARENT|NOACTIVATEで
/// クリックスルーを実現する(PoC3でWindowFromPointのヒットテストにより機構自体を確認済み)。
/// STAスレッド(WPF Dispatcherが動作するスレッド)からの呼び出しが前提。
/// </summary>
public sealed class WpfOverlayWindowController : IOverlayWindowController
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_NOACTIVATE = 0x8000000;

    private Window? _overlayWindow;

    public void ShowOverlay(IReadOnlyCollection<OverlayTextBlock> textBlocks)
    {
        ClearOverlay();

        if (textBlocks.Count == 0) return;

        var canvas = new Canvas();
        foreach (var block in textBlocks)
        {
            var textBlock = new TextBlock
            {
                Text = block.TranslatedText,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Foreground = Brushes.White,
                FontSize = 14,
                Padding = new Thickness(2),
            };
            Canvas.SetLeft(textBlock, block.BoundingBox.X);
            Canvas.SetTop(textBlock, block.BoundingBox.Y);
            canvas.Children.Add(textBlock);
        }

        var window = new Window
        {
            WindowStyle = WindowStyle.None,
            AllowsTransparency = true,
            Background = Brushes.Transparent,
            Topmost = true,
            ShowInTaskbar = false,
            Left = 0,
            Top = 0,
            Width = SystemParameters.PrimaryScreenWidth,
            Height = SystemParameters.PrimaryScreenHeight,
            Content = canvas,
        };

        window.SourceInitialized += (_, _) => ApplyClickThroughStyle(window);
        window.Show();

        _overlayWindow = window;
    }

    public void ClearOverlay()
    {
        _overlayWindow?.Close();
        _overlayWindow = null;
    }

    private static void ApplyClickThroughStyle(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE);
    }

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
