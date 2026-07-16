using System.Runtime.InteropServices;

namespace YakuMado.TextAcquisition;

/// <summary>SendInputでCtrl+Cキー入力を合成して送出する実装。</summary>
public sealed class SendInputCopyCommandSender : ICopyCommandSender
{
    private const int VK_CONTROL = 0x11;
    private const int VK_C = 0x43;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int INPUT_KEYBOARD = 1;

    public void SendCopy()
    {
        var inputs = new INPUT[4];
        inputs[0] = KeyInput(VK_CONTROL, keyUp: false);
        inputs[1] = KeyInput(VK_C, keyUp: false);
        inputs[2] = KeyInput(VK_C, keyUp: true);
        inputs[3] = KeyInput(VK_CONTROL, keyUp: true);

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
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
