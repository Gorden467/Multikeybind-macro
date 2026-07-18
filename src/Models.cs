using System.Collections.Generic;

namespace Multikeys
{
    /// <summary>Art eines einzelnen Schrittes innerhalb eines Makros.</summary>
    public enum StepType
    {
        KeyPress = 0, // Taste druecken und wieder loslassen
        KeyDown = 1,  // Taste gedrueckt halten
        KeyUp = 2,    // Taste loslassen
        Delay = 3,    // Pause in Millisekunden
        Text = 4      // Text tippen
    }

    /// <summary>Ein einzelner Schritt der abgespielten Tastenfolge.</summary>
    public sealed class MacroStep
    {
        public StepType Type { get; set; }
        public int VkCode { get; set; }
        public int DelayMs { get; set; }
        public string Text { get; set; }

        public MacroStep()
        {
            Text = "";
        }

        public override string ToString()
        {
            switch (Type)
            {
                case StepType.KeyPress:
                    return "Druecke  " + KeyNames.NameOf(VkCode);
                case StepType.KeyDown:
                    return "Halte    " + KeyNames.NameOf(VkCode);
                case StepType.KeyUp:
                    return "Loslassen " + KeyNames.NameOf(VkCode);
                case StepType.Delay:
                    return "Pause    " + DelayMs + " ms";
                case StepType.Text:
                    return "Text     \"" + Text + "\"";
                default:
                    return "?";
            }
        }
    }

    /// <summary>Ein Makro: eine Trigger-Taste plus die Folge, die dann abgespielt wird.</summary>
    public sealed class Macro
    {
        public string Name { get; set; }
        public int TriggerVkCode { get; set; }
        // Wenn true, wird der Trigger-Tastendruck nicht an andere Programme weitergereicht.
        public bool SuppressTrigger { get; set; }
        public bool Enabled { get; set; }
        public List<MacroStep> Steps { get; set; }

        public Macro()
        {
            Name = "Neues Makro";
            Enabled = true;
            SuppressTrigger = true;
            Steps = new List<MacroStep>();
        }
    }

    /// <summary>Hilfsfunktionen zum Erkennen von Modifier-Tasten (Strg/Alt/Umschalt/Win).</summary>
    public static class KeyMods
    {
        public static bool IsCtrl(int vk) { return vk == 0x11 || vk == 0xA2 || vk == 0xA3; }
        public static bool IsAlt(int vk) { return vk == 0x12 || vk == 0xA4 || vk == 0xA5; }
        public static bool IsShift(int vk) { return vk == 0x10 || vk == 0xA0 || vk == 0xA1; }
        public static bool IsWin(int vk) { return vk == 0x5B || vk == 0x5C; }

        public static bool IsModifier(int vk)
        {
            return IsCtrl(vk) || IsAlt(vk) || IsShift(vk) || IsWin(vk);
        }
    }

    /// <summary>
    /// Eine frei waehlbare Tastenkombination: eine Haupttaste plus beliebige Modifier.
    /// Wird z. B. verwendet, um die Makros global an-/auszuschalten.
    /// </summary>
    public sealed class Hotkey
    {
        public int Vk { get; set; }     // Haupttaste (0 = nicht gesetzt)
        public bool Ctrl { get; set; }
        public bool Alt { get; set; }
        public bool Shift { get; set; }
        public bool Win { get; set; }

        public bool IsSet { get { return Vk != 0; } }

        public override string ToString()
        {
            if (!IsSet)
                return "(keiner)";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (Ctrl) sb.Append("Strg + ");
            if (Alt) sb.Append("Alt + ");
            if (Shift) sb.Append("Umschalt + ");
            if (Win) sb.Append("Win + ");
            sb.Append(KeyNames.NameOf(Vk));
            return sb.ToString();
        }
    }

    /// <summary>Die komplette gespeicherte Konfiguration.</summary>
    public sealed class AppConfig
    {
        public bool EngineEnabled { get; set; }
        public Hotkey ToggleHotkey { get; set; }
        public List<Macro> Macros { get; set; }

        public AppConfig()
        {
            EngineEnabled = true;
            ToggleHotkey = new Hotkey();
            Macros = new List<Macro>();
        }
    }
}
