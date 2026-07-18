using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Multikeys
{
    /// <summary>
    /// Verbindet den globalen Tastatur-Hook mit dem Abspielen der Makros.
    /// Wird eine Trigger-Taste gedrueckt, spielt die Engine die zugehoerige
    /// Tastenfolge in einem Hintergrund-Thread ab.
    /// </summary>
    public sealed class MacroEngine : IDisposable
    {
        private readonly KeyboardHook _hook = new KeyboardHook();
        private readonly object _sync = new object();

        // Trigger-Codes, die aktuell physisch gedrueckt gehalten werden
        // (verhindert Mehrfachausloesung durch die Tastenwiederholung).
        private readonly HashSet<int> _heldTriggers = new HashSet<int>();

        private AppConfig _config = new AppConfig();
        private volatile bool _running;
        // Verhindert, dass die gedrueckt gehaltene Hotkey-Taste mehrfach umschaltet.
        private bool _hotkeyActive;

        /// <summary>Wenn true, werden keine Makros ausgeloest, sondern Tasten an
        /// <see cref="RecordedKey"/> gemeldet (zum Aufnehmen einer Trigger-Taste
        /// oder einer Tastenfolge).</summary>
        public bool RecordMode { get; set; }

        /// <summary>Meldet aufgenommene Tasten im Aufnahmemodus (VK-Code, IsDown).</summary>
        public event Action<int, bool> RecordedKey;

        /// <summary>Meldet, wenn ein Makro ausgeloest wurde (Name des Makros).</summary>
        public event Action<string> MacroTriggered;

        /// <summary>Meldet, wenn die Engine per Hotkey umgeschaltet wurde (neuer Zustand).</summary>
        public event Action<bool> EngineToggled;

        public MacroEngine()
        {
            _hook.KeyEvent += OnKeyEvent;
        }

        public void Start()
        {
            _hook.Install();
            _running = true;
        }

        public void Stop()
        {
            _running = false;
            _hook.Uninstall();
            lock (_sync)
                _heldTriggers.Clear();
        }

        public void UpdateConfig(AppConfig config)
        {
            lock (_sync)
            {
                _config = config ?? new AppConfig();
            }
        }

        private void OnKeyEvent(object sender, KeyHookEventArgs e)
        {
            // Vom Programm selbst erzeugte Tasten nie erneut verarbeiten.
            if (e.Injected)
                return;

            if (RecordMode)
            {
                Action<int, bool> rec = RecordedKey;
                if (rec != null)
                    rec(e.VkCode, e.IsDown);
                // Aufgenommene Tasten abfangen, damit sie nichts ausloesen.
                e.Suppress = true;
                return;
            }

            // --- Umschalt-Hotkey pruefen (funktioniert auch bei deaktivierter Engine) ---
            Hotkey hotkey;
            lock (_sync)
                hotkey = _config.ToggleHotkey;

            if (hotkey != null && hotkey.IsSet && e.VkCode == hotkey.Vk)
            {
                if (e.IsDown)
                {
                    if (_hotkeyActive)
                    {
                        // Tastenwiederholung waehrend die Kombi gehalten wird -> ignorieren.
                        e.Suppress = true;
                        return;
                    }
                    if (ModifiersHeld(hotkey))
                    {
                        _hotkeyActive = true;
                        bool newState;
                        lock (_sync)
                        {
                            _config.EngineEnabled = !_config.EngineEnabled;
                            newState = _config.EngineEnabled;
                        }
                        e.Suppress = true;
                        Action<bool> toggled = EngineToggled;
                        if (toggled != null)
                            toggled(newState);
                        return;
                    }
                    // Modifier passten nicht: Taste normal durchlassen.
                }
                else if (_hotkeyActive)
                {
                    // Loslassen der Haupttaste nur unterdruecken, wenn das Druecken
                    // unterdrueckt wurde (sonst bleibt die Taste woanders haengen).
                    _hotkeyActive = false;
                    e.Suppress = true;
                    return;
                }
            }

            Macro macro = null;
            lock (_sync)
            {
                if (!_running || !_config.EngineEnabled)
                    return;

                if (e.IsDown)
                {
                    if (_heldTriggers.Contains(e.VkCode))
                    {
                        // Tastenwiederholung: bereits ausgeloest -> nur ggf. unterdruecken.
                        macro = FindMacro(e.VkCode);
                        if (macro != null && macro.SuppressTrigger)
                            e.Suppress = true;
                        return;
                    }

                    macro = FindMacro(e.VkCode);
                    if (macro != null)
                    {
                        _heldTriggers.Add(e.VkCode);
                        if (macro.SuppressTrigger)
                            e.Suppress = true;
                    }
                }
                else
                {
                    // Taste losgelassen
                    _heldTriggers.Remove(e.VkCode);
                    Macro up = FindMacro(e.VkCode);
                    if (up != null && up.SuppressTrigger)
                        e.Suppress = true;
                    return;
                }
            }

            if (macro != null)
            {
                string name = macro.Name;
                Action<string> trig = MacroTriggered;
                if (trig != null)
                    trig(name);

                Macro toPlay = macro;
                Task.Run(() => PlayMacro(toPlay));
            }
        }

        private static bool IsDownNow(int vk)
        {
            return (NativeMethods.GetAsyncKeyState(vk) & 0x8000) != 0;
        }

        private static bool ModifiersHeld(Hotkey hk)
        {
            if (hk.Ctrl && !IsDownNow(0x11)) return false;   // VK_CONTROL
            if (hk.Alt && !IsDownNow(0x12)) return false;    // VK_MENU
            if (hk.Shift && !IsDownNow(0x10)) return false;  // VK_SHIFT
            if (hk.Win && !(IsDownNow(0x5B) || IsDownNow(0x5C))) return false; // L/R Win
            return true;
        }

        private Macro FindMacro(int vkCode)
        {
            foreach (Macro m in _config.Macros)
            {
                if (m.Enabled && m.TriggerVkCode == vkCode)
                    return m;
            }
            return null;
        }

        private void PlayMacro(Macro macro)
        {
            try
            {
                foreach (MacroStep step in macro.Steps)
                {
                    switch (step.Type)
                    {
                        case StepType.KeyPress:
                            KeySender.KeyPress(step.VkCode);
                            break;
                        case StepType.KeyDown:
                            KeySender.KeyDown(step.VkCode);
                            break;
                        case StepType.KeyUp:
                            KeySender.KeyUp(step.VkCode);
                            break;
                        case StepType.Delay:
                            if (step.DelayMs > 0)
                                Thread.Sleep(step.DelayMs);
                            break;
                        case StepType.Text:
                            KeySender.TypeText(step.Text);
                            break;
                    }
                }
            }
            catch
            {
                // Ein fehlgeschlagener Schritt darf die Anwendung nicht abstuerzen lassen.
            }
        }

        public void Dispose()
        {
            _hook.KeyEvent -= OnKeyEvent;
            _hook.Dispose();
        }
    }
}
