using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

// 実行エンジン。項目を順に実行し、繰り返し・条件分岐・計算・変数などを扱う
sealed class MacroEngine
{
    static readonly Dictionary<string, ushort> KeyNames = new Dictionary<string, ushort>
    {
        ["ctrl"] = 0x11, ["lctrl"] = 0xA2, ["rctrl"] = 0xA3,
        ["shift"] = 0x10, ["lshift"] = 0xA0, ["rshift"] = 0xA1,
        ["alt"] = 0x12, ["lalt"] = 0xA4, ["ralt"] = 0xA5,
        ["win"] = 0x5B, ["cmd"] = 0x5B,
        ["enter"] = 0x0D, ["space"] = 0x20, ["tab"] = 0x09, ["esc"] = 0x1B,
        ["delete"] = 0x2E, ["backspace"] = 0x08, ["prtsc"] = 0x2C, ["print_screen"] = 0x2C,
        ["left"] = 0x25, ["up"] = 0x26, ["right"] = 0x27, ["down"] = 0x28,
        ["home"] = 0x24, ["end"] = 0x23, ["page_up"] = 0x21, ["page_down"] = 0x22,
        ["insert"] = 0x2D, ["pause"] = 0x13, ["caps_lock"] = 0x14,
        ["num_lock"] = 0x90, ["scroll_lock"] = 0x91, ["menu"] = 0x5D,
        ["f1"] = 0x70, ["f2"] = 0x71, ["f3"] = 0x72, ["f4"] = 0x73, ["f5"] = 0x74, ["f6"] = 0x75,
        ["f7"] = 0x76, ["f8"] = 0x77, ["f9"] = 0x78, ["f10"] = 0x79, ["f11"] = 0x7A, ["f12"] = 0x7B,
    };

    // 拡張キーとして送る必要がある仮想キー
    static readonly HashSet<ushort> ExtendedKeys = new HashSet<ushort>
    {
        0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x2C, 0x2D, 0x2E, 0x5B, 0x5C, 0x5D, 0xA3, 0xA5, 0x90,
    };

    public readonly List<MacroAction> Actions = new List<MacroAction>();
    public readonly object Sync = new object();

    public event Action<bool> StateChanged;

    volatile bool _active;
    volatile bool _running = true;

    public bool Active => _active;

    // 繰り返し回数（0 は無限）。UI への通知は UiHost の UI スレッドで行う
    public int RepeatCount { get; set; }
    public Control UiHost { get; set; }

    // 実行速度（1.0 が標準）。内部の待ち時間だけを割る（待機や長押しの時間は変えない）
    public static double SpeedFactor { get; set; } = 1.0;

    int _cycles;

    // 実行中の値：変数（名前ごと）と、項目の結果（項目の Id ごと）。開始するたびに空に戻す
    readonly Dictionary<string, object> _variables = new Dictionary<string, object>();
    readonly Dictionary<string, object> _results = new Dictionary<string, object>();

    public MacroEngine()
    {
        var thread = new Thread(Loop) { IsBackground = true };
        thread.Start();
    }

    public void Stop() => _running = false;

