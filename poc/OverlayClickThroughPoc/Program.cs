using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Interop;
using System.Windows.Media;

var thread = new Thread(RunPoc);
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
return;

static void RunPoc()
{
    const int GWL_EXSTYLE = -20;
    const int WS_EX_LAYERED = 0x80000;
    const int WS_EX_TRANSPARENT = 0x20;
    const int WS_EX_NOACTIVATE = 0x8000000;
    const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    const uint MOUSEEVENTF_LEFTUP = 0x0004;
    const uint GA_ROOT = 2;

    Console.WriteLine("=== PoC3: オーバーレイ クリックスルー 実機検証 ===");

    // 1. メモ帳を新規起動し、ウィンドウ矩形を取得する
    var beforeHandles = new System.Collections.Generic.HashSet<IntPtr>();
    EnumWindows((hwnd, _) => { beforeHandles.Add(hwnd); return true; }, IntPtr.Zero);

    var psi = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
    var proc = Process.Start(psi)!;
    proc.WaitForInputIdle(5000);
    Thread.Sleep(1000);

    IntPtr notepadHwnd = IntPtr.Zero;
    for (int i = 0; i < 30 && notepadHwnd == IntPtr.Zero; i++)
    {
        var sb = new System.Text.StringBuilder(256);
        EnumWindows((hwnd, _) =>
        {
            if (beforeHandles.Contains(hwnd)) return true;
            if (!IsWindowVisible(hwnd)) return true;
            sb.Clear();
            GetWindowText(hwnd, sb, 256);
            var title = sb.ToString();
            if (title.Contains("メモ帳") || title.Contains("Notepad") || title.Contains("無題") || title.Contains("Untitled"))
            {
                notepadHwnd = hwnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);
        if (notepadHwnd == IntPtr.Zero) Thread.Sleep(200);
    }

    if (notepadHwnd == IntPtr.Zero)
    {
        Console.WriteLine("結果: メモ帳ウィンドウの取得に失敗");
        return;
    }
    Console.WriteLine($"メモ帳ウィンドウハンドル: {notepadHwnd}");

    SetForegroundWindow(notepadHwnd);
    Thread.Sleep(300);
    GetWindowRect(notepadHwnd, out var rect);
    Console.WriteLine($"メモ帳ウィンドウ矩形: Left={rect.Left}, Top={rect.Top}, Right={rect.Right}, Bottom={rect.Bottom}");

    // UI Automationでメモ帳のエディットコントロールを特定しておく(クリック前後のHasKeyboardFocusを比較するため)
    var rootElement = AutomationElement.FromHandle(notepadHwnd);
    var editCondition = new OrCondition(
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit),
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document));
    var editElement = rootElement.FindFirst(TreeScope.Descendants, editCondition);
    if (editElement == null)
    {
        Console.WriteLine("結果: UI Automationでエディットコントロールが見つからない");
        proc.Kill();
        return;
    }

    // 2. WPFオーバーレイウィンドウをメモ帳の矩形上に表示する
    var app = new Application();
    string clickThroughResult = "未実施";
    string styleCheckResult = "未実施";

    // メモ帳をいったん非アクティブにする(オーバーレイ経由のクリックで再アクティブ化されるか検証するため)。
    // メモ帳の矩形と重ならない画面左上隅に、非表示に近い小さなダミーウィンドウを表示してフォーカスを奪わせる
    // (他の実アプリのz-orderに触れると検証対象の矩形が別ウィンドウに覆われる恐れがあるため自前ウィンドウを使う)
    var dummy = new Window
    {
        Left = 0,
        Top = 0,
        Width = 50,
        Height = 50,
        WindowStyle = WindowStyle.ToolWindow,
        Content = new System.Windows.Controls.TextBlock { Text = "dummy" }
    };
    dummy.Show();
    dummy.Activate();
    Thread.Sleep(300);
    var foregroundBeforeClick = GetForegroundWindow();
    Console.WriteLine($"クリック前のフォアグラウンドウィンドウ: {foregroundBeforeClick} (メモ帳と一致: {foregroundBeforeClick == notepadHwnd})");

    var overlay = new Window
    {
        WindowStyle = WindowStyle.None,
        AllowsTransparency = true,
        Background = new SolidColorBrush(Color.FromArgb(64, 255, 0, 0)), // 半透明の赤(視認確認用)
        Topmost = true,
        ShowInTaskbar = false,
        Left = rect.Left,
        Top = rect.Top,
        Width = rect.Right - rect.Left,
        Height = rect.Bottom - rect.Top,
        Content = new System.Windows.Controls.TextBlock
        {
            Text = "YakuMado overlay PoC (click-through test)",
            Foreground = System.Windows.Media.Brushes.White,
            FontSize = 20,
            Margin = new Thickness(20)
        }
    };

    overlay.SourceInitialized += (_, _) =>
    {
        var hwndHelper = new WindowInteropHelper(overlay);
        var overlayHwnd = hwndHelper.Handle;
        int exStyle = GetWindowLong(overlayHwnd, GWL_EXSTYLE);
        int newExStyle = exStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE;
        SetWindowLong(overlayHwnd, GWL_EXSTYLE, newExStyle);

        int confirmedExStyle = GetWindowLong(overlayHwnd, GWL_EXSTYLE);
        bool layeredSet = (confirmedExStyle & WS_EX_LAYERED) != 0;
        bool transparentSet = (confirmedExStyle & WS_EX_TRANSPARENT) != 0;
        bool noActivateSet = (confirmedExStyle & WS_EX_NOACTIVATE) != 0;
        styleCheckResult = $"WS_EX_LAYERED={layeredSet}, WS_EX_TRANSPARENT={transparentSet}, WS_EX_NOACTIVATE={noActivateSet}";
        Console.WriteLine($"[拡張ウィンドウスタイル適用確認] {styleCheckResult}");
    };

    overlay.Show();

    var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
    timer.Tick += (_, _) =>
    {
        timer.Stop();
        try
        {
            // 3. WindowFromPointによる直接ヒットテスト検証(フォーカス奪取やSendInputの可否に依存しない、
            //    OSのヒットテスト機構そのものを問い合わせる決定的な方法)
            int centerX = (rect.Left + rect.Right) / 2;
            int centerY = (rect.Top + rect.Bottom) / 2;
            var hwndHelper = new WindowInteropHelper(overlay);
            var overlayHwnd = hwndHelper.Handle;

            var hitWithOverlay = WindowFromPoint(new POINT { X = centerX, Y = centerY });
            var hitRootWithOverlay = GetAncestor(hitWithOverlay, GA_ROOT);
            bool hitIsOverlay = hitWithOverlay == overlayHwnd || hitRootWithOverlay == overlayHwnd;
            bool hitIsNotepad = hitRootWithOverlay == notepadHwnd;

            Console.WriteLine($"WindowFromPoint(中心座標)={hitWithOverlay}, ルートウィンドウ={hitRootWithOverlay} (overlay={overlayHwnd}, notepad={notepadHwnd})");

            // 対照実験: 一時的にWS_EX_TRANSPARENTを外すと、ヒットテストがoverlay自身を返すことを確認する
            int exStyleNow = GetWindowLong(overlayHwnd, GWL_EXSTYLE);
            SetWindowLong(overlayHwnd, GWL_EXSTYLE, exStyleNow & ~WS_EX_TRANSPARENT);
            Thread.Sleep(100);
            var hitWithoutTransparent = WindowFromPoint(new POINT { X = centerX, Y = centerY });
            bool controlGroupHitsOverlay = hitWithoutTransparent == overlayHwnd;
            Console.WriteLine($"[対照実験] WS_EX_TRANSPARENTを外した状態でのWindowFromPoint={hitWithoutTransparent} (overlay自身と一致: {controlGroupHitsOverlay})");
            // 元に戻す
            SetWindowLong(overlayHwnd, GWL_EXSTYLE, exStyleNow);

            clickThroughResult = (hitIsNotepad && !hitIsOverlay && controlGroupHitsOverlay)
                ? $"成功: WS_EX_TRANSPARENT指定時はヒットテストがメモ帳({hitRootWithOverlay})を返し、対照実験(TRANSPARENT解除時)ではoverlay自身({hitWithoutTransparent})を返した。クリックスルーが機能していることをOSのヒットテスト機構レベルで確認"
                : $"要確認: hitIsNotepad={hitIsNotepad}, hitIsOverlay={hitIsOverlay}, controlGroupHitsOverlay={controlGroupHitsOverlay}";
            Console.WriteLine($"[クリックスルー検証] {clickThroughResult}");
        }
        finally
        {
            overlay.Close();
            dummy.Close();
            try { proc.Kill(); } catch { }
            app.Shutdown();
        }
    };
    timer.Start();

    app.Run();

    Console.WriteLine("=== PoC3 完了 ===");
}

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
static extern IntPtr GetForegroundWindow();

[DllImport("user32.dll")]
static extern IntPtr GetDesktopWindow();

[DllImport("user32.dll")]
static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

[DllImport("user32.dll")]
static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32.dll")]
static extern bool IsWindowVisible(IntPtr hWnd);

[DllImport("user32.dll", CharSet = CharSet.Auto)]
static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

[DllImport("user32.dll")]
static extern int GetWindowLong(IntPtr hWnd, int nIndex);

[DllImport("user32.dll")]
static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

[DllImport("user32.dll")]
static extern bool SetCursorPos(int x, int y);

[DllImport("user32.dll")]
static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

[DllImport("user32.dll")]
static extern IntPtr WindowFromPoint(POINT Point);

[DllImport("user32.dll")]
static extern IntPtr GetAncestor(IntPtr hwnd, uint gaFlags);

delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

[StructLayout(LayoutKind.Sequential)]
struct RECT { public int Left, Top, Right, Bottom; }

[StructLayout(LayoutKind.Sequential)]
struct POINT { public int X, Y; }
