using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Automation;

var mainThread = new Thread(RunPoc);
mainThread.SetApartmentState(ApartmentState.STA);
mainThread.Start();
mainThread.Join();
return;

static void RunPoc()
{
const string TestText = "This is a YakuMado selection acquisition PoC test sentence.";

Console.WriteLine("=== PoC1: 選択テキスト取得 実機検証 ===");

// 1. メモ帳を新規起動する(ユーザーの既存デスクトップ操作を妨げないよう新規ウィンドウのみ操作する)
// Windows 11のメモ帳はパッケージアプリのため起動プロセスと実ウィンドウのプロセスが異なる場合がある。
// Process.MainWindowHandleに依存せず、起動前後のトップレベルウィンドウ差分で新規ウィンドウを特定する。
var beforeHandles = new System.Collections.Generic.HashSet<IntPtr>();
EnumWindows((hwnd, _) => { beforeHandles.Add(hwnd); return true; }, IntPtr.Zero);

var psi = new ProcessStartInfo("notepad.exe") { UseShellExecute = true };
var proc = Process.Start(psi)!;
proc.WaitForInputIdle(5000);
Thread.Sleep(1000);

IntPtr mainHwnd = IntPtr.Zero;
for (int i = 0; i < 30 && mainHwnd == IntPtr.Zero; i++)
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
            mainHwnd = hwnd;
            return false;
        }
        return true;
    }, IntPtr.Zero);
    if (mainHwnd == IntPtr.Zero) Thread.Sleep(200);
}
Console.WriteLine($"メモ帳ウィンドウハンドル: {mainHwnd}");

if (mainHwnd == IntPtr.Zero)
{
    Console.WriteLine("結果: メモ帳ウィンドウの取得に失敗");
    return;
}

SetForegroundWindow(mainHwnd);
Thread.Sleep(300);

// 2. UI Automationでエディットコントロールを探す
var rootElement = AutomationElement.FromHandle(mainHwnd);
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

// 3. テキストを設定(ValuePatternで)
bool textSet = false;
if (editElement.TryGetCurrentPattern(ValuePattern.Pattern, out var valuePatternObj))
{
    try
    {
        ((ValuePattern)valuePatternObj).SetValue(TestText);
        textSet = true;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"ValuePattern.SetValue失敗: {ex.Message}");
    }
}
if (!textSet)
{
    // フォールバック: SendKeysでタイプ
    SetForegroundWindow(mainHwnd);
    Thread.Sleep(200);
    System.Windows.Forms.SendKeys.SendWait(TestText);
    Thread.Sleep(300);
}

// 4. 全選択(Ctrl+A)
SetForegroundWindow(mainHwnd);
Thread.Sleep(200);
System.Windows.Forms.SendKeys.SendWait("^a");
Thread.Sleep(300);

// 5. UI Automation TextPatternで選択範囲取得を試みる
string uiaResult = "未実施";
try
{
    if (editElement.TryGetCurrentPattern(TextPattern.Pattern, out var textPatternObj))
    {
        var textPattern = (TextPattern)textPatternObj;
        var selection = textPattern.GetSelection();
        if (selection.Length > 0)
        {
            var text = selection[0].GetText(-1);
            uiaResult = $"成功: '{text}'";
        }
        else
        {
            uiaResult = "選択範囲が空(0件)";
        }
    }
    else
    {
        uiaResult = "TextPatternが非対応(InvalidOperationException相当)";
    }
}
catch (Exception ex)
{
    uiaResult = $"例外: {ex.GetType().Name}: {ex.Message}";
}
Console.WriteLine($"[方式1: UI Automation TextPattern] {uiaResult}");

// 6. クリップボード方式(Ctrl+C→読取→復元)フォールバックの検証
string clipboardResult = "未実施";
var clipThread = new Thread(() =>
{
    try
    {
        string? originalClip = null;
        try { originalClip = System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null; } catch { }

        SetForegroundWindow(mainHwnd);
        Thread.Sleep(200);
        System.Windows.Forms.SendKeys.SendWait("^c");
        Thread.Sleep(300);

        string got = "";
        for (int i = 0; i < 10; i++)
        {
            try
            {
                got = System.Windows.Clipboard.GetText();
                if (!string.IsNullOrEmpty(got)) break;
            }
            catch { }
            Thread.Sleep(100);
        }
        clipboardResult = got == TestText ? $"成功: '{got}'" : $"不一致または空: '{got}'";

        // クリップボード復元
        try
        {
            if (originalClip != null) System.Windows.Clipboard.SetText(originalClip);
            else System.Windows.Clipboard.Clear();
        }
        catch { }
    }
    catch (Exception ex)
    {
        clipboardResult = $"例外: {ex.GetType().Name}: {ex.Message}";
    }
});
clipThread.SetApartmentState(ApartmentState.STA);
clipThread.Start();
clipThread.Join();
Console.WriteLine($"[方式2: クリップボード方式] {clipboardResult}");

// 後片付け: メモ帳を保存せず終了
try
{
    proc.Kill();
}
catch { }

Console.WriteLine("=== PoC1 完了 ===");

[DllImport("user32.dll")]
static extern bool SetForegroundWindow(IntPtr hWnd);

[DllImport("user32.dll")]
static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

[DllImport("user32.dll")]
static extern bool IsWindowVisible(IntPtr hWnd);

[DllImport("user32.dll", CharSet = CharSet.Auto)]
static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);
}

delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
