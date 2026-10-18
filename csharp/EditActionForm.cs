using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// アクションの内容を編集するウィンドウ。種類に合う項目だけを出す
sealed class EditActionForm : Form
{
    static readonly string[] ClickButtons = { "left_click", "right_click" };
    static readonly string[] DragButtons = { "left_drag", "right_drag" };
    static readonly string[] HoldButtons = { "left_hold", "right_hold" };
    static readonly string[] KeyboardModes = { "text", "hotkey" };

    readonly MacroAction _source;
    readonly ComboBox _btn, _mode;
    readonly TextBox _value;
    readonly Dictionary<string, TextBox> _fields = new Dictionary<string, TextBox>();
    int _y = 20;

    public MacroAction Result { get; private set; }

    public EditActionForm(MacroAction source)
    {
        _source = source;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Theme.Window;
        Font = Theme.Ui(9);
        Text = Strings.T("edit");
        HandleCreated += (s, e) => Native.SetDarkTitleBar(Handle, Theme.IsDark);

        switch (source.Type)
        {
            case "click":
                _btn = AddChoice("click", ClickButtons, source.Btn);
                AddField("X", "x", source.X);
                AddField("Y", "y", source.Y);
                break;
            case "drag":
                _btn = AddChoice("drag", DragButtons, source.Btn);
                AddField("X1", "sx", source.Sx);
                AddField("Y1", "sy", source.Sy);
                AddField("X2", "ex", source.Ex);
                AddField("Y2", "ey", source.Ey);
                break;
            case "hold":
                _btn = AddChoice("hold", HoldButtons, source.Btn);
                AddField("X", "x", source.X);
                AddField("Y", "y", source.Y);
                AddField(Strings.T("ms"), "duration", source.Duration);
                break;
            case "keyboard":
                _mode = AddChoice("keyboard", KeyboardModes, source.Mode);
                _value = AddText(Strings.T("edit_value"), source.Value ?? "");
                break;
            case "repeat":
                AddField(Strings.T("repeat_count"), "count", source.Count);
                break;
            case "repeat_end":
                break;
            case "wait":
                AddField(Strings.T("ms"), "duration", source.Duration);
                break;
        }

        var ok = Ui.Button(Strings.T("ok"), 11, true, null, 100, 30);
        ok.Location = new Point(150, _y + 10);
        ok.Click += (s, e) => { if (TryApply()) DialogResult = DialogResult.OK; };
        var cancel = Ui.Button(Strings.T("cancel"), 11, true, Theme.ClearBtn, 100, 30);
        cancel.Location = new Point(258, _y + 10);
        cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        ClientSize = new Size(380, _y + 56);
    }

    // 選択肢の行（ボタンの種類や、キーボードの入力方式）
    ComboBox AddChoice(string labelKey, string[] keys, string current)
    {
        var label = Ui.Label(Strings.T(labelKey), 11);
        label.Location = new Point(20, _y + 6);
        Controls.Add(label);

        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(11),
            Location = new Point(150, _y + 4),
            Width = 200,
        };
        foreach (var k in keys) box.Items.Add(Strings.T(k));
        int index = System.Array.IndexOf(keys, current);
        box.SelectedIndex = index < 0 ? 0 : index;
        Controls.Add(box);
        _y += 40;
        return box;
    }

    void AddField(string caption, string name, int? value)
    {
        _fields[name] = AddText(caption, value?.ToString() ?? "");
    }

    TextBox AddText(string caption, string value)
    {
        var label = Ui.Label(caption, 11);
        label.Location = new Point(20, _y + 8);
        Controls.Add(label);

        var box = Ui.Entry(12, 200);
        box.Text = value;
        box.Location = new Point(150, _y + 6);
        Controls.Add(box);
        _y += 40;
        return box;
    }

    // 入力内容を Result に反映する。数値でない欄があれば警告を出して、閉じない
    bool TryApply()
    {
        var a = Copy(_source);

        if (_btn != null)
        {
            string[] keys = _source.Type == "click" ? ClickButtons : _source.Type == "drag" ? DragButtons : HoldButtons;
            a.Btn = keys[_btn.SelectedIndex];
        }
        if (_mode != null) a.Mode = KeyboardModes[_mode.SelectedIndex];
        if (_value != null) a.Value = _value.Text;

        if (!ReadInt("x", out int? x)) return false;
        if (!ReadInt("y", out int? y)) return false;
        if (!ReadInt("sx", out int? sx)) return false;
        if (!ReadInt("sy", out int? sy)) return false;
        if (!ReadInt("ex", out int? ex)) return false;
        if (!ReadInt("ey", out int? ey)) return false;
        if (!ReadInt("duration", out int? duration)) return false;
        if (!ReadInt("count", out int? count)) return false;

        a.X = x;
        a.Y = y;
        a.Sx = sx;
        a.Sy = sy;
        a.Ex = ex;
        a.Ey = ey;
        a.Duration = duration;
        a.Count = count;
        Result = a;
        return true;
    }

    // 欄がないときは null のまま（その種類では使わない項目）
    bool ReadInt(string name, out int? value)
    {
        value = null;
        if (!_fields.TryGetValue(name, out TextBox box)) return true;
        if (int.TryParse(box.Text.Trim(), out int v))
        {
            value = v;
            return true;
        }

        Dialogs.Show(this, Strings.T("invalid_coord"), Strings.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    static MacroAction Copy(MacroAction s) => new MacroAction
    {
        Type = s.Type,
        Btn = s.Btn,
        Mode = s.Mode,
        Value = s.Value,
        X = s.X,
        Y = s.Y,
        Sx = s.Sx,
        Sy = s.Sy,
        Ex = s.Ex,
        Ey = s.Ey,
        Duration = s.Duration,
        Count = s.Count,
        Waypoints = s.Waypoints == null ? null : new List<int[]>(s.Waypoints),
    };
}
