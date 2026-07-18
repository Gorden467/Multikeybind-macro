using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Multikeys
{
    /// <summary>
    /// Faengt per globalem Hook eine einzelne Taste ab. Optional kann anschliessend
    /// die Art des Schrittes gewaehlt werden (Druecken / Halten / Loslassen).
    /// </summary>
    public sealed class KeyCaptureDialog : Form
    {
        private readonly MacroEngine _engine;
        private readonly bool _chooseKind;
        private readonly Label _label;
        private readonly Panel _kindPanel;
        private bool _captured;

        public int CapturedVk { get; private set; }
        public StepType ChosenKind { get; private set; }

        public KeyCaptureDialog(MacroEngine engine, bool chooseKind)
        {
            _engine = engine;
            _chooseKind = chooseKind;

            Text = "Taste aufnehmen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(360, 140);

            _label = new Label();
            _label.Text = "Druecke jetzt die gewuenschte Taste ...";
            _label.TextAlign = ContentAlignment.MiddleCenter;
            _label.Dock = DockStyle.Top;
            _label.Height = 70;
            _label.Font = new Font(Font.FontFamily, 11f);
            Controls.Add(_label);

            _kindPanel = new Panel();
            _kindPanel.Dock = DockStyle.Fill;
            _kindPanel.Visible = false;
            Controls.Add(_kindPanel);
            _kindPanel.BringToFront();

            if (chooseKind)
            {
                AddKindButton("Druecken", StepType.KeyPress, 10);
                AddKindButton("Halten", StepType.KeyDown, 125);
                AddKindButton("Loslassen", StepType.KeyUp, 240);
            }

            Load += (s, e) =>
            {
                _engine.RecordedKey += OnRecorded;
                _engine.RecordMode = true;
            };
            FormClosed += (s, e) =>
            {
                _engine.RecordMode = false;
                _engine.RecordedKey -= OnRecorded;
            };
        }

        private void AddKindButton(string text, StepType kind, int x)
        {
            Button b = new Button();
            b.Text = text;
            b.SetBounds(x, 20, 105, 30);
            b.Click += (s, e) =>
            {
                ChosenKind = kind;
                DialogResult = DialogResult.OK;
                Close();
            };
            _kindPanel.Controls.Add(b);
        }

        private void OnRecorded(int vk, bool isDown)
        {
            if (_captured || !isDown)
                return;
            _captured = true;
            CapturedVk = vk;
            _engine.RecordMode = false;

            if (_chooseKind)
            {
                _label.Text = "Taste: " + KeyNames.NameOf(vk) + "\nAktion waehlen:";
                _kindPanel.Visible = true;
            }
            else
            {
                ChosenKind = StepType.KeyPress;
                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }

    /// <summary>
    /// Nimmt eine ganze Tastenfolge inklusive Pausen auf (globaler Hook).
    /// </summary>
    public sealed class SequenceRecorderDialog : Form
    {
        private readonly MacroEngine _engine;
        private readonly ListBox _list;
        private readonly Stopwatch _watch = new Stopwatch();
        private long _lastMs = -1;

        public List<MacroStep> Result { get; private set; }

        public SequenceRecorderDialog(MacroEngine engine)
        {
            _engine = engine;
            Result = new List<MacroStep>();

            Text = "Folge aufnehmen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(380, 320);

            Label info = new Label();
            info.Text = "Tippe jetzt deine Tastenfolge. Pausen werden mit aufgenommen.";
            info.Dock = DockStyle.Top;
            info.Height = 40;
            Controls.Add(info);

            _list = new ListBox();
            _list.Dock = DockStyle.Fill;
            Controls.Add(_list);
            _list.BringToFront();

            Panel bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 44;
            Controls.Add(bottom);
            bottom.BringToFront();

            Button stop = new Button();
            stop.Text = "Aufnahme beenden";
            stop.SetBounds(200, 8, 165, 30);
            stop.Click += (s, e) => { DialogResult = DialogResult.OK; Close(); };
            bottom.Controls.Add(stop);

            Button cancel = new Button();
            cancel.Text = "Abbrechen";
            cancel.SetBounds(20, 8, 165, 30);
            cancel.Click += (s, e) => { Result.Clear(); DialogResult = DialogResult.Cancel; Close(); };
            bottom.Controls.Add(cancel);

            Load += (s, e) =>
            {
                _engine.RecordedKey += OnRecorded;
                _engine.RecordMode = true;
                _watch.Start();
            };
            FormClosed += (s, e) =>
            {
                _engine.RecordMode = false;
                _engine.RecordedKey -= OnRecorded;
            };
        }

        private void OnRecorded(int vk, bool isDown)
        {
            if (!isDown)
                return;

            long now = _watch.ElapsedMilliseconds;
            if (_lastMs >= 0)
            {
                int gap = (int)(now - _lastMs);
                if (gap >= 15)
                {
                    MacroStep delay = new MacroStep { Type = StepType.Delay, DelayMs = gap };
                    Result.Add(delay);
                    _list.Items.Add(delay.ToString());
                }
            }
            _lastMs = now;

            MacroStep step = new MacroStep { Type = StepType.KeyPress, VkCode = vk };
            Result.Add(step);
            _list.Items.Add(step.ToString());
            _list.TopIndex = _list.Items.Count - 1;
        }
    }

    /// <summary>
    /// Nimmt eine Tastenkombination auf: beliebige Modifier (Strg/Alt/Umschalt/Win)
    /// plus eine Haupttaste. Wird fuer den globalen An/Aus-Hotkey verwendet.
    /// </summary>
    public sealed class HotkeyCaptureDialog : Form
    {
        private readonly MacroEngine _engine;
        private readonly HashSet<int> _held = new HashSet<int>();
        private readonly Label _preview;

        public Hotkey Result { get; private set; }

        public HotkeyCaptureDialog(MacroEngine engine)
        {
            _engine = engine;

            Text = "Hotkey aufnehmen";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(400, 160);

            Label info = new Label();
            info.Text = "Halte die gewuenschte Kombination gedrueckt\n(z. B. Strg + Alt + M).";
            info.SetBounds(12, 12, 376, 40);
            info.TextAlign = ContentAlignment.MiddleCenter;
            Controls.Add(info);

            _preview = new Label();
            _preview.Text = "...";
            _preview.SetBounds(12, 56, 376, 40);
            _preview.TextAlign = ContentAlignment.MiddleCenter;
            _preview.Font = new Font(Font.FontFamily, 13f, FontStyle.Bold);
            Controls.Add(_preview);

            Button cancel = new Button();
            cancel.Text = "Abbrechen";
            cancel.SetBounds(150, 112, 100, 30);
            cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(cancel);

            Load += (s, e) =>
            {
                _engine.RecordedKey += OnRecorded;
                _engine.RecordMode = true;
            };
            FormClosed += (s, e) =>
            {
                _engine.RecordMode = false;
                _engine.RecordedKey -= OnRecorded;
            };
        }

        private void OnRecorded(int vk, bool isDown)
        {
            if (isDown)
                _held.Add(vk);
            else
                _held.Remove(vk);

            bool ctrl = false, alt = false, shift = false, win = false;
            foreach (int k in _held)
            {
                if (KeyMods.IsCtrl(k)) ctrl = true;
                else if (KeyMods.IsAlt(k)) alt = true;
                else if (KeyMods.IsShift(k)) shift = true;
                else if (KeyMods.IsWin(k)) win = true;
            }

            // Erst wenn eine Nicht-Modifier-Taste gedrueckt wird, ist die Kombi vollstaendig.
            if (isDown && !KeyMods.IsModifier(vk))
            {
                Result = new Hotkey { Vk = vk, Ctrl = ctrl, Alt = alt, Shift = shift, Win = win };
                _engine.RecordMode = false;
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            // Vorschau der bisher gehaltenen Modifier anzeigen.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            if (ctrl) sb.Append("Strg + ");
            if (alt) sb.Append("Alt + ");
            if (shift) sb.Append("Umschalt + ");
            if (win) sb.Append("Win + ");
            sb.Append("...");
            _preview.Text = sb.ToString();
        }
    }

    /// <summary>Einfacher Text-/Zahlen-Eingabedialog (Ersatz fuer InputBox).</summary>
    public static class InputDialog
    {
        public static string AskText(IWin32Window owner, string title, string prompt, string defaultValue)
        {
            using (Form f = new Form())
            {
                f.Text = title;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.ClientSize = new Size(360, 130);

                Label l = new Label { Text = prompt, Left = 12, Top = 12, Width = 336 };
                TextBox t = new TextBox { Left = 12, Top = 40, Width = 336, Text = defaultValue ?? "" };
                Button ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Left = 172, Top = 80, Width = 80 };
                Button cancel = new Button { Text = "Abbrechen", DialogResult = DialogResult.Cancel, Left = 262, Top = 80, Width = 86 };

                f.Controls.Add(l);
                f.Controls.Add(t);
                f.Controls.Add(ok);
                f.Controls.Add(cancel);
                f.AcceptButton = ok;
                f.CancelButton = cancel;

                return f.ShowDialog(owner) == DialogResult.OK ? t.Text : null;
            }
        }

        public static int? AskNumber(IWin32Window owner, string title, string prompt, int defaultValue)
        {
            string s = AskText(owner, title, prompt, defaultValue.ToString());
            if (s == null)
                return null;
            int value;
            if (int.TryParse(s.Trim(), out value) && value >= 0)
                return value;
            MessageBox.Show("Bitte eine gueltige Zahl (>= 0) eingeben.");
            return null;
        }
    }
}
