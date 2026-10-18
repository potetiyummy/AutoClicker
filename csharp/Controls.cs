using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

// 角丸の形を作る。Round はコントロールの表示領域を角丸に切り抜く
static class Shape
{
    public static GraphicsPath Path(RectangleF r, float radius)
    {
        var p = new GraphicsPath();
        if (radius <= 0)
        {
            p.AddRectangle(r);
            return p;
        }
        float d = radius * 2;
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // radius が負の場合は高さの半分（〇の形）にする
    public static void Round(Control c, int radius)
    {
        void Apply()
        {
            if (c.Width <= 0 || c.Height <= 0) return;
            float r = radius < 0 ? Math.Min(c.Width, c.Height) / 2f : Math.Min(radius, Math.Min(c.Width, c.Height) / 2f);
            using (var path = Path(new RectangleF(0, 0, c.Width, c.Height), r))
                c.Region = new Region(path);
        }
        Apply();
        c.SizeChanged += (s, e) => Apply();
    }
}

// 各種コントロールを同じ見た目で作るヘルパー
static class Ui
{
    public static Button Button(string text, float size, bool bold, Color? bg = null, int width = 0, int height = 28)
    {
        var font = Theme.Ui(size, bold);
        var b = new MarqueeButton
        {
            Text = text,
            Font = font,
            FlatStyle = FlatStyle.Flat,
            BackColor = bg ?? Theme.Accent,
            ForeColor = bg == Theme.PickBtn ? Theme.Text : Theme.OnAccent,
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
            Height = height,
            Width = width > 0 ? width : TextRenderer.MeasureText(text, font).Width + 28,
        };
        b.FlatAppearance.BorderSize = 0;
        b.FlatAppearance.MouseOverBackColor = bg == null ? Theme.AccentHover : (bg ?? Theme.AccentHover);
        Shape.Round(b, -1);
        return b;
    }

    public static TextBox Entry(float size, int width, string placeholder = null)
    {
        var tb = new TextBox
        {
            Font = Theme.Ui(size),
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.None,
            Width = width,
        };
        if (placeholder != null) Cue(tb, placeholder);
        Shape.Round(tb, 10);
        return tb;
    }

    // プレースホルダー（入力前に薄く表示される文字）
    public static void Cue(TextBox tb, string text)
    {
        Native.SendMessage(tb.Handle, Native.EM_SETCUEBANNER, (IntPtr)1, text);
    }

    public static Label Label(string text, float size, bool bold = false, Color? color = null, int maxWidth = 0)
    {
        var l = new MarqueeLabel
        {
            Text = text,
            Font = Theme.Ui(size, bold),
            ForeColor = color ?? Theme.Text,
            AutoSize = true,
        };
        var preferred = l.GetPreferredSize(Size.Empty);
        l.AutoSize = false;
        l.Size = maxWidth > 0 ? new Size(Math.Min(preferred.Width, maxWidth), preferred.Height) : preferred;
        return l;
    }

    // 中央揃えで横一列に並べる。戻り値は次の行の y 座標
    public static int Row(Control host, int y, int gap, params Control[] items)
    {
        int total = items.Sum(c => c.Width) + 6 * (items.Length - 1);
        int x = (host.ClientSize.Width - total) / 2;
        int h = items.Max(c => c.Height);
        foreach (var c in items)
        {
            c.Location = new Point(x, y + (h - c.Height) / 2);
            host.Controls.Add(c);
            x += c.Width + 6;
        }
        return y + h + gap;
    }

    // 1つだけの行（中央揃え）
    public static int Single(Control host, int y, int gap, Control item)
    {
        item.Location = new Point((host.ClientSize.Width - item.Width) / 2, y);
        host.Controls.Add(item);
        return y + item.Height + gap;
    }
}

// CTkSegmentedButton の代わり。選択中の項目を強調表示する
sealed class SegmentedButton : Control
{
    readonly string[] _keys;
    string _selected;
    readonly float _fontSize;

    public event EventHandler SelectionChanged;

