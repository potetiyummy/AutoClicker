using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// 値の選択肢（数値を入れる、変数を選ぶ、計算結果などを選ぶ）
sealed class ValueChoice
{
    public string Display;
    public ValueRef Ref;   // null のときは「数値」（欄に入れた文字を使う）
}

// 値を選ぶ欄：ドロップダウン（数値／変数／項目の結果）と、数値を入れる欄
sealed class ValueBox : Panel
{
    readonly ComboBox _choice;
    readonly TextBox _literal;
    List<ValueChoice> _items = new List<ValueChoice>();

    public ValueBox(List<ValueChoice> items, ValueRef current)
    {
        Size = new Size(240, 28);
        _choice = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(10),
            Location = new Point(0, 0),
            Width = 130,
        };
        _literal = new TextBox
        {
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.Ui(10),
            Location = new Point(138, 2),
            Width = 100,
        };
        Controls.Add(_choice);
        Controls.Add(_literal);
        _choice.SelectedIndexChanged += (s, e) => _literal.Visible = _choice.SelectedIndex == 0;
        SetChoices(items, current);
    }

    // 選択肢を入れ直す（現在の値は、選択肢になくても残す）
    public void SetChoices(List<ValueChoice> items, ValueRef current)
    {
        var list = new List<ValueChoice> { new ValueChoice { Display = Strings.T("value_number"), Ref = null } };
        list.AddRange(items);
        _items = list;

        _choice.Items.Clear();
        foreach (var c in _items) _choice.Items.Add(c.Display);

        int index = 0;
        if (current != null && current.Kind != "lit")
        {
            index = _items.FindIndex(c => c.Ref != null && c.Ref.Kind == current.Kind && c.Ref.Name == current.Name && c.Ref.Ref == current.Ref);
            if (index < 0)
            {
                // 選択肢にない参照（すでに消えた変数や項目など）も、そのまま選んだ状態で残す
                _items.Add(new ValueChoice { Display = Describe(current), Ref = current });
                _choice.Items.Add(_items[_items.Count - 1].Display);
                index = _items.Count - 1;
            }
        }
        else if (current == null)
        {
            _literal.Text = "0";
        }
        else
        {
            _literal.Text = current.Text ?? "";
        }
        _choice.SelectedIndex = index;
        _literal.Visible = index == 0;
    }

    static string Describe(ValueRef r)
    {
        switch (r.Kind)
        {
            case "var": return Strings.T("value_var", ("name", r.Name ?? ""));
            case "res": return Strings.T("value_missing");
            default: return r.Text ?? "";
        }
    }

    public ValueRef Get()
    {
        var c = _items[Math.Max(0, _choice.SelectedIndex)];
        if (c.Ref == null) return ValueRef.Literal(_literal.Text.Trim());
        return c.Ref;
    }
}

// アクションの種類ごとに、必要な入力欄だけを並べる部品。編集と、その他タブの追加で使う
sealed class ActionFields
{
    static readonly string[] CalcOps = { "add", "sub", "mul", "div", "mod", "gcd", "lcm", "pow", "sqrt", "abs", "max", "min" };
    static readonly string[] CalcOneOperand = { "sqrt", "abs" };
    static readonly string[] RoundModes = { "round", "floor", "ceil" };
    static readonly string[] ConvertTargets = { "string", "int", "decimal", "bool" };

    // 端数を処理の「位」（10 の何乗か）と、その名前
    static readonly int[] PlaceValues = { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, -1, -2, -3, -4, -5, -6, -7, -8, -9, -10, -11 };
    static readonly string[] PlaceNames =
    {
        "1000億の位", "100億の位", "10億の位", "1億の位", "1000万の位", "100万の位", "10万の位", "1万の位",
        "1000の位", "100の位", "10の位", "1の位（整数）", "10分の1の位", "100分の1の位", "1000分の1の位", "1万分の1の位",
        "10万分の1の位", "100万分の1の位", "1000万分の1の位", "1億分の1の位", "10億分の1の位", "100億分の1の位", "1000億分の1の位",
    };

    readonly Panel _host;
    readonly Func<List<ValueChoice>> _choices;
    readonly Func<List<string>> _varNames;
    readonly List<Control> _rows = new List<Control>();
    string _type;
    int _y;

    // 入力欄の部品（その種類で使うものだけが作られる）
    ComboBox _op, _place, _mode, _target;
    ValueBox _a, _b, _default;
    ComboBox _name;
    TextBox _prompt;
    CheckBox _multiline;
    NumericUpDown _count;
    TextBox _save;
    List<Control> _bRow = new List<Control>();

