using System;
using System.Drawing;
using System.Windows.Forms;

namespace Multikeys
{
    public sealed class MainForm : Form
    {
        private readonly MacroEngine _engine = new MacroEngine();
        private AppConfig _config;
        private bool _loading;

        // Steuerelemente
        private CheckBox _chkEngine;
        private ComboBox _cboMethod;
        private Label _lblHotkey;
        private Button _btnHkCapture;
        private Button _btnHkClear;
        private ListBox _lstMacros;
        private Button _btnAddMacro;
        private Button _btnDelMacro;

        private TextBox _txtName;
        private Label _lblTrigger;
        private Button _btnCaptureTrigger;
        private CheckBox _chkMacroEnabled;
        private CheckBox _chkSuppress;

        private ListBox _lstSteps;
        private Button _btnAddKey;
        private Button _btnAddDelay;
        private Button _btnAddText;
        private Button _btnRecordSeq;
        private Button _btnStepEdit;
        private Button _btnStepUp;
        private Button _btnStepDown;
        private Button _btnStepDel;

        private Label _lblStatus;
        private NotifyIcon _tray;

        public MainForm()
        {
            _config = ConfigStore.Load();

            BuildUi();

            _engine.MacroTriggered += OnMacroTriggered;
            _engine.EngineToggled += OnEngineToggled;
            _engine.UpdateConfig(_config);
            _engine.Start();

            _loading = true;
            _chkEngine.Checked = _config.EngineEnabled;
            _cboMethod.SelectedIndex = (int)_config.SendMethod;
            _loading = false;
            UpdateHotkeyLabel();
            RefreshMacroList();
            LoadEditor(null);
        }

        private Macro Current
        {
            get { return _lstMacros.SelectedItem as Macro; }
        }

        // ---------------------------------------------------------------- UI

        private void BuildUi()
        {
            Text = "Multikeys - Tastatur-Makros";
            ClientSize = new Size(720, 600);
            MinimumSize = new Size(720, 600);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 9f);

            _chkEngine = new CheckBox();
            _chkEngine.Text = "Makros aktiv";
            _chkEngine.SetBounds(12, 11, 128, 24);
            _chkEngine.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
            _chkEngine.CheckedChanged += (s, e) =>
            {
                if (_loading) return;
                _config.EngineEnabled = _chkEngine.Checked;
                Save();
            };
            Controls.Add(_chkEngine);

            Label lblMethod = new Label { Text = "Ausgabe:", Left = 148, Top = 14, Width = 58 };
            Controls.Add(lblMethod);

            _cboMethod = new ComboBox { Left = 206, Top = 10, Width = 244, DropDownStyle = ComboBoxStyle.DropDownList };
            _cboMethod.Items.Add("Scancode (Standard, gut fuer Spiele)");
            _cboMethod.Items.Add("Virtueller Tastencode (fuer Programme)");
            _cboMethod.SelectedIndexChanged += (s, e) =>
            {
                if (_loading) return;
                _config.SendMethod = (KeySendMethod)_cboMethod.SelectedIndex;
                Save();
            };
            Controls.Add(_cboMethod);

            Label hint = new Label();
            hint.Text = "Erkennt ein Programm die Tasten nicht? Andere Ausgabe waehlen.";
            hint.SetBounds(458, 14, 254, 32);
            hint.ForeColor = Color.DimGray;
            Controls.Add(hint);

            // ---- Zeile: globaler An/Aus-Hotkey
            Label lblHk = new Label { Text = "An/Aus-Hotkey:", Left = 12, Top = 46, Width = 95 };
            Controls.Add(lblHk);

            _lblHotkey = new Label
            {
                Left = 110,
                Top = 43,
                Width = 170,
                Height = 24,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "(keiner)"
            };
            Controls.Add(_lblHotkey);

            _btnHkCapture = new Button { Text = "Aufnehmen", Left = 288, Top = 42, Width = 100 };
            _btnHkCapture.Click += (s, e) => CaptureHotkey();
            Controls.Add(_btnHkCapture);