    public SegmentedButton(string[] keys, string selected, float fontSize, int width)
    {
        _keys = keys;
        _selected = selected ?? keys[0];
        _fontSize = fontSize;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        Size = new Size(width, 28);
        Cursor = Cursors.Hand;
        TabStop = false;
    }

    public string Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            Invalidate();
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Parent?.BackColor ?? Theme.Frame);
        float pill = Height / 2f;
        using (var b = new SolidBrush(Theme.Track))
        using (var track = Shape.Path(new RectangleF(0, 0, Width - 1, Height - 1), pill))
            g.FillPath(b, track);

        float seg = (float)Width / _keys.Length;
        using (var font = Theme.Ui(_fontSize))
        using (var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
        {
            for (int i = 0; i < _keys.Length; i++)
            {
                var rect = new RectangleF(i * seg, 0, seg, Height);
                if (_keys[i] == _selected)
                {
                    using (var b = new SolidBrush(Theme.Accent))
                    using (var sel = Shape.Path(new RectangleF(rect.X + 2, 2, rect.Width - 4, Height - 5), (Height - 4) / 2f))
                        g.FillPath(b, sel);
                }
                    Marquee.Draw(g, this, Rectangle.Round(new RectangleF(rect.X + 2, 0, rect.Width - 4, Height)), Strings.T(_keys[i]), font, _keys[i] == _selected ? Theme.OnAccent : Theme.Text, StringAlignment.Center);
            }
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || Width <= 0) return;
        int index = Math.Min(_keys.Length - 1, e.X * _keys.Length / Width);
        if (_keys[index] == _selected) return;
        _selected = _keys[index];
        Invalidate();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }
}

// アクションのリスト（右側）。ドラッグで並び替えできる
sealed class MacroListPanel : Control
{
    const int Pad = 5;
    const int ItemHeight = 40;

    const int IndentWidth = 20;

