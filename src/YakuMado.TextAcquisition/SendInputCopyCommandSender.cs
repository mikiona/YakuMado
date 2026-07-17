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
        Console.WriteLine($"[SendCopy] Alt押下検出: {altPressed}");

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
        var sent = SendInput((uint)array.Length, array, Marshal.SizeOf<INPUT>());
        Console.WriteLine($"[SendCopy] SendInput結果: {sent}/{array.Length}件受理 (0の場合はGetLastErrorで原因確認要)");
        if (sent == 0)
        {
            Console.WriteLine($"[SendCopy] GetLastError: {Marshal.GetLastWin32Error()}");
        }
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

    // WindowsネイティブのINPUT共用体はMOUSEINPUT/KEYBDINPUT/HARDWAREINPUTのうち
    // 最大のMOUSEINPUT(x64で32バイト)を基準にサイズが決まる(INPUT全体で40バイト)。
    // KEYBDINPUTのみを含めるとx64で32バイトになりSendInputに渡すcbSizeが実際の
    // ネイティブサイズと一致せず、SendInputが入力を1件も受理せず0を返す不具合があったため、
    // 未使用でもMOUSEINPUTを共用体に含めてサイズを一致させる。
    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
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