            _btnHkClear = new Button { Text = "Entfernen", Left = 392, Top = 42, Width = 95 };
            _btnHkClear.Click += (s, e) => ClearHotkey();
            Controls.Add(_btnHkClear);

            Label hkHint = new Label
            {
                Text = "schaltet alle Makros um",
                Left = 495,
                Top = 46,
                Width = 210,
                ForeColor = Color.DimGray
            };
            Controls.Add(hkHint);

            // ---- Linke Spalte: Liste der Makros
            Label lblMacros = new Label { Text = "Makros:", Left = 12, Top = 78, Width = 200 };
            Controls.Add(lblMacros);

            _lstMacros = new ListBox();
            _lstMacros.SetBounds(12, 100, 240, 422);
            _lstMacros.SelectedIndexChanged += (s, e) => { if (!_loading) LoadEditor(Current); };
            Controls.Add(_lstMacros);

            _btnAddMacro = new Button { Text = "Neu", Left = 12, Top = 528, Width = 115 };
            _btnAddMacro.Click += (s, e) => AddMacro();
            Controls.Add(_btnAddMacro);

            _btnDelMacro = new Button { Text = "Loeschen", Left = 137, Top = 528, Width = 115 };
            _btnDelMacro.Click += (s, e) => DeleteMacro();
            Controls.Add(_btnDelMacro);

            // ---- Rechte Spalte: Editor
            GroupBox box = new GroupBox { Text = "Makro bearbeiten", Left = 268, Top = 78, Width = 440, Height = 444 };
            Controls.Add(box);

            Label lblName = new Label { Text = "Name:", Left = 14, Top = 28, Width = 90 };
            box.Controls.Add(lblName);
            _txtName = new TextBox { Left = 110, Top = 25, Width = 310 };
            _txtName.TextChanged += (s, e) =>
            {
                if (_loading || Current == null) return;
                Current.Name = _txtName.Text;
                int idx = _lstMacros.SelectedIndex;
                if (idx >= 0)
                {
                    // Listenanzeige aktualisieren, ohne den Editor neu zu laden.
                    // Ohne diesen Schutz wuerde die Auswahl kurz auf -1 springen,
                    // LoadEditor(null) das Textfeld deaktivieren und der Fokus ginge
                    // nach jedem Buchstaben verloren.
                    _loading = true;
                    _lstMacros.Items[idx] = Current;
                    _lstMacros.SelectedIndex = idx;
                    _loading = false;
                }
                Save();
            };
            box.Controls.Add(_txtName);

            Label lblTrig = new Label { Text = "Trigger-Taste:", Left = 14, Top = 62, Width = 90 };
            box.Controls.Add(lblTrig);
            _lblTrigger = new Label
            {
                Left = 110,
                Top = 60,
                Width = 200,
                Height = 24,
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "(keine)"
            };
            box.Controls.Add(_lblTrigger);
            _btnCaptureTrigger = new Button { Text = "Aufnehmen", Left = 318, Top = 59, Width = 102 };
            _btnCaptureTrigger.Click += (s, e) => CaptureTrigger();
            box.Controls.Add(_btnCaptureTrigger);

            _chkMacroEnabled = new CheckBox { Text = "Dieses Makro aktiv", Left = 110, Top = 92, Width = 160 };
            _chkMacroEnabled.CheckedChanged += (s, e) =>
            {
                if (_loading || Current == null) return;
                Current.Enabled = _chkMacroEnabled.Checked;
                Save();
            };
            box.Controls.Add(_chkMacroEnabled);

            _chkSuppress = new CheckBox { Text = "Trigger-Taste unterdruecken", Left = 270, Top = 92, Width = 200 };
            _chkSuppress.CheckedChanged += (s, e) =>
            {
                if (_loading || Current == null) return;
                Current.SuppressTrigger = _chkSuppress.Checked;
                Save();
            };
            box.Controls.Add(_chkSuppress);

            Label lblSteps = new Label { Text = "Schritte (von oben nach unten; Doppelklick zum Aendern):", Left = 14, Top = 122, Width = 410 };
            box.Controls.Add(lblSteps);