    // 各項目の字下げの深さ（繰り返しの中は 1 つ深い）
    static int[] DepthsOf(List<MacroAction> actions)
    {
        var depths = new int[actions.Count];
        int current = 0;
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].Type == "repeat_end" && current > 0) current--;
            depths[i] = current;
            if (actions[i].Type == "repeat") current++;
        }
        return depths;
    }

    readonly MacroEngine _engine;
    readonly Dictionary<string, Image> _images;
    int _dragIndex = -1;
    int _insertIndex = -1;
    int _dragY;

    public MacroListPanel(MacroEngine engine, Dictionary<string, Image> images)
    {
        _engine = engine;
        _images = images;
        BackColor = Theme.Frame;
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Shape.Round(this, 12);
        SetStyle(ControlStyles.Selectable, true);
        MouseEnter += (s, e) => Focus();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var fill = new SolidBrush(BackColor))
        using (var outline = Shape.Path(new RectangleF(0, 0, Width - 1, Height - 1), 12))
        using (var pen = new Pen(Theme.Border))
        {
            g.FillPath(fill, outline);
            g.DrawPath(pen, outline);
        }

        int w = Width - Pad * 2;
        ClampTop();
        g.TranslateTransform(Pad, Pad - _top);
        g.SetClip(new Rectangle(0, _top, w, VisibleHeight));
        g.SmoothingMode = SmoothingMode.AntiAlias;

        List<MacroAction> actions;
        lock (_engine.Sync) actions = new List<MacroAction>(_engine.Actions);
        int[] depths = DepthsOf(actions);

        int y = 0;
        for (int i = 0; i < actions.Count; i++)
        {
            if (i == _dragIndex) continue;
            if (i == _insertIndex)
            {
                DrawInsertionLines(g, y, w);
                y += ItemHeight / 2;
            }
            DrawItem(g, i, actions[i], depths[i], y, w, false);
            y += ItemHeight;
        }

        if (_insertIndex == actions.Count && _insertIndex >= 0)
            DrawInsertionLines(g, y, w);

        if (_dragIndex >= 0 && _dragIndex < actions.Count)
            DrawItem(g, _dragIndex, actions[_dragIndex], depths[_dragIndex], _dragY - ItemHeight / 2, w, true);
        g.ResetTransform();
        DrawScrollBar(g);
    }

    static void DrawInsertionLines(Graphics g, int y, int width)
    {
        const int space = 6;
        using (var pen = new Pen(Theme.Accent, 2))
        {
            g.DrawLine(pen, 10, y - space, width - 10, y - space);
            g.DrawLine(pen, 10, y + space, width - 10, y + space);
        }
    }

    void DrawItem(Graphics g, int index, MacroAction a, int depth, int y, int width, bool dragging)
    {
        string imgKey = null;
        string text = null;
        int n = index + 1;

        switch (a.Type)
        {
            case "click":
                imgKey = (a.Btn ?? "").Contains("right") ? "right" : "left";
                text = Strings.T("list_click", ("index", n), ("btn", Strings.T(a.Btn)), ("x", a.X ?? 0), ("y", a.Y ?? 0));
                break;
            case "drag":
                int dragCount = a.Waypoints?.Count ?? 0;
                imgKey = dragCount > 0 ? "drag_via" : "drag_straight";
                text = Strings.T("list_drag", ("index", n), ("btn", Strings.T(a.Btn)), ("sx", a.Sx ?? 0), ("count", dragCount), ("ex", a.Ex ?? 0));
                break;
            case "hold":
                imgKey = (a.Btn ?? "").Contains("right") ? "right" : "left";
                int holdCount = a.Waypoints?.Count ?? 0;
                text = holdCount > 0
                    ? Strings.T("list_hold_via", ("index", n), ("btn", Strings.T(a.Btn)), ("x", a.X ?? 0), ("y", a.Y ?? 0), ("count", holdCount), ("duration", a.Duration ?? 0))
                    : Strings.T("list_hold", ("index", n), ("btn", Strings.T(a.Btn)), ("x", a.X ?? 0), ("y", a.Y ?? 0), ("duration", a.Duration ?? 0));
                break;
            case "keyboard":
                imgKey = a.Mode == "text" ? "text" : "hotkey";
                text = Strings.T("list_keyboard", ("index", n), ("kind", Strings.T("keyboard")), ("mode", Strings.T(a.Mode)), ("value", a.Value ?? ""));
                break;
            case "repeat":
                text = Strings.T("list_repeat", ("index", n), ("count", a.Count ?? 0));
                break;
            case "repeat_end":
                text = Strings.T("list_repeat_end", ("index", n));
                break;
            case "wait":
                imgKey = "wait";
                text = Strings.T("list_wait", ("index", n), ("kind", Strings.T("wait")), ("duration", a.Duration ?? 0));
                break;
        }

        // 繰り返しの中身は、字下げして左に縦線を引き、どこからどこまでかを見せる
        for (int l = 0; l < depth; l++)
        {
            int barX = 5 + l * IndentWidth + 8;
            using (var bar = new Pen(Theme.Accent, 2))
                g.DrawLine(bar, barX, y, barX, y + ItemHeight);
        }
        var box = new RectangleF(5 + depth * IndentWidth, y + 2, width - 10 - depth * IndentWidth, ItemHeight - 4);
        using (var fill = new SolidBrush(dragging ? Theme.ItemDragging : Theme.Item))
        using (var path = Shape.Path(box, (ItemHeight - 4) / 2f))
        using (var pen = new Pen(dragging ? Theme.Accent : Theme.Border))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        int textX = 15 + depth * IndentWidth;
        if (imgKey != null && _images.TryGetValue(imgKey, out Image img))
        {
            g.DrawImage(img, 25 + depth * IndentWidth - img.Width / 2, y + ItemHeight / 2 - img.Height / 2, img.Width, img.Height);
            textX = 45 + depth * IndentWidth;
        }

        using (var font = Theme.Ui(12))
        using (var brush = new SolidBrush(Theme.Text))
        using (var sf = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
        {
            Marquee.Draw(g, this, new Rectangle(textX, y, width - textX - 6, ItemHeight), text ?? "", font, Theme.Text, StringAlignment.Near);
        }
    }

    // 右クリックで「編集・複製・削除」を出す。ダブルクリックは編集
    public event Action<int> EditRequested;
    public event Action<int> DuplicateRequested;
    public event Action<int> DeleteRequested;


    int _top;   // 縦スクロールの位置（px）

    int ContentHeight()
    {
        int count;
        lock (_engine.Sync) count = _engine.Actions.Count;
        return count * ItemHeight + ItemHeight / 2;
    }

    int VisibleHeight => Height - Pad * 2;

    void ClampTop()
    {
        _top = Math.Max(0, Math.Min(_top, ContentHeight() - VisibleHeight));
    }

    void ScrollBy(int delta)
    {
        _top += delta;
        ClampTop();
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ScrollBy(-(e.Delta / 120) * ItemHeight);
    }

    // 右端のスクロールバー（項目が収まらないときだけ出す）
    void DrawScrollBar(Graphics g)
    {
        int content = ContentHeight(), visible = VisibleHeight;
        if (content <= visible) return;
        int thumbH = Math.Max(24, visible * visible / content);
        int thumbY = Pad + (int)((visible - thumbH) * (_top / (double)(content - visible)));
        using (var brush = new SolidBrush(Theme.Border))
            g.FillRectangle(brush, Width - 9, thumbY, 4, thumbH);
    }

    int ItemAt(Point p)
    {
        int index = IndexAt(p.Y - Pad + _top);
        int count;
        lock (_engine.Sync) count = _engine.Actions.Count;
        return index >= 0 && index < count ? index : -1;
    }

    void ShowMenu(Point at)
    {
        int index = ItemAt(at);
        if (index < 0) return;

        var menu = new ContextMenuStrip();
        menu.Items.Add(Strings.T("edit"), null, (s, e) => EditRequested?.Invoke(index));
        menu.Items.Add(Strings.T("duplicate"), null, (s, e) => DuplicateRequested?.Invoke(index));
        menu.Items.Add(Strings.T("delete"), null, (s, e) => DeleteRequested?.Invoke(index));
        menu.Show(this, at);
    }

    protected override void OnMouseDoubleClick(MouseEventArgs e)
    {
        base.OnMouseDoubleClick(e);
        if (e.Button != MouseButtons.Left) return;
        int index = ItemAt(e.Location);
        if (index >= 0) EditRequested?.Invoke(index);
    }

    int IndexAt(int localY) => localY < 0 ? -1 : localY / ItemHeight;

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Right) { ShowMenu(e.Location); return; }
        if (e.Button != MouseButtons.Left) return;
        int y = e.Y - Pad + _top;
        int index = IndexAt(y);
        int count;
        lock (_engine.Sync) count = _engine.Actions.Count;
        if (index < 0 || index >= count) return;

        _dragIndex = index;
        _dragY = y;
        Cursor = Cursors.Hand;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragIndex < 0) return;
        int count;
        lock (_engine.Sync) count = _engine.Actions.Count;
        if (e.Y < Pad) ScrollBy(-ItemHeight / 2);
        else if (e.Y > Height - Pad) ScrollBy(ItemHeight / 2);
        int y = Math.Max(0, Math.Min(e.Y - Pad + _top, count * ItemHeight));
        int raw = (y + ItemHeight / 2) / ItemHeight;
        _insertIndex = Math.Max(0, Math.Min(raw, count));
        _dragY = y;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragIndex < 0) return;

        lock (_engine.Sync)
        {
            if (_insertIndex >= 0)
            {
                int target = _insertIndex;
                if (_dragIndex < target) target--;
                if (target != _dragIndex && target >= 0 && target < _engine.Actions.Count)
                {
                    var item = _engine.Actions[_dragIndex];
                    _engine.Actions.RemoveAt(_dragIndex);
                    _engine.Actions.Insert(target, item);
                }
            }
        }

        _dragIndex = -1;
        _insertIndex = -1;
        Cursor = Cursors.Default;
        Invalidate();
    }
}