    // 実行開始/停止の切り替え。画面外の座標があれば開始しない
    public void Toggle(IWin32Window owner)
    {
        if (Actions.Count == 0)
        {
            SetActive(false);
            Dialogs.Show(owner, Strings.T("no_actions"), Strings.T("notice"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!CheckAllCoordinates())
        {
            SetActive(false);
            Dialogs.Show(owner, Strings.T("invalid_coord"), Strings.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        SetActive(!_active);
    }

    void SetActive(bool value)
    {
        _active = value;
        if (value)
        {
            _cycles = 0;
            _variables.Clear();
            _results.Clear();
        }
        StateChanged?.Invoke(value);
    }

    static bool IsValidCoordinate(int x, int y)
    {
        var bounds = Screen.PrimaryScreen.Bounds;
        return x >= 0 && x < bounds.Width && y >= 0 && y < bounds.Height;
    }

    bool CheckAllCoordinates()
    {
        lock (Sync)
        {
            foreach (var act in Actions)
            {
                if (act.Type == "click" || act.Type == "hold")
                {
                    if (!IsValidCoordinate(act.X ?? 0, act.Y ?? 0)) return false;
                    if (act.Type == "hold")
                    {
                        foreach (var wp in act.Waypoints ?? new List<int[]>())
                            if (!IsValidCoordinate(wp[0], wp[1])) return false;
                    }
                }
                else if (act.Type == "drag")
                {
                    if (!IsValidCoordinate(act.Sx ?? 0, act.Sy ?? 0)) return false;
                    if (!IsValidCoordinate(act.Ex ?? 0, act.Ey ?? 0)) return false;
                    foreach (var wp in act.Waypoints ?? new List<int[]>())
                        if (!IsValidCoordinate(wp[0], wp[1])) return false;
                }
            }
        }
        return true;
    }

    void Loop()
    {
        while (_running)
        {
            if (!_active || Actions.Count == 0)
            {
                Thread.Sleep(200);
                continue;
            }

            MacroAction[] snapshot;
            lock (Sync) snapshot = Actions.ToArray();

            bool finished;
            try
            {
                finished = RunRange(snapshot, 0, snapshot.Length);
            }
            catch (MacroError ex)
            {
                // 値が正しくないなどで止まるときは、警告を出して停止する
                _active = false;
                ShowError(ex.Message);
                continue;
            }

            if (!_active)
            {
                // 入力のキャンセルなどで止まったときは、表示を停止に合わせる
                UiHost?.BeginInvoke(new Action(() => StateChanged?.Invoke(false)));
                continue;
            }

            // 1周が終わったら回数を数え、指定の回数に達したら止める（0 は無限）
            if (finished && RepeatCount > 0 && ++_cycles >= RepeatCount)
            {
                _active = false;
                UiHost?.BeginInvoke(new Action(() => StateChanged?.Invoke(false)));
            }
        }
    }

    void ShowError(string message)
    {
        UiHost?.BeginInvoke(new Action(() =>
        {
            Dialogs.Show(UiHost, message, Strings.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            StateChanged?.Invoke(false);
        }));
    }

    // list の [from, to) を順に実行する。
    // 繰り返し（repeat）と条件分岐（if）は、対応する終わり（repeat_end / if_end）まで中身をまとめて扱う。
    // 止められたら false を返す
    bool RunRange(MacroAction[] list, int from, int to)
    {
        for (int i = from; i < to; i++)
        {
            if (!_active) return false;

            var act = list[i];
            switch (act.Type)
            {
                case "repeat":
                {
                    int end = FindBlockEnd(list, i, to, out _);
                    for (int r = 0; r < (act.Count ?? 0); r++)
                    {
                        if (!RunRange(list, i + 1, end)) return false;
                    }
                    i = end;   // 終わりの位置。次のループで、その次から続ける
                    continue;
                }
                case "if":
                {
                    int end = FindBlockEnd(list, i, to, out int elsePos);
                    decimal right = act.B != null ? Num(act.B) : (act.Number ?? 0);
                    bool matched = Compare(Num(act.A), act.Op, right);
                    int bodyEnd = elsePos >= 0 ? elsePos : end;
                    if (matched)
                    {
                        if (!RunRange(list, i + 1, bodyEnd)) return false;
                    }
                    else if (elsePos >= 0)
                    {
                        if (!RunRange(list, elsePos + 1, end)) return false;
                    }
                    i = end;
                    continue;
                }
                case "repeat_end":
                case "else":
                case "if_end":
                    continue;
                case "calc":
                    _results[act.Id] = Calculate(act);
                    continue;
                case "round":
                    _results[act.Id] = Round(act);
                    continue;
                case "convert":
                    _results[act.Id] = Convert(act);
                    continue;
                case "input":
                {
                    string text = AskInput(act);
                    if (text == null)
                    {
                        // 入力をキャンセルしたら、マクロを停止する
                        _active = false;
                        return false;
                    }
                    _results[act.Id] = text;
                    if (!string.IsNullOrEmpty(act.Var)) _variables[act.Var] = text;
                    continue;
                }
                case "setvar":
                    _variables[RequireName(act)] = Eval(act.A);
                    continue;
                case "addvar":
                    AddToVariable(act);
                    continue;
                case "getvar":
                    _results[act.Id] = Snapshot(Eval(ValueRef.Variable(RequireName(act))));
                    continue;
                case "cond":
                    _results[act.Id] = Compare(Num(act.A), act.Op, Num(act.B));
                    continue;
            }

            Run(act);
            Wait(100);
        }
        return true;
    }

    // 始まり（repeat / if）に対応する終わりの位置を探す（入れ子に対応）。なければ範囲の最後。
    // if の場合は、同じ段にある「その他の場合」（else）の位置も返す（なければ -1）
    static int FindBlockEnd(MacroAction[] list, int start, int to, out int elsePos)
    {
        elsePos = -1;
        bool isIf = list[start].Type == "if";
        int depth = 0;
        for (int j = start + 1; j < to; j++)
        {
            string t = list[j].Type;
            if (t == "repeat" || t == "if") depth++;
            else if (t == "repeat_end" || t == "if_end")
            {
                if (depth == 0) return j;
                depth--;
            }
            else if (t == "else" && depth == 0 && isIf && elsePos < 0) elsePos = j;
        }
        return to;
    }

    // --- 値の扱い ---

    // 参照した値を読む。変数がない、結果がない、といった場合は警告で止める
    object Eval(ValueRef r)
    {
        if (r == null) throw new MacroError(Strings.T("error_no_value"));
        switch (r.Kind)
        {
            case "var":
                if (!_variables.TryGetValue(r.Name ?? "", out object v))
                    throw new MacroError(Strings.T("error_var_missing", ("name", r.Name ?? "")));
                return v;
            case "res":
                if (!_results.TryGetValue(r.Ref ?? "", out object rv))
                    throw new MacroError(Strings.T("error_result_missing"));
                return rv;
            default:
                // 数値として読めればその数値、読めなければ文字
                return decimal.TryParse(r.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal d) ? (object)d : (r.Text ?? "");
        }
    }

    // 数値として読む（真偽値は 1 / 0）
    decimal Num(ValueRef r) => ToDecimal(Eval(r));

    static decimal ToDecimal(object v)
    {
        switch (v)
        {
            case decimal d: return d;
            case bool b: return b ? 1 : 0;
            case string s when decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal p): return p;
            default: throw new MacroError(Strings.T("error_number", ("value", ToText(v))));
        }
    }

    static string ToText(object v)
    {
        switch (v)
        {
            case null: return "";
            case decimal d: return d.ToString(CultureInfo.InvariantCulture);
            case bool b: return b ? "true" : "false";
            case List<object> list: return string.Join(", ", list.Select(ToText));
            default: return v.ToString();
        }
    }

    static object Snapshot(object v) => v is List<object> list ? new List<object>(list) : v;

    static bool Compare(decimal a, string op, decimal b)
    {
        switch (op)
        {
            case "neq": return a != b;
            case "gt": return a > b;
            case "lt": return a < b;
            case "ge": return a >= b;
            case "le": return a <= b;
            default: return a == b;
        }
    }

    static string RequireName(MacroAction act)
    {
        if (string.IsNullOrWhiteSpace(act.Var)) throw new MacroError(Strings.T("error_var_name"));
        return act.Var;
    }

    void AddToVariable(MacroAction act)
    {
        string name = RequireName(act);
        object value = Eval(act.A);
        _variables.TryGetValue(name, out object current);
        List<object> list;
        if (current is List<object> existing) list = existing;
        else
        {
            list = new List<object>();
            if (current != null) list.Add(current);
        }
        list.Add(value);
        _variables[name] = list;
    }

    // --- 計算・端数・型変換・入力 ---

    decimal Calculate(MacroAction act)
    {
        decimal a = Num(act.A);
        decimal b = act.B != null ? Num(act.B) : 0;
        switch (act.Op ?? "add")
        {
            case "sub": return a - b;
            case "mul": return a * b;
            case "div":
                if (b == 0) throw new MacroError(Strings.T("error_div_zero"));
                return a / b;
            case "mod":
                if (b == 0) throw new MacroError(Strings.T("error_div_zero"));
                return a % b;
            case "gcd":
            {
                long x = AsInteger(a), y = AsInteger(b);
                return Gcd(Math.Abs(x), Math.Abs(y));
            }
            case "lcm":
            {
                long x = AsInteger(a), y = AsInteger(b);
                if (x == 0 || y == 0) return 0;
                long g = Gcd(Math.Abs(x), Math.Abs(y));
                return Math.Abs((decimal)x * y) / g;
            }
            case "pow":
            {
                double r = Math.Pow((double)a, (double)b);
                if (double.IsNaN(r) || double.IsInfinity(r)) throw new MacroError(Strings.T("error_math"));
                return (decimal)r;
            }
            case "sqrt":
                if (a < 0) throw new MacroError(Strings.T("error_math"));
                return (decimal)Math.Sqrt((double)a);
            case "abs": return Math.Abs(a);
            case "max": return Math.Max(a, b);
            case "min": return Math.Min(a, b);
            default: return a + b;
        }
    }

    static long AsInteger(decimal d)
    {
        if (d != Math.Truncate(d)) throw new MacroError(Strings.T("error_not_integer"));
        return (long)d;
    }

    static long Gcd(long x, long y)
    {
        while (y != 0)
        {
            long t = x % y;
            x = y;
            y = t;
        }
        return x;
    }

    decimal Round(MacroAction act)
    {
        decimal v = Num(act.A);
        int place = act.Place ?? 0;
        decimal scale = 1m;
        for (int k = 0; k < Math.Abs(place); k++)
        {
            if (place > 0) scale *= 10;
            else scale /= 10;
        }
        decimal x = v / scale;
        decimal y;
        switch (act.RMode)
        {
            case "floor": y = Math.Floor(x); break;
            case "ceil": y = Math.Ceiling(x); break;
            default: y = Math.Round(x, 0, MidpointRounding.AwayFromZero); break;
        }
        return y * scale;
    }

    object Convert(MacroAction act)
    {
        object source = Eval(act.A);
        object result;
        switch (act.Target ?? "string")
        {
            case "int":
                result = AsInteger(ToDecimal(source));
                break;
            case "decimal":
                result = ToDecimal(source);
                break;
            case "bool":
                result = ToBool(source);
                break;
            default:
                result = ToText(source);
                break;
        }
        if (!string.IsNullOrEmpty(act.Var)) _variables[act.Var] = result;
        return result;
    }

    static bool ToBool(object v)
    {
        if (v is bool b) return b;
        if (v is string s)
        {
            if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase)) return true;
            if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase)) return false;
        }
        throw new MacroError(Strings.T("error_bool", ("value", ToText(v))));
    }