            _lstSteps = new ListBox { Left = 14, Top = 144, Width = 280, Height = 280 };
            _lstSteps.Font = new Font("Consolas", 9f);
            _lstSteps.DoubleClick += (s, e) => EditStep();
            box.Controls.Add(_lstSteps);

            int bx = 302, bw = 122, by = 144;
            _btnAddKey = MakeStepButton(box, "Taste ...", bx, ref by, bw);
            _btnAddKey.Click += (s, e) => AddKeyStep();
            _btnAddDelay = MakeStepButton(box, "Pause ...", bx, ref by, bw);
            _btnAddDelay.Click += (s, e) => AddDelayStep();
            _btnAddText = MakeStepButton(box, "Text ...", bx, ref by, bw);
            _btnAddText.Click += (s, e) => AddTextStep();
            _btnRecordSeq = MakeStepButton(box, "Folge aufnehmen", bx, ref by, bw);
            _btnRecordSeq.Click += (s, e) => RecordSequence();
            by += 8;
            _btnStepEdit = MakeStepButton(box, "Aendern ...", bx, ref by, bw);
            _btnStepEdit.Click += (s, e) => EditStep();
            _btnStepUp = MakeStepButton(box, "Nach oben", bx, ref by, bw);
            _btnStepUp.Click += (s, e) => MoveStep(-1);
            _btnStepDown = MakeStepButton(box, "Nach unten", bx, ref by, bw);
            _btnStepDown.Click += (s, e) => MoveStep(+1);
            _btnStepDel = MakeStepButton(box, "Entfernen", bx, ref by, bw);
            _btnStepDel.Click += (s, e) => RemoveStep();

            _lblStatus = new Label { Left = 268, Top = 528, Width = 440, Height = 40, ForeColor = Color.SteelBlue };
            _lblStatus.Text = "Konfiguration: " + ConfigStore.ConfigPath;
            Controls.Add(_lblStatus);

            // ---- Tray
            _tray = new NotifyIcon();
            _tray.Icon = SystemIcons.Application;
            _tray.Text = "Multikeys";
            _tray.Visible = true;
            _tray.DoubleClick += (s, e) => ShowFromTray();
            ContextMenuStrip menu = new ContextMenuStrip();
            menu.Items.Add("Anzeigen", null, (s, e) => ShowFromTray());
            menu.Items.Add("Beenden", null, (s, e) => ExitApp());
            _tray.ContextMenuStrip = menu;

            Resize += (s, e) =>
            {
                if (WindowState == FormWindowState.Minimized)
                {
                    Hide();
                    _tray.ShowBalloonTip(1500, "Multikeys", "Laeuft weiter im Infobereich.", ToolTipIcon.Info);
                }
            };
            FormClosing += (s, e) => ExitApp();
        }

        private Button MakeStepButton(Control parent, string text, int x, ref int y, int w)
        {
            Button b = new Button { Text = text, Left = x, Top = y, Width = w, Height = 30 };
            parent.Controls.Add(b);
            y += 34;
            return b;
        }

        // ------------------------------------------------------------- Logik

        private void RefreshMacroList()
        {
            _loading = true;
            int sel = _lstMacros.SelectedIndex;
            _lstMacros.Items.Clear();
            foreach (Macro m in _config.Macros)
                _lstMacros.Items.Add(m);
            _lstMacros.DisplayMember = "Name";
            if (sel >= 0 && sel < _lstMacros.Items.Count)
                _lstMacros.SelectedIndex = sel;
            _loading = false;
        }