// 長押しの経由地一覧。実行順に番号を付け、ドラッグで並び替えできる（スクロール可）
sealed class WaypointListPanel : Control
{
    const int Pad = 4;
    const int ItemHeight = 22;
    readonly int _visibleHeight;

    readonly List<int[]> _points;
    int _top;
    int _dragIndex = -1;
    int _insertIndex = -1;
    int _dragY;

    public WaypointListPanel(List<int[]> points, int visibleHeight)
    {
        _points = points;
        _visibleHeight = visibleHeight;
        BackColor = Theme.Dark;
        Size = new Size(350, _visibleHeight + Pad * 2);
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        MouseEnter += (s, e) => Focus();
        Shape.Round(this, 10);
    }

    int ContentHeight => _points.Count * ItemHeight + ItemHeight / 2;

    void ClampTop()
    {
        _top = Math.Max(0, Math.Min(_top, ContentHeight - _visibleHeight));
    }

    public void Redraw() => Invalidate();

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (var fill = new SolidBrush(BackColor))
        using (var outline = Shape.Path(new RectangleF(0, 0, Width - 1, Height - 1), 10))
            g.FillPath(fill, outline);
        int w = Width - Pad * 2;
        g.TranslateTransform(Pad, Pad);
        g.SetClip(new Rectangle(0, 0, w, _visibleHeight));

