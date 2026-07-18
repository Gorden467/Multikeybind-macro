using System;
using System.Collections.Generic;

namespace Multikeys
{
    /// <summary>
    /// Sendet Tastatureingaben systemweit via SendInput.
    /// Verwendet Scancodes, damit die Ausgabe unabhaengig vom Tastaturlayout
    /// funktioniert und auch von Spielen zuverlaessig erkannt wird.
    /// </summary>
    public static class KeySender
    {
        /// <summary>
        /// Wie Tasten gesendet werden. Wird aus der Konfiguration gesetzt.
        /// Scancode = gut fuer Spiele; VirtualKey = fuer manche normalen Programme.
        /// </summary>
        public static KeySendMethod Method = KeySendMethod.Scancode;

        // Erweiterte Tasten (rechte Strg/Alt, Pfeiltasten, Pos1/Ende usw.) brauchen das Extended-Flag.
        private static readonly HashSet<int> ExtendedKeys = new HashSet<int>
        {
            0x21, // PageUp
            0x22, // PageDown
            0x23, // End
            0x24, // Home
            0x25, // Left
            0x26, // Up
            0x27, // Right
            0x28, // Down
            0x2D, // Insert
            0x2E, // Delete
            0xA3, // RControl
            0xA5, // RMenu (Right Alt)
            0x5B, // LWin
            0x5C, // RWin
            0x6F, // Divide (NumPad /)
            0x90, // NumLock
        };

        public static void KeyDown(int vkCode)
        {
            SendKey(vkCode, false);
        }

        public static void KeyUp(int vkCode)
        {
            SendKey(vkCode, true);
        }

        public static void KeyPress(int vkCode)
        {
            SendKey(vkCode, false);
            SendKey(vkCode, true);
        }

        private static void SendKey(int vkCode, bool keyUp)
        {
            uint scan = NativeMethods.MapVirtualKey((uint)vkCode, NativeMethods.MAPVK_VK_TO_VSC);

            uint flags = 0;
            if (keyUp)
                flags |= NativeMethods.KEYEVENTF_KEYUP;
            if (ExtendedKeys.Contains(vkCode))
                flags |= NativeMethods.KEYEVENTF_EXTENDEDKEY;

            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.u.ki.time = 0;
            input.u.ki.dwExtraInfo = IntPtr.Zero;

            if (Method == KeySendMethod.VirtualKey)
            {
                // Virtueller Tastencode: manche Programme erkennen nur diese Variante.
                input.u.ki.wVk = (ushort)vkCode;
                input.u.ki.wScan = (ushort)scan;
                input.u.ki.dwFlags = flags; // ohne SCANCODE-Flag -> wVk ist massgeblich
            }
            else
            {
                // Scancode (Standard): gut fuer Spiele / DirectInput.
                input.u.ki.wVk = 0;
                input.u.ki.wScan = (ushort)scan;
                input.u.ki.dwFlags = flags | NativeMethods.KEYEVENTF_SCANCODE;
            }

            NativeMethods.SendInput(1, new[] { input }, System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        /// <summary>
        /// Tippt beliebigen Unicode-Text zeichenweise (layoutunabhaengig).
        /// </summary>
        public static void TypeText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            List<NativeMethods.INPUT> inputs = new List<NativeMethods.INPUT>(text.Length * 2);
            foreach (char c in text)
            {
                inputs.Add(MakeUnicodeInput(c, false));
                inputs.Add(MakeUnicodeInput(c, true));
            }

            NativeMethods.SendInput((uint)inputs.Count, inputs.ToArray(),
                System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativeMethods.INPUT)));
        }

        private static NativeMethods.INPUT MakeUnicodeInput(char c, bool keyUp)
        {
            uint flags = NativeMethods.KEYEVENTF_UNICODE;
            if (keyUp)
                flags |= NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.INPUT input = new NativeMethods.INPUT();
            input.type = NativeMethods.INPUT_KEYBOARD;
            input.u.ki.wVk = 0;
            input.u.ki.wScan = c;
            input.u.ki.dwFlags = flags;
            input.u.ki.time = 0;
            input.u.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }
    }
}