        private void LoadEditor(Macro m)
        {
            _loading = true;
            bool has = m != null;
            _txtName.Enabled = has;
            _lblTrigger.Enabled = has;
            _btnCaptureTrigger.Enabled = has;
            _chkMacroEnabled.Enabled = has;
            _chkSuppress.Enabled = has;
            _lstSteps.Enabled = has;
            _btnAddKey.Enabled = has;
            _btnAddDelay.Enabled = has;
            _btnAddText.Enabled = has;
            _btnRecordSeq.Enabled = has;
            _btnStepEdit.Enabled = has;
            _btnStepUp.Enabled = has;
            _btnStepDown.Enabled = has;
            _btnStepDel.Enabled = has;

            if (has)
            {
                _txtName.Text = m.Name;
                _lblTrigger.Text = m.TriggerVkCode == 0 ? "(keine)" : KeyNames.NameOf(m.TriggerVkCode);
                _chkMacroEnabled.Checked = m.Enabled;
                _chkSuppress.Checked = m.SuppressTrigger;
                RefreshSteps();
            }
            else
            {
                _txtName.Text = "";
                _lblTrigger.Text = "(keine)";
                _chkMacroEnabled.Checked = false;
                _chkSuppress.Checked = false;
                _lstSteps.Items.Clear();
            }
            _loading = false;
        }

        private void RefreshSteps()
        {
            Macro m = Current;
            int sel = _lstSteps.SelectedIndex;
            _lstSteps.Items.Clear();
            if (m != null)
            {
                foreach (MacroStep step in m.Steps)
                    _lstSteps.Items.Add(step.ToString());
            }
            if (sel >= 0 && sel < _lstSteps.Items.Count)
                _lstSteps.SelectedIndex = sel;
        }

        private void AddMacro()
        {
            Macro m = new Macro();
            _config.Macros.Add(m);
            RefreshMacroList();
            _lstMacros.SelectedIndex = _config.Macros.Count - 1;
            Save();
            _txtName.Focus();
            _txtName.SelectAll();
        }

        private void DeleteMacro()
        {
            Macro m = Current;
            if (m == null) return;
            if (MessageBox.Show("Makro \"" + m.Name + "\" wirklich loeschen?", "Multikeys",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;
            _config.Macros.Remove(m);
            RefreshMacroList();
            LoadEditor(Current);
            Save();
        }

        private void UpdateHotkeyLabel()
        {
            _lblHotkey.Text = _config.ToggleHotkey != null ? _config.ToggleHotkey.ToString() : "(keiner)";
        }

        private void CaptureHotkey()
        {
            using (HotkeyCaptureDialog dlg = new HotkeyCaptureDialog(_engine))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result != null)
                {
                    _config.ToggleHotkey = dlg.Result;
                    UpdateHotkeyLabel();
                    Save();
                }
            }
        }

        private void ClearHotkey()
        {
            _config.ToggleHotkey = new Hotkey();
            UpdateHotkeyLabel();
            Save();
        }

