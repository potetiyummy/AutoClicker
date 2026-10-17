using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Windows.Forms;

// gui/gui_base.py・gui/tabs/*.py・gui/gui_event_handlers.py・gui/gui_io.py の移植
sealed class MainForm : Form
{
    const string Version = "1.10.2";

    static readonly string[] TabKeys = { "click", "drag", "hold", "keyboard", "wait" };
    static readonly string[] ClickButtons = { "left_click", "right_click" };
    static readonly string[] DragButtons = { "left_drag", "right_drag" };
    static readonly string[] HoldButtons = { "left_hold", "right_hold" };
    static readonly string[] KeyboardModes = { "text", "hotkey" };
    static readonly string[] DragTargets = { "start", "end", "via" };

    static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    readonly MacroEngine _engine = new MacroEngine();
    readonly Dictionary<string, Image> _images;
    readonly List<int[]> _currentWaypoints = new List<int[]>();
    readonly List<int[]> _holdWaypoints = new List<int[]>();
    int[] _dragStart = { 0, 0 };
    int[] _dragEnd = { 100, 100 };
    bool _keepAwakeOn;

    IntPtr _hook = IntPtr.Zero;
    readonly Native.HookProc _hookProc;

    // 画面の部品（BuildUi で作り直す）
    Dictionary<string, Panel> _pages = new Dictionary<string, Panel>();
    ComboBox _langBox;
    SegmentedButton _tabs;
    Button _runBtn;
    Label _statusLabel;
    MacroListPanel _listPanel;
    CheckBox _keepAwakeBox;
    TextBox _wayBox;
    WaypointListPanel _holdList;
    Label _lblStart, _lblEnd;

    SegmentedButton _cBtn, _dBtn, _dTarget, _hBtn, _kMode;
    TextBox _cX, _cY, _dX, _dY, _hX, _hY, _hDur, _hwX, _hwY, _kVal, _wDur;
    Button _cPick, _dPick, _hPick, _hwPick;

    public MainForm()
    {
        Text = $"Auto Clicker v{Version}";
        ClientSize = new Size(820, 630);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        HandleCreated += (s, e) => Native.SetDarkTitleBar(Handle, Theme.IsDark);
        BackColor = Theme.Window;
        if (IsHandleCreated) Native.SetDarkTitleBar(Handle, Theme.IsDark);
        Font = Theme.Ui(9);
        Icon = LoadIcon();

        _images = LoadImages();
        _engine.StateChanged += UpdateStatus;

        BuildUi();

        _hookProc = OnKeyboardHook;
        _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _hookProc, Native.GetModuleHandle(null), 0);

