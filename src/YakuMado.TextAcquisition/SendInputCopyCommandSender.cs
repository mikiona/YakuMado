using System.Runtime.InteropServices;

namespace YakuMado.TextAcquisition;

/// <summary>SendInputでCtrl+Cキー入力を合成して送出する実装。</summary>
public sealed class SendInputCopyCommandSender : ICopyCommandSender
{
    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt
    private const int VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int INPUT_KEYBOARD = 1;

    public void SendCopy()
    {
        // 本アプリのグローバルホットキーはCtrl+Alt+*であり、WM_HOTKEYはユーザーが
        // まだAltキーを物理的に押している間に発火する。この状態のままCtrl+Cを合成送出すると、
        // 対象アプリからはCtrl+Alt+Cとして解釈されコピーが実行されないため、
        // Altが押下中であれば先にキーアップを合成してから送出する。
        var altPressed = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;

        var inputs = new List<INPUT>(5);
        if (altPressed)
        {
            inputs.Add(KeyInput(VK_MENU, keyUp: true));
        }
        inputs.Add(KeyInput(VK_CONTROL, keyUp: false));
        inputs.Add(KeyInput(VK_C, keyUp: false));
        inputs.Add(KeyInput(VK_C, keyUp: true));
        inputs.Add(KeyInput(VK_CONTROL, keyUp: true));

        var array = inputs.ToArray();
        SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>());
    }

    private static INPUT KeyInput(int virtualKeyCode, bool keyUp) => new()
    {
        type = INPUT_KEYBOARD,
        U = new InputUnion
        {
            ki = new KEYBDINPUT
            {
                wVk = (ushort)virtualKeyCode,
                dwFlags = keyUp ? KEYEVENTF_KEYUP : 0,
            }
        }
    };

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public int type;
        public InputUnion U;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
}