    // 入力ダイアログを UI スレッドで出し、入力を待つ。キャンセルなら null
    string AskInput(MacroAction act)
    {
        string prompt = act.Prompt ?? "";
        bool multiline = act.Multiline == true;
        string defaultText = act.Default != null ? ToText(Eval(act.Default)) : "";
        if (UiHost == null) return defaultText;
        return (string)UiHost.Invoke(new Func<string>(() => InputForm.Ask(UiHost, prompt, multiline, defaultText)));
    }

    // --- 実際の操作 ---

    void Wait(int ms)
    {
        if (ms > 0) Thread.Sleep((int)Math.Max(1, ms / SpeedFactor));
    }

    void Run(MacroAction act)
    {
        switch (act.Type)
        {
            case "click":
                MoveTo(act.X ?? 0, act.Y ?? 0);
                Wait(50);
                Click(IsRight(act), down: true);
                Click(IsRight(act), down: false);
                break;

            case "drag":
                MoveTo(act.Sx ?? 0, act.Sy ?? 0);
                Wait(50);
                Click(IsRight(act), down: true);
                Wait(50);
                foreach (var wp in act.Waypoints ?? new List<int[]>())
                {
                    MoveTo(wp[0], wp[1]);
                    Wait(100);
                }
                MoveTo(act.Ex ?? 0, act.Ey ?? 0);
                Wait(50);
                Click(IsRight(act), down: false);
                break;

            case "hold":
                MoveTo(act.X ?? 0, act.Y ?? 0);
                Wait(50);
                Click(IsRight(act), down: true);
                var start = Stopwatch.StartNew();
                foreach (var wp in act.Waypoints ?? new List<int[]>())
                {
                    MoveTo(wp[0], wp[1]);
                    Wait(100);
                }
                // 合計の長押し時間になるまで待ってから離す
                int remaining = (act.Duration ?? 0) - (int)start.ElapsedMilliseconds;
                if (remaining > 0) Thread.Sleep(remaining);
                Click(IsRight(act), down: false);
                break;

            case "keyboard":
                if (act.Mode == "text")
                {
                    TypeText(act.Value ?? "");
                }
                else
                {
                    PressHotkey(act.Value ?? "");
                }
                break;

            case "wait":
                Thread.Sleep(act.Duration ?? 0);
                break;
        }
    }