    public ActionFields(Panel host, Func<List<ValueChoice>> choices, Func<List<string>> varNames)
    {
        _host = host;
        _choices = choices;
        _varNames = varNames;
    }

    // 値の参照として選べる項目（結果を持つ項目と、使われている変数）
    static readonly string[] ResultTypes = { "calc", "round", "convert", "input", "getvar", "cond" };

    public static List<ValueChoice> ChoicesFrom(List<MacroAction> actions)
    {
        var list = new List<ValueChoice>();
        for (int i = 0; i < actions.Count; i++)
        {
            var a = actions[i];
            if (ResultTypes.Contains(a.Type))
            {
                list.Add(new ValueChoice
                {
                    Display = Strings.T("value_result", ("index", i + 1), ("kind", TypeName(a.Type))),
                    Ref = ValueRef.Result(a.Id),
                });
            }
        }
        foreach (var name in VarNamesFrom(actions))
        {
            list.Add(new ValueChoice { Display = Strings.T("value_var", ("name", name)), Ref = ValueRef.Variable(name) });
        }
        return list;
    }

    public static List<string> VarNamesFrom(List<MacroAction> actions) =>
        actions.Select(a => a.Var).Where(v => !string.IsNullOrWhiteSpace(v)).Distinct().ToList();

    // 入力欄の下端までの高さ（ダイアログの大きさに使う）
    public int ContentHeight => _y;

    // 位の名前（10 の何乗か）
    public static string PlaceName(int place)
    {
        int index = Array.IndexOf(PlaceValues, place);
        return PlaceNames[index < 0 ? 11 : index];
    }

    // 項目の表示名（その他タブのドロップダウンと、リストで使う）
    public static string TypeName(string type)
    {
        switch (type)
        {
            case "repeat": return Strings.T("repeat");
            case "if": return Strings.T("if_block");
            case "calc": return Strings.T("calc");
            case "round": return Strings.T("round");
            case "convert": return Strings.T("convert");
            case "input": return Strings.T("input");
            case "setvar": return Strings.T("setvar");
            case "addvar": return Strings.T("addvar");
            case "getvar": return Strings.T("getvar");
            case "cond": return Strings.T("cond");
            default: return type;
        }
    }

    // 種類を選んで、その入力欄を作る。source があれば、その値を入れる
    public void Show(string type, MacroAction source)
    {
        foreach (var c in _bRow)
        {
            _host.Controls.Remove(c);
            c.Dispose();
        }
        _rows.Clear();
        _type = type;
        _bRow = new List<Control>();
        _y = 8;

        switch (type)
        {
            case "repeat":
                _count = NumberBox(1, 9999, source?.Count ?? 2);
                AddRow(Strings.T("field_count"), _count);
                break;
            case "if":
                _a = AddValue(Strings.T("field_left"), source?.A);
                _op = AddOptions(Strings.T("field_op"), Strings.T("if_ops").Split('|'), IndexOf(Strings.IfOpKeys, source?.Op), Strings.IfOpKeys);
                _b = AddValue(Strings.T("field_right"), source?.B ?? (source?.Number.HasValue == true ? ValueRef.Literal(source.Number.Value.ToString()) : null));
                break;
            case "cond":
                _a = AddValue(Strings.T("field_left"), source?.A);
                _op = AddOptions(Strings.T("field_op"), Strings.T("if_ops").Split('|'), IndexOf(Strings.IfOpKeys, source?.Op), Strings.IfOpKeys);
                _b = AddValue(Strings.T("field_right"), source?.B);
                break;
            case "calc":
                _op = AddOptions(Strings.T("field_calc"), CalcOps.Select(k => Strings.T("op_" + k)).ToArray(), IndexOf(CalcOps, source?.Op), CalcOps);
                _a = AddValue(Strings.T("field_left"), source?.A);
                int bStart = _rows.Count;
                _b = AddValue(Strings.T("field_right"), source?.B);
                _bRow = _rows.Skip(bStart).ToList();
                _op.SelectedIndexChanged += (s, e) => ShowCalcRight();
                ShowCalcRight();
                break;
            case "round":
                _a = AddValue(Strings.T("field_source"), source?.A);
                _place = AddOptions(Strings.T("field_place"), PlaceNames, IndexOf(PlaceValues, source?.Place ?? 0), null);
                _mode = AddOptions(Strings.T("field_mode"), RoundModes.Select(k => Strings.T("round_" + k)).ToArray(), IndexOf(RoundModes, source?.RMode ?? "round"), RoundModes);
                break;
            case "convert":
                _a = AddValue(Strings.T("field_source"), source?.A);
                _target = AddOptions(Strings.T("field_target"), ConvertTargets.Select(k => Strings.T("type_" + k)).ToArray(), IndexOf(ConvertTargets, source?.Target ?? "string"), ConvertTargets);
                _save = AddText(Strings.T("field_save"), source?.Var ?? "");
                break;
            case "input":
                _prompt = AddText(Strings.T("field_prompt"), source?.Prompt ?? "");
                _multiline = new CheckBox { Text = Strings.T("field_multiline"), Checked = source?.Multiline == true, Font = Theme.Ui(10), ForeColor = Theme.Text, AutoSize = true };
                AddRow("", _multiline);
                _default = AddValue(Strings.T("field_default"), source?.Default ?? ValueRef.Literal(""));
                _save = AddText(Strings.T("field_save"), source?.Var ?? "");
                break;
            case "setvar":
            case "addvar":
            case "getvar":
                _name = AddNameBox(Strings.T("field_var"), source?.Var ?? "");
                if (type != "getvar") _a = AddValue(Strings.T("field_value"), source?.A);
                break;
        }
    }

