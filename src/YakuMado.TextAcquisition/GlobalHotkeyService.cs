using System.Runtime.InteropServices;
using System.Windows.Interop;
using YakuMado.Core.Input;

namespace YakuMado.TextAcquisition;

/// <summary>
/// RegisterHotKey/UnregisterHotKeyによるグローバルホットキー登録。
/// メッセージのみを受け取る隠しウィンドウ(HwndSource)を内部で保持する。
/// 実OSフックへの依存が強いため自動テスト対象外(手動/E2Eで検証)。
/// </summary>
public sealed class GlobalHotkeyService : IHotkeyService, IDisposable
{
    private const int WM_HOTKEY = 0x0312;
    private const int HotkeyId = 0xB001;

    private readonly uint _modifiers;
    private readonly uint _virtualKey;
    private HwndSource? _hwndSource;

    public event EventHandler? SelectionTranslateRequested;

    public GlobalHotkeyService(uint modifiers, uint virtualKey)
    {
        _modifiers = modifiers;
        _virtualKey = virtualKey;
    }

    public void Register()
    {
        var parameters = new HwndSourceParameters("YakuMadoHotkeyMessageWindow")
        {
            WindowStyle = 0,
            Width = 0,
            Height = 0,
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        RegisterHotKey(_hwndSource.Handle, HotkeyId, _modifiers, _virtualKey);
    }

    public void Unregister()
    {
        if (_hwndSource == null) return;
        UnregisterHotKey(_hwndSource.Handle, HotkeyId);
        _hwndSource.RemoveHook(WndProc);
        _hwndSource.Dispose();
        _hwndSource = null;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == HotkeyId)
        {
            SelectionTranslateRequested?.Invoke(this, EventArgs.Empty);
            handled = true;
        }
        return IntPtr.Zero;
    }

    public void Dispose() => Unregister();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
