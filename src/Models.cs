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

    /// <summary>Die komplette gespeicherte Konfiguration.</summary>
    public sealed class AppConfig
    {
        public bool EngineEnabled { get; set; }
        public List<Macro> Macros { get; set; }

        public AppConfig()
        {
            EngineEnabled = true;
            Macros = new List<Macro>();
        }
    }
}