    static bool IsRight(MacroAction act) => (act.Btn ?? "").Contains("right");

    static void MoveTo(int x, int y) => Native.SetCursorPos(x, y);

    static void Click(bool right, bool down)
    {
        uint flag = right
            ? (down ? Native.MOUSEEVENTF_RIGHTDOWN : Native.MOUSEEVENTF_RIGHTUP)
            : (down ? Native.MOUSEEVENTF_LEFTDOWN : Native.MOUSEEVENTF_LEFTUP);
        Native.mouse_event(flag, 0, 0, 0, UIntPtr.Zero);
    }

    static void SendKey(ushort vk, bool up)
    {
        uint flags = up ? Native.KEYEVENTF_KEYUP : 0;
        if (ExtendedKeys.Contains(vk)) flags |= Native.KEYEVENTF_EXTENDEDKEY;
        SendKeyInput(new Native.KEYBDINPUT { wVk = vk, dwFlags = flags });
    }

    static void SendChar(char c, bool up)
    {
        uint flags = Native.KEYEVENTF_UNICODE | (up ? Native.KEYEVENTF_KEYUP : 0);
        SendKeyInput(new Native.KEYBDINPUT { wVk = 0, wScan = c, dwFlags = flags });
    }

    static void SendKeyInput(Native.KEYBDINPUT ki)
    {
        var input = new Native.INPUT[1];
        input[0].type = Native.INPUT_KEYBOARD;
        input[0].u.ki = ki;
        Native.SendInput(1, input, Marshal.SizeOf(typeof(Native.INPUT)));
    }