        Shown += (s, e) => PromptUnsupportedAtStart();
        FormClosing += (s, e) => Shutdown();
    }

    // --- 画面の組み立て ---

    void BuildUi()
    {
        var old = Controls.Cast<Control>().ToArray();
        Controls.Clear();
        foreach (var c in old) c.Dispose();

        SuspendLayout();
        BackColor = Theme.Window;
        if (IsHandleCreated) Native.SetDarkTitleBar(Handle, Theme.IsDark);

        // 右上：テーマ切替（ライト／ダーク）。選択は config.py に保存する
        var themeBtn = Ui.Button(Strings.T(Theme.IsDark ? "to_light_mode" : "to_dark_mode"), 10, false, null, 130, 28);
        themeBtn.Location = new Point(500, 10);
        themeBtn.Click += (s, e) => BeginInvoke(new Action(() =>
        {
            Strings.SaveTheme(!Theme.IsDark);
            BuildUi();
            RefreshList();
        }));
        Controls.Add(themeBtn);

        // 右上：言語（ドロップダウン）
        var options = Strings.LanguageOptions();
        _langBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(10),
            Width = 160,
            Location = new Point(640, 10),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22,
        };
        // ダーク／ライトに合わせて項目を自前で描く（開いた一覧も同じ配色にする）
        _langBox.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(selected ? Theme.Track : Theme.Entry))
                e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, (string)_langBox.Items[e.Index], Theme.Ui(10),
                new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height), Theme.Text,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.SingleLine);
        };
        foreach (var o in options) _langBox.Items.Add(o.Label);
        SyncLanguageChecks();
        _langBox.SelectedIndexChanged += (s, e) =>
        {
            string label = (string)_langBox.SelectedItem;
            string code = options.First(o => o.Label == label).Code;
            BeginInvoke(new Action(() => OnLanguageChanged(code)));
        };
        Controls.Add(_langBox);

        BuildLeftColumn();
        BuildRightColumn();

        ResumeLayout(false);
        PerformLayout();

        UpdateWaypointDisplay();
        UpdateStatus(_engine.Active);
        RefreshList();
        SetKeepAwake(_keepAwakeOn);
    }

    void Place(Control c, int x, int y)
    {
        c.Location = new Point(x, y);
        Controls.Add(c);
    }

    void BuildLeftColumn()
    {
        Place(Ui.Label(Strings.T("panel_title"), 20, true), 20, 58);

        _tabs = new SegmentedButton(TabKeys, "click", 13, 380) { Location = new Point(20, 98) };
        _tabs.SelectionChanged += (s, e) => ShowPage(_tabs.Selected);
        Controls.Add(_tabs);

        foreach (var key in TabKeys)
        {
            var page = new Panel { Location = new Point(20, 130), Size = new Size(380, 308), BackColor = Theme.Frame, Visible = key == "click" };
            Shape.Round(page, 12);
            _pages[key] = page;
            Controls.Add(page);
        }
        BuildClickPage(_pages["click"]);
        BuildDragPage(_pages["drag"]);
        BuildHoldPage(_pages["hold"]);
        BuildKeyboardPage(_pages["keyboard"]);
        BuildWaitPage(_pages["wait"]);

        _runBtn = Ui.Button(Strings.T("run"), 14, true, null, 380, 40);
        _runBtn.Click += (s, e) => _engine.Toggle(this);
        Place(_runBtn, 20, 446);

        var saveBtn = Ui.Button(Strings.T("save_acf"), 12, true, null, 185, 28);
        saveBtn.Click += (s, e) => SaveAcf();
        Place(saveBtn, 20, 496);

        var loadBtn = Ui.Button(Strings.T("load_acf"), 12, true, null, 185, 28);
        loadBtn.Click += (s, e) => LoadAcf();
        Place(loadBtn, 215, 496);
    }

    void BuildRightColumn()
    {
        Place(Ui.Label(Strings.T("list_title"), 16, true), 420, 58);

        _listPanel = new MacroListPanel(_engine, _images) { Location = new Point(420, 86), Size = new Size(380, 428) };
        Controls.Add(_listPanel);

        _statusLabel = Ui.Label(Strings.T("status_stopped"), 14, true);
        Place(_statusLabel, 420, 525);

        var clearBtn = Ui.Button(Strings.T("clear"), 12, true, Theme.ClearBtn, 70, 28);
        clearBtn.Click += (s, e) => ClearList();
        Place(clearBtn, 730, 521);

        _keepAwakeBox = new CheckBox
        {
            Text = Strings.T("keep_awake"),
            Font = Theme.Ui(12),
            ForeColor = _keepAwakeOn ? Theme.Green : Theme.Text,
            AutoSize = true,
            Checked = _keepAwakeOn,
        };
        _keepAwakeBox.CheckedChanged += (s, e) =>
        {
            _keepAwakeOn = _keepAwakeBox.Checked;
            SetKeepAwake(_keepAwakeOn);
        };
        Place(_keepAwakeBox, 420, 562);
    }

    void ShowPage(string key)
    {
        foreach (var kv in _pages) kv.Value.Visible = kv.Key == key;
    }

    void BuildClickPage(Panel p)
    {
        _cBtn = new SegmentedButton(ClickButtons, "left_click", 12, 160);
        _cX = Ui.Entry(13, 60); _cX.Text = "0";
        _cY = Ui.Entry(13, 60); _cY.Text = "0";
        _cPick = Ui.Button(Strings.T("pick"), 12, false, Theme.PickBtn, 150, 28);
        _cPick.Click += (s, e) => StartPick("click");
        var add = Ui.Button(Strings.T("add_click"), 13, true, null, 170, 28);
        add.Click += (s, e) => AddClick();

        int y = Ui.Row(p, 10, 10, _cBtn);
        y = Ui.Row(p, y, 10, _cX, _cY);
        y = Ui.Row(p, y, 10, _cPick);
        Ui.Single(p, y + 10, 0, add);
    }

    void BuildDragPage(Panel p)
    {
        _dBtn = new SegmentedButton(DragButtons, "left_drag", 12, 160);
        _dTarget = new SegmentedButton(DragTargets, "start", 11, 216);
        _dPick = Ui.Button(Strings.T("pick_short"), 11, false, Theme.PickBtn, 84, 28);
        _dPick.Click += (s, e) => StartPick("drag");

        _dX = Ui.Entry(12, 60, "X"); _dX.Text = "0";
        _dY = Ui.Entry(12, 60, "Y"); _dY.Text = "0";

        _lblStart = Ui.Label(Strings.T("start_label", ("x", _dragStart[0]), ("y", _dragStart[1])), 11, false, Theme.Gray);
        _lblEnd = Ui.Label(Strings.T("end_label", ("x", _dragEnd[0]), ("y", _dragEnd[1])), 11, false, Theme.Gray);

        var addWay = Ui.Button(Strings.T("add_waypoint"), 11, true, null, 0, 28);
        addWay.Click += (s, e) => AddDragWaypoint();
        var clearWay = Ui.Button(Strings.T("clear_waypoints"), 11, false, Theme.ClearBtn, 70, 28);
        clearWay.Click += (s, e) => { _currentWaypoints.Clear(); UpdateWaypointDisplay(); };

        _wayBox = new TextBox
        {
            Multiline = true,
            ReadOnly = true,
            BackColor = Theme.Dark,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.None,
            Font = Theme.Ui(11),
            ScrollBars = ScrollBars.Vertical,
            Size = new Size(350, 50),
        };
        Shape.Round(_wayBox, 10);

        var add = Ui.Button(Strings.T("add_drag"), 13, true, null, 200, 28);
        add.Click += (s, e) => AddDrag();

        int y = Ui.Row(p, 6, 4, _dBtn);
        y = Ui.Row(p, y, 4, _dTarget, _dPick);
        y = Ui.Row(p, y, 4, _dX, _dY);
        y = Ui.Row(p, y, 4, _lblStart, _lblEnd);
        y = Ui.Row(p, y, 4, addWay, clearWay);
        y = Ui.Row(p, y, 4, _wayBox);
        Ui.Single(p, y + 4, 0, add);
    }

    void BuildHoldPage(Panel p)
    {
        _hBtn = new SegmentedButton(HoldButtons, "left_hold", 12, 160);
        _hX = Ui.Entry(12, 50, "X");
        _hY = Ui.Entry(12, 50, "Y");
        _hDur = Ui.Entry(12, 80, Strings.T("ms"));
        _hPick = Ui.Button(Strings.T("pick"), 12, false, Theme.PickBtn, 150, 28);
        _hPick.Click += (s, e) => StartPick("hold");

        _hwX = Ui.Entry(12, 60, Strings.T("via_x"));
        _hwY = Ui.Entry(12, 60, "Y");
        _hwPick = Ui.Button(Strings.T("pick_short"), 11, false, Theme.PickBtn, 84, 28);
        _hwPick.Click += (s, e) => StartPick("hold_via");

        var addWay = Ui.Button(Strings.T("add_waypoint"), 11, true, null, 0, 28);
        addWay.Click += (s, e) => AddHoldWaypoint();
        var clearWay = Ui.Button(Strings.T("clear_waypoints"), 11, false, Theme.ClearBtn, 70, 28);
        clearWay.Click += (s, e) => { _holdWaypoints.Clear(); _holdList.Redraw(); };

        var listTitle = Ui.Label(Strings.T("hold_list_title"), 11, false, Theme.Gray);
        _holdList = new WaypointListPanel(_holdWaypoints, 72);

        var add = Ui.Button(Strings.T("add_hold"), 13, true, null, 200, 28);
        add.Click += (s, e) => AddHold();

        int y = Ui.Row(p, 4, 2, _hBtn);
        y = Ui.Row(p, y, 2, _hX, _hY, _hDur);
        y = Ui.Row(p, y, 2, _hPick);
        y = Ui.Row(p, y, 2, _hwX, _hwY, _hwPick);
        y = Ui.Row(p, y, 2, addWay, clearWay);
        y = Ui.Row(p, y, 2, listTitle);
        y = Ui.Row(p, y, 2, _holdList);
        Ui.Single(p, y + 2, 0, add);
    }

    void BuildKeyboardPage(Panel p)
    {
        _kMode = new SegmentedButton(KeyboardModes, "text", 12, 160);
        _kMode.SelectionChanged += (s, e) => OnKeyboardModeChanged();

        _kVal = Ui.Entry(13, 200);
        _kVal.Text = "Hello";
        _kVal.TextChanged += (s, e) => ValidateKeyboardInput();

        var guide = Ui.Label(Strings.T("kb_guide"), 10, false, Theme.Guide);
        guide.TextAlign = ContentAlignment.MiddleCenter;

        var add = Ui.Button(Strings.T("add_keyboard"), 13, true, null, 200, 28);
        add.Click += (s, e) => AddKeyboard();

        int y = Ui.Row(p, 10, 6, _kMode);
        y = Ui.Row(p, y, 4, _kVal);
        y = Ui.Row(p, y, 4, guide);
        Ui.Single(p, y + 6, 0, add);
        OnKeyboardModeChanged();
    }

    void BuildWaitPage(Panel p)
    {
        var label = Ui.Label(Strings.T("wait_time"), 13);
        _wDur = Ui.Entry(13, 100, Strings.T("ms"));
        _wDur.Text = "500";
        var add = Ui.Button(Strings.T("add_wait"), 13, true, null, 200, 28);
        add.Click += (s, e) => AddWait();

        int y = Ui.Single(p, 20, 6, label);
        y = Ui.Single(p, y, 10, _wDur);
        Ui.Single(p, y + 4, 0, add);
    }


    // --- 動作 ---

    static bool TryInt(string text, int fallback, out int value)
    {
        string s = (text ?? "").Trim();
        if (s.Length == 0) { value = fallback; return true; }
        return int.TryParse(s, out value);
    }

    static int[] Copy(int[] p) => new[] { p[0], p[1] };

    void AddClick()
    {
        if (!TryInt(_cX.Text, 0, out int x) || !TryInt(_cY.Text, 0, out int y)) return;
        AddAction(new MacroAction { Type = "click", Btn = _cBtn.Selected, X = x, Y = y });
    }

    void AddDrag()
    {
        AddAction(new MacroAction
        {
            Type = "drag",
            Btn = _dBtn.Selected,
            Sx = _dragStart[0], Sy = _dragStart[1],
            Ex = _dragEnd[0], Ey = _dragEnd[1],
            Waypoints = _currentWaypoints.Select(Copy).ToList(),
        });
    }

    void AddHold()
    {
        if (!TryInt(_hX.Text, 0, out int x) || !TryInt(_hY.Text, 0, out int y)) return;
        if (!TryInt(_hDur.Text, 1000, out int duration)) return;
        AddAction(new MacroAction
        {
            Type = "hold",
            Btn = _hBtn.Selected,
            X = x, Y = y,
            Duration = duration,
            Waypoints = _holdWaypoints.Select(Copy).ToList(),
        });
    }

    void AddKeyboard()
    {
        AddAction(new MacroAction { Type = "keyboard", Mode = _kMode.Selected, Value = _kVal.Text });
    }

    void AddWait()
    {
        if (!TryInt(_wDur.Text, 500, out int duration)) return;
        AddAction(new MacroAction { Type = "wait", Duration = duration });
    }

    void AddDragWaypoint()
    {
        if (!TryInt(_dX.Text, 0, out int x) || !TryInt(_dY.Text, 0, out int y)) return;
        _currentWaypoints.Add(new[] { x, y });
        UpdateWaypointDisplay();
    }

    void AddHoldWaypoint()
    {
        if (!TryInt(_hwX.Text, 0, out int x) || !TryInt(_hwY.Text, 0, out int y)) return;
        _holdWaypoints.Add(new[] { x, y });
        _holdList.Redraw();
    }

    void AddAction(MacroAction action)
    {
        lock (_engine.Sync) _engine.Actions.Add(action);
        RefreshList();
    }

    void ClearList()
    {
        int count;
        lock (_engine.Sync) count = _engine.Actions.Count;

        if (count == 0)
        {
            Dialogs.Show(this, Strings.T("no_actions"), Strings.T("notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (Dialogs.Show(this, Strings.T("clear_confirm"), Strings.T("warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        lock (_engine.Sync) _engine.Actions.Clear();
        RefreshList();
    }

    void RefreshList() => _listPanel?.Invalidate();

    void UpdateWaypointDisplay()
    {
        if (_wayBox == null) return;
        if (_currentWaypoints.Count == 0)
        {
            _wayBox.Text = Strings.T("no_waypoints");
            return;
        }
        var path = _currentWaypoints.Select(wp => $"({wp[0]}, {wp[1]})");
        _wayBox.Text = Strings.T("waypoint_order", ("path", string.Join(" -> ", path)));
    }

    void OnKeyboardModeChanged()
    {
        if (_kMode.Selected == "text")
        {
            Ui.Cue(_kVal, "Hello");
            ValidateKeyboardInput();
        }
        else
        {
            Ui.Cue(_kVal, "{ctrl+c}");
        }
    }

    // 入力モードでは { } を使えないので、入力時に取り除く
    void ValidateKeyboardInput()
    {
        if (_kMode == null || _kMode.Selected != "text") return;
        string cleaned = _kVal.Text.Replace("{", "").Replace("}", "");
        if (cleaned != _kVal.Text) _kVal.Text = cleaned;
    }

    // 座標取得：3秒カウントダウンの後にマウス位置を取得する
    (Button Button, string IdleKey) PickTarget(string tab)
    {
        switch (tab)
        {
            case "click": return (_cPick, "pick");
            case "drag": return (_dPick, "pick_short");
            case "hold": return (_hPick, "pick");
            default: return (_hwPick, "pick_short");
        }
    }

    void StartPick(string tab)
    {
        var (btn, idleKey) = PickTarget(tab);
        btn.Text = Strings.T("countdown", ("count", 3));
        btn.BackColor = Theme.Countdown;

        int count = 2;
        var timer = new Timer { Interval = 1000 };
        timer.Tick += (s, e) =>
        {
            if (btn.IsDisposed)
            {
                timer.Stop();
                timer.Dispose();
                return;
            }
            if (count > 0)
            {
                btn.Text = Strings.T("countdown", ("count", count));
                count--;
                return;
            }
            timer.Stop();
            timer.Dispose();
            btn.Text = Strings.T(idleKey);
            btn.BackColor = Theme.PickBtn;
            var pos = System.Windows.Forms.Cursor.Position;
            ApplyPickedPosition(tab, pos.X, pos.Y);
        };
        timer.Start();
    }

    void ApplyPickedPosition(string tab, int x, int y)
    {
        switch (tab)
        {
            case "click":
                SetText(_cX, x); SetText(_cY, y);
                break;
            case "hold":
                SetText(_hX, x); SetText(_hY, y);
                break;
            case "hold_via":
                SetText(_hwX, x); SetText(_hwY, y);
                break;
            case "drag":
                SetText(_dX, x); SetText(_dY, y);
                string mode = _dTarget.Selected;
                if (mode == "start")
                {
                    _dragStart = new[] { x, y };
                    _lblStart.Text = Strings.T("start_label", ("x", x), ("y", y));
                }
                else if (mode == "end")
                {
                    _dragEnd = new[] { x, y };
                    _lblEnd.Text = Strings.T("end_label", ("x", x), ("y", y));
                }
                break;
        }
    }

    static void SetText(TextBox tb, int value) => tb.Text = value.ToString();

    // 実行状態に合わせてステータスと実行ボタンの文言を切り替える
    void UpdateStatus(bool active)
    {
        if (_statusLabel == null || _runBtn == null) return;
        _statusLabel.Text = Strings.T(active ? "status_running" : "status_stopped");
        _runBtn.Text = Strings.T(active ? "stop" : "run");
    }

    void SetKeepAwake(bool on)
    {
        Native.SetThreadExecutionState(on
            ? Native.ES_CONTINUOUS | Native.ES_SYSTEM_REQUIRED | Native.ES_DISPLAY_REQUIRED
            : Native.ES_CONTINUOUS);
        if (_keepAwakeBox != null) _keepAwakeBox.ForeColor = on ? Theme.Green : Theme.Text;
    }

    // --- 言語 ---

    // ドロップダウンの表示を現在の言語に合わせる
    void SyncLanguageChecks()
    {
        string code = Strings.CurrentCode();
        _langBox.SelectedItem = Strings.LanguageOptions().First(o => o.Code == code).Label;
    }

    void OnLanguageChanged(string code)
    {
        if (code == Strings.CurrentCode())
        {
            SyncLanguageChecks();
            return;
        }

        if (code == Strings.SystemValue && Strings.SystemLang() == null)
        {
            // 変更前の言語で確認する。「他の言語に変更する」なら元の選択に戻す
            if (!AskUnsupported(Strings.Lang))
            {
                SyncLanguageChecks();
                return;
            }
            code = "en-US";
        }

        Strings.ApplyLanguage(code);
        BuildUi();
    }

    void PromptUnsupportedAtStart()
    {
        if (Strings.SystemMode && Strings.SystemLang() == null && AskUnsupported(Strings.Lang))
        {
            Strings.ApplyLanguage("en-US");
            BuildUi();
        }
    }

    // システムの規定が対応外のときの確認。英語を使う場合は true
    bool AskUnsupported(string lang)
    {
        using (var dlg = new Form
        {
            Text = Strings.TIn(lang, "info_title"),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(440, 200),
            BackColor = Theme.Frame,
            Font = Theme.Ui(9),
        })
        {
            var icon = Ui.Label("ℹ", 32, true);
            dlg.Controls.Add(icon);
            icon.Location = new Point((dlg.ClientSize.Width - icon.Width) / 2, 14);

            var msg = new Label
            {
                Text = Strings.TIn(lang, "sys_unsupported_msg"),
                Font = Theme.Ui(13),
                ForeColor = Theme.Text,
                MaximumSize = new Size(380, 0),
                AutoSize = true,
            };
            dlg.Controls.Add(msg);
            msg.Location = new Point((dlg.ClientSize.Width - msg.PreferredWidth) / 2, icon.Bottom + 8);

            var english = Ui.Button(Strings.TIn(lang, "btn_use_english"), 12, true, null, 160, 30);
            english.DialogResult = DialogResult.Yes;
            var change = Ui.Button(Strings.TIn(lang, "btn_change_language"), 12, true, Theme.ClearBtn, 160, 30);
            change.DialogResult = DialogResult.No;
            dlg.Controls.Add(english);
            dlg.Controls.Add(change);
            int buttonY = Math.Max(msg.Bottom + 16, 140);
            english.Location = new Point(dlg.ClientSize.Width / 2 - english.Width - 8, buttonY);
            change.Location = new Point(dlg.ClientSize.Width / 2 + 8, buttonY);
            dlg.AcceptButton = english;
            dlg.CancelButton = change;

            return dlg.ShowDialog(this) == DialogResult.Yes;
        }
    }

    // --- ACF の保存・読込（JSON 形式、旧版と互換） ---

    void SaveAcf()
    {
        using (var dlg = new SaveFileDialog { DefaultExt = "acf", Filter = "AutoClicker Config (*.acf)|*.acf" })
        {
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            string json;
            lock (_engine.Sync) json = JsonSerializer.Serialize(_engine.Actions, JsonOptions);
            File.WriteAllText(dlg.FileName, json, new UTF8Encoding(false));
        }
    }

    void LoadAcf()
    {
        using (var dlg = new OpenFileDialog { Title = Strings.T("import_acf_title"), Filter = "AutoClicker Config (*.acf)|*.acf" })
        {
            if (dlg.ShowDialog(this) != DialogResult.OK) return;
            var loaded = JsonSerializer.Deserialize<List<MacroAction>>(File.ReadAllText(dlg.FileName, Encoding.UTF8)) ?? new List<MacroAction>();
            foreach (var a in loaded) MigrateAction(a);
            lock (_engine.Sync)
            {
                _engine.Actions.Clear();
                _engine.Actions.AddRange(loaded);
            }
            RefreshList();
        }
    }

    // 旧バージョン（日本語のまま保存された値）を内部キーに変換する
    static void MigrateAction(MacroAction a)
    {
        if (a.Btn != null && Strings.LegacyValues.TryGetValue(a.Btn, out string btn)) a.Btn = btn;
        if (a.Mode != null && Strings.LegacyValues.TryGetValue(a.Mode, out string mode)) a.Mode = mode;
    }

    // --- 起動・終了時の処理 ---

    IntPtr OnKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        // F8 で実行/停止（キーは他のアプリにも届く）
        if (nCode >= 0 && (wParam == (IntPtr)Native.WM_KEYDOWN || wParam == (IntPtr)Native.WM_SYSKEYDOWN))
        {
            int vkCode = Marshal.ReadInt32(lParam);
            if (vkCode == Native.VK_F8) BeginInvoke(new Action(() => _engine.Toggle(this)));
        }
        return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    void Shutdown()
    {
        _engine.Stop();
        SetKeepAwake(false);
        if (_hook != IntPtr.Zero)
        {
            Native.UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    static Dictionary<string, Image> LoadImages()
    {
        var files = new Dictionary<string, string>
        {
            ["left"] = "left_click.png",
            ["right"] = "right_click.png",
            ["drag_straight"] = "drag_straight.png",
            ["drag_via"] = "drag_via.png",
            ["text"] = "text.png",
            ["hotkey"] = "hotkey.png",
            ["wait"] = "wait.png",
        };

        var result = new Dictionary<string, Image>();
        foreach (var kv in files)
        {
            using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("images." + kv.Value))
            {
                if (s == null) continue;
                using (var src = new Bitmap(s))
                {
                    // Python 版の subsample(4, 4) と同じく 1/4 に縮小する
                    var small = new Bitmap(Math.Max(1, src.Width / 4), Math.Max(1, src.Height / 4));
                    using (var g = Graphics.FromImage(small))
                    {
                        g.InterpolationMode = InterpolationMode.NearestNeighbor;
                        g.DrawImage(src, 0, 0, small.Width, small.Height);
                    }
                    result[kv.Key] = small;
                }
            }
        }
        return result;
    }

    static Icon LoadIcon()
    {
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("app_icon.ico"))
        {
            return s == null ? null : new Icon(s);
        }
    }
}