        if (_points.Count == 0)
        {
            _top = 0;
            using (var font = Theme.Ui(11))
            using (var brush = new SolidBrush(Theme.Gray))
                g.DrawString(Strings.T("no_waypoints"), font, brush, 10, ItemHeight / 2 + 2 - 9);
            return;
        }

        int y = 0;
        for (int i = 0; i < _points.Count; i++)
        {
            if (i == _dragIndex) continue;
            if (i == _insertIndex)
            {
                DrawInsertionLines(g, y - _top, w);
                y += ItemHeight / 2;
            }
            DrawItem(g, i, y - _top, w, false);
            y += ItemHeight;
        }

        if (_insertIndex == _points.Count && _insertIndex >= 0)
            DrawInsertionLines(g, y - _top, w);

        if (_dragIndex >= 0)
            DrawItem(g, _dragIndex, _dragY - ItemHeight / 2 - _top, w, true);
    }

    static void DrawInsertionLines(Graphics g, int y, int width)
    {
        const int space = 4;
        using (var pen = new Pen(Theme.Accent, 2))
        {
            g.DrawLine(pen, 6, y - space, width - 6, y - space);
            g.DrawLine(pen, 6, y + space, width - 6, y + space);
        }
    }

    void DrawItem(Graphics g, int index, int y, int width, bool dragging)
    {
        var point = _points[index];
        var box = new RectangleF(4, y + 2, width - 8, ItemHeight - 4);
        using (var fill = new SolidBrush(dragging ? Theme.ItemDragging : Theme.WaypointItem))
        using (var path = Shape.Path(box, (ItemHeight - 4) / 2f))
        using (var pen = new Pen(dragging ? Theme.Accent : Theme.Border))
        {
            g.FillPath(fill, path);
            g.DrawPath(pen, path);
        }

        using (var font = Theme.Ui(11))
        using (var brush = new SolidBrush(Theme.Text))
            g.DrawString($"{index + 1}. ({point[0]}, {point[1]})", font, brush, 12, y + ItemHeight / 2 - 9);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        int y = e.Y - Pad + _top;
        if (y < 0) return;
        int index = y / ItemHeight;
        if (index >= _points.Count) return;
        _dragIndex = index;
        _dragY = y;
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragIndex < 0) return;
        int y = e.Y - Pad + _top;
        int raw = (y + ItemHeight / 2) / ItemHeight;
        _insertIndex = Math.Max(0, Math.Min(raw, _points.Count));
        _dragY = y;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragIndex < 0) return;

        if (_insertIndex >= 0)
        {
            int target = _insertIndex;
            if (_dragIndex < target) target--;
            if (target != _dragIndex && target >= 0 && target < _points.Count)
            {
                var item = _points[_dragIndex];
                _points.RemoveAt(_dragIndex);
                _points.Insert(target, item);
            }
        }

        _dragIndex = -1;
        _insertIndex = -1;
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _top -= (e.Delta / 120) * ItemHeight;
        ClampTop();
        Invalidate();
    }
}