    static void TypeText(string text)
    {
        foreach (char c in text)
        {
            SendChar(c, up: false);
            SendChar(c, up: true);
        }
    }

    // "{ctrl+c}" や "ctrl+c" のような文字列を仮想キーの列に変換する。
    // 解釈できないキーがあれば false を返す
    static bool TryParseHotkey(string value, out List<ushort> keys)
    {
        keys = new List<ushort>();
        string hk = value.ToLowerInvariant().Trim();
        if (hk.StartsWith("{") && hk.EndsWith("}")) hk = hk.Substring(1, hk.Length - 2);

        foreach (string raw in hk.Split('+'))
        {
            string p = raw.Trim();
            if (p == "fn") continue;  // fn は環境依存のため送らない

            if (p.Length == 1)
            {
                short scan = Native.VkKeyScan(p[0]);
                if (scan == -1) return false;
                if ((scan & 0x0100) != 0) keys.Add(0x10);  // Shift が必要な文字
                keys.Add((ushort)(scan & 0xFF));
            }
            else if (KeyNames.TryGetValue(p, out ushort vk))
            {
                keys.Add(vk);
            }
            else
            {
                return false;
            }
        }
        return keys.Count > 0;
    }

    static void PressHotkey(string value)
    {
        // 解釈できないキーを含む場合は何も送らない（押しっぱなしになるのを防ぐ）
        if (!TryParseHotkey(value, out List<ushort> keys)) return;

        foreach (ushort k in keys) SendKey(k, up: false);
        Thread.Sleep(50);
        for (int i = keys.Count - 1; i >= 0; i--) SendKey(keys[i], up: true);
    }
}