        private void OnEngineToggled(bool newState)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    _loading = true;
                    _chkEngine.Checked = newState;
                    _loading = false;
                    _lblStatus.Text = (newState ? "Makros EINGESCHALTET" : "Makros AUSGESCHALTET")
                        + "   (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                    try { ConfigStore.Save(_config); } catch { }
                    if (_tray != null)
                        _tray.ShowBalloonTip(1200, "Multikeys",
                            newState ? "Makros eingeschaltet" : "Makros ausgeschaltet",
                            ToolTipIcon.Info);
                }));
            }
            catch { }
        }

        private void CaptureTrigger()
        {
            Macro m = Current;
            if (m == null) return;
            using (KeyCaptureDialog dlg = new KeyCaptureDialog(_engine, false))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    m.TriggerVkCode = dlg.CapturedVk;
                    _lblTrigger.Text = KeyNames.NameOf(m.TriggerVkCode);
                    Save();
                }
            }
        }

        private void AddKeyStep()
        {
            Macro m = Current;
            if (m == null) return;
            using (KeyCaptureDialog dlg = new KeyCaptureDialog(_engine, true))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    MacroStep step = new MacroStep { Type = dlg.ChosenKind, VkCode = dlg.CapturedVk };
                    InsertStep(step);
                }
            }
        }

        private void AddDelayStep()
        {
            Macro m = Current;
            if (m == null) return;
            int? ms = InputDialog.AskNumber(this, "Pause", "Pause in Millisekunden:", 100);
            if (ms.HasValue)
                InsertStep(new MacroStep { Type = StepType.Delay, DelayMs = ms.Value });
        }

        private void AddTextStep()
        {
            Macro m = Current;
            if (m == null) return;
            string text = InputDialog.AskText(this, "Text", "Zu tippender Text:", "");
            if (text != null)
                InsertStep(new MacroStep { Type = StepType.Text, Text = text });
        }

        private void RecordSequence()
        {
            Macro m = Current;
            if (m == null) return;
            using (SequenceRecorderDialog dlg = new SequenceRecorderDialog(_engine))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK && dlg.Result.Count > 0)
                {
                    int at = _lstSteps.SelectedIndex;
                    if (at < 0) at = m.Steps.Count - 1;
                    foreach (MacroStep step in dlg.Result)
                    {
                        at++;
                        m.Steps.Insert(at, step);
                    }
                    RefreshSteps();
                    if (at < _lstSteps.Items.Count) _lstSteps.SelectedIndex = at;
                    Save();
                }
            }
        }

        private void InsertStep(MacroStep step)
        {
            Macro m = Current;
            if (m == null) return;
            int at = _lstSteps.SelectedIndex;
            if (at < 0) at = m.Steps.Count - 1;
            at++;
            m.Steps.Insert(at, step);
            RefreshSteps();
            _lstSteps.SelectedIndex = at;
            Save();
        }

        private void EditStep()
        {
            Macro m = Current;
            if (m == null) return;
            int i = _lstSteps.SelectedIndex;
            if (i < 0 || i >= m.Steps.Count) return;
            MacroStep step = m.Steps[i];

            switch (step.Type)
            {
                case StepType.Delay:
                    int? ms = InputDialog.AskNumber(this, "Pause aendern", "Pause in Millisekunden:", step.DelayMs);
                    if (!ms.HasValue) return;
                    step.DelayMs = ms.Value;
                    break;

                case StepType.Text:
                    string text = InputDialog.AskText(this, "Text aendern", "Zu tippender Text:", step.Text);
                    if (text == null) return;
                    step.Text = text;
                    break;

                default: // KeyPress / KeyDown / KeyUp -> Taste und Aktion neu waehlen
                    using (KeyCaptureDialog dlg = new KeyCaptureDialog(_engine, true))
                    {
                        if (dlg.ShowDialog(this) != DialogResult.OK) return;
                        step.Type = dlg.ChosenKind;
                        step.VkCode = dlg.CapturedVk;
                    }
                    break;
            }

            RefreshSteps();
            _lstSteps.SelectedIndex = i;
            Save();
        }

        private void MoveStep(int dir)
        {
            Macro m = Current;
            if (m == null) return;
            int i = _lstSteps.SelectedIndex;
            int j = i + dir;
            if (i < 0 || j < 0 || j >= m.Steps.Count) return;
            MacroStep tmp = m.Steps[i];
            m.Steps[i] = m.Steps[j];
            m.Steps[j] = tmp;
            RefreshSteps();
            _lstSteps.SelectedIndex = j;
            Save();
        }

        private void RemoveStep()
        {
            Macro m = Current;
            if (m == null) return;
            int i = _lstSteps.SelectedIndex;
            if (i < 0) return;
            m.Steps.RemoveAt(i);
            RefreshSteps();
            if (i < _lstSteps.Items.Count) _lstSteps.SelectedIndex = i;
            else if (_lstSteps.Items.Count > 0) _lstSteps.SelectedIndex = _lstSteps.Items.Count - 1;
            Save();
        }

        private void OnMacroTriggered(string name)
        {
            if (IsDisposed) return;
            try
            {
                BeginInvoke((Action)(() =>
                {
                    _lblStatus.Text = "Ausgeloest: " + name + "   (" + DateTime.Now.ToString("HH:mm:ss") + ")";
                }));
            }
            catch { }
        }

        private void Save()
        {
            _engine.UpdateConfig(_config);
            try
            {
                ConfigStore.Save(_config);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "Speichern fehlgeschlagen: " + ex.Message;
            }
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private bool _exiting;
        private void ExitApp()
        {
            if (_exiting) return;
            _exiting = true;
            try { Save(); } catch { }
            try { _engine.Stop(); _engine.Dispose(); } catch { }
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            Application.Exit();
        }
    }
}