    // 入力内容を、アクションとして読み取る。Id は元の項目を引き継ぐ
    public MacroAction Read(MacroAction source)
    {
        var a = new MacroAction { Type = _type };
        if (source != null) a.Id = source.Id;
        switch (_type)
        {
            case "repeat":
                a.Count = (int)_count.Value;
                break;
            case "if":
            case "cond":
                a.A = _a.Get();
                a.Op = Strings.IfOpKeys[_op.SelectedIndex];
                a.B = _b.Get();
                break;
            case "calc":
                a.Op = CalcOps[_op.SelectedIndex];
                a.A = _a.Get();
                if (!CalcOneOperand.Contains(a.Op)) a.B = _b.Get();
                break;
            case "round":
                a.A = _a.Get();
                a.Place = PlaceValues[_place.SelectedIndex];
                a.RMode = RoundModes[_mode.SelectedIndex];
                break;
            case "convert":
                a.A = _a.Get();
                a.Target = ConvertTargets[_target.SelectedIndex];
                a.Var = _save.Text.Trim();
                break;
            case "input":
                a.Prompt = _prompt.Text;
                a.Multiline = _multiline.Checked;
                a.Default = _default.Get();
                a.Var = _save.Text.Trim();
                break;
            case "setvar":
            case "addvar":
                a.Var = _name.Text.Trim();
                a.A = _a.Get();
                break;
            case "getvar":
                a.Var = _name.Text.Trim();
                break;
        }
        return a;
    }

    // 計算で、演算子が 1 つの値だけを使う場合は、右の値の欄を隠す
    void ShowCalcRight()
    {
        bool one = CalcOneOperand.Contains(CalcOps[_op.SelectedIndex]);
        foreach (var c in _bRow) c.Visible = !one;
    }

    // --- 行の作り方 ---

    void AddRow(string caption, Control control)
    {
        var label = Ui.Label(caption, 11);
        label.Location = new Point(8, _y + 6);
        label.Tag = "row";
        control.Location = new Point(140, _y + 2);
        _host.Controls.Add(label);
        _host.Controls.Add(control);
        _rows.Add(label);
        _rows.Add(control);
        _y += 40;
    }

    ValueBox AddValue(string caption, ValueRef current)
    {
        var box = new ValueBox(_choices(), current);
        AddRow(caption, box);
        return box;
    }

    TextBox AddText(string caption, string value)
    {
        var box = Ui.Entry(11, 220);
        box.Text = value;
        AddRow(caption, box);
        return box;
    }

    ComboBox AddNameBox(string caption, string value)
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDown,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(11),
            Width = 220,
        };
        foreach (var n in _varNames()) box.Items.Add(n);
        box.Text = value;
        AddRow(caption, box);
        return box;
    }

    ComboBox AddOptions(string caption, string[] names, int selected, string[] keys)
    {
        var box = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(11),
            Width = 220,
        };
        foreach (var n in names) box.Items.Add(n);
        box.SelectedIndex = selected < 0 ? 0 : selected;
        AddRow(caption, box);
        return box;
    }

    NumericUpDown NumberBox(int min, int max, int value)
    {
        return new NumericUpDown
        {
            Minimum = min,
            Maximum = max,
            Value = value,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(11),
            Width = 100,
        };
    }


    static int IndexOf(string[] array, string value) => Array.IndexOf(array, value ?? "");

    static int IndexOf(int[] array, int value) => Array.IndexOf(array, value);
}
