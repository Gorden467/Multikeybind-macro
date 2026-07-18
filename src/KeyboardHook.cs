using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Multikeys
{
    /// <summary>
    /// Ereignisdaten eines global abgefangenen Tastendrucks.
    /// Suppress = true verhindert, dass die Taste an andere Programme weitergereicht wird.
    /// </summary>
    public sealed class KeyHookEventArgs : EventArgs
    {
        public int VkCode;
        public bool IsDown;
        public bool Injected;
        public bool Suppress;
    }

    /// <summary>
    /// Systemweiter Low-Level-Keyboard-Hook (WH_KEYBOARD_LL).
    /// Arbeitet geräteunabhängig und funktioniert daher mit jeder Tastatur.
    /// </summary>
    public sealed class KeyboardHook : IDisposable
    {
        private IntPtr _hookHandle = IntPtr.Zero;
        // Verhindert, dass der GC den Delegate einsammelt, solange der Hook aktiv ist.
        private NativeMethods.LowLevelKeyboardProc _proc;

        public event EventHandler<KeyHookEventArgs> KeyEvent;

        public void Install()
        {
            if (_hookHandle != IntPtr.Zero)
                return;

            _proc = HookCallback;
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
            {
                _hookHandle = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WH_KEYBOARD_LL,
                    _proc,
                    NativeMethods.GetModuleHandle(module.ModuleName),
                    0);
            }

            if (_hookHandle == IntPtr.Zero)
                throw new InvalidOperationException("Der Keyboard-Hook konnte nicht installiert werden (Fehler " + Marshal.GetLastWin32Error() + ").");
        }

        public void Uninstall()
        {
            if (_hookHandle == IntPtr.Zero)
                return;

            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
            _proc = null;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                bool isDown = msg == NativeMethods.WM_KEYDOWN || msg == NativeMethods.WM_SYSKEYDOWN;
                bool isUp = msg == NativeMethods.WM_KEYUP || msg == NativeMethods.WM_SYSKEYUP;

                if (isDown || isUp)
                {
                    NativeMethods.KBDLLHOOKSTRUCT data =
                        (NativeMethods.KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(NativeMethods.KBDLLHOOKSTRUCT));

                    EventHandler<KeyHookEventArgs> handler = KeyEvent;
                    if (handler != null)
                    {
                        KeyHookEventArgs args = new KeyHookEventArgs();
                        args.VkCode = (int)data.vkCode;
                        args.IsDown = isDown;
                        args.Injected = (data.flags & NativeMethods.LLKHF_INJECTED) != 0;

                        handler(this, args);

                        if (args.Suppress)
                            return (IntPtr)1;
                    }
                }
            }

            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        public void Dispose()
        {
            Uninstall();
        }
    }
}
