using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

// アクセントカラーを選ぶ専用ウィンドウ。プリセット色のほか、
// 「色相・彩度・明暗」「レッド・グリーン・ブルー」「16進数」のどれかで細かく選べる（チェックボックスで切り替え）
sealed class ColorPickerForm : Form
{
    const int Area = 220;   // 色の四角形と色相バーの長さ

    static readonly Color[] Presets =
    {
        Color.FromArgb(0x1F, 0x6A, 0xA5), Color.FromArgb(0x00, 0x78, 0xD7), Color.FromArgb(0x00, 0xB2, 0x94), Color.FromArgb(0x10, 0x7C, 0x41),
        Color.FromArgb(0x7F, 0xBA, 0x00), Color.FromArgb(0xFF, 0xB9, 0x00), Color.FromArgb(0xF7, 0x63, 0x0C), Color.FromArgb(0xE7, 0x48, 0x56),
        Color.FromArgb(0xD1, 0x34, 0x38), Color.FromArgb(0xEA, 0x00, 0x5E), Color.FromArgb(0xB1, 0x46, 0xC2), Color.FromArgb(0x87, 0x64, 0xB8),
        Color.FromArgb(0x74, 0x4D, 0xA9), Color.FromArgb(0x48, 0x68, 0x60), Color.FromArgb(0x69, 0x79, 0x7E), Color.FromArgb(0x30, 0x30, 0x30),
    };

    double _h, _s, _v;
    bool _syncing;          // 数値欄の表示を更新している最中は、入力の処理をしない
    bool _modeBusy;         // 入力方式を切り替えている最中
    Bitmap _sv;
    readonly Bitmap _hueBar;

    readonly Panel _svPanel, _huePanel, _preview;
    readonly TextBox _hex;
    readonly NumericUpDown _r, _g, _b;
    readonly Label _rLabel, _gLabel, _bLabel;
    readonly MarqueeCheckBox _modeHsv, _modeRgb, _modeHex;

    public Color SelectedColor => FromHsv(_h, _s, _v);

    public ColorPickerForm(Color initial)
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 420);
        BackColor = Theme.Window;
        Font = Theme.Ui(9);
        Text = Strings.T("accent_pick");
        HandleCreated += (s, e) => Native.SetDarkTitleBar(Handle, Theme.IsDark);

        ToHsv(initial, out _h, out _s, out _v);
        _hueBar = MakeHueBar();

        // 上段：プリセット色
        for (int i = 0; i < Presets.Length; i++)
        {
            var swatch = new Panel
            {
                Location = new Point(20 + (i % 8) * 36, 20 + (i / 8) * 36),
                Size = new Size(30, 30),
                BackColor = Presets[i],
                Cursor = Cursors.Hand,
            };
            Color preset = Presets[i];
            swatch.Click += (s, e) => SetColor(preset);
            Controls.Add(swatch);
        }

        // 入力方式のチェックボックス（どれか1つだけ選ぶ）
        _modeHsv = ModeBox(Strings.T("color_hsv"), 20);
        _modeRgb = ModeBox(Strings.T("color_rgb"), 150);
        _modeHex = ModeBox(Strings.T("color_hex"), 280);
        _modeHsv.Checked = true;
        _modeHsv.CheckedChanged += (s, e) => SelectMode(_modeHsv);
        _modeRgb.CheckedChanged += (s, e) => SelectMode(_modeRgb);
        _modeHex.CheckedChanged += (s, e) => SelectMode(_modeHex);

        // 色の四角形と色相バー（色相・彩度・明暗のとき）
        _svPanel = new Panel { Location = new Point(20, 150), Size = new Size(Area, Area), Cursor = Cursors.Cross };
        _svPanel.Paint += (s, e) => PaintSv(e.Graphics);
        _svPanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) PickSv(e.X, e.Y); };
        _svPanel.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) PickSv(e.X, e.Y); };
        Controls.Add(_svPanel);

        _huePanel = new Panel { Location = new Point(256, 150), Size = new Size(24, Area), Cursor = Cursors.Hand };
        _huePanel.Paint += (s, e) => PaintHue(e.Graphics);
        _huePanel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) PickHue(e.Y); };
        _huePanel.MouseMove += (s, e) => { if (e.Button == MouseButtons.Left) PickHue(e.Y); };
        Controls.Add(_huePanel);

        // レッド・グリーン・ブルー（RGBのとき）
        _rLabel = ChannelLabel("R", 150);
        _r = ChannelBox(150);
        _gLabel = ChannelLabel("G", 190);
        _g = ChannelBox(190);
        _bLabel = ChannelLabel("B", 230);
        _b = ChannelBox(230);
        _r.ValueChanged += (s, e) => RgbChanged();
        _g.ValueChanged += (s, e) => RgbChanged();
        _b.ValueChanged += (s, e) => RgbChanged();

        // プレビューと16進数（右側）
        _preview = new Panel { Location = new Point(300, 150), Size = new Size(96, 60), BorderStyle = BorderStyle.FixedSingle };
        _preview.Paint += (s, e) => e.Graphics.Clear(SelectedColor);
        Controls.Add(_preview);

        _hex = new TextBox
        {
            Location = new Point(300, 224),
            Width = 96,
            Font = Theme.Ui(11),
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
        };
        _hex.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyHex(); } };
        _hex.Leave += (s, e) => ApplyHex();
        Controls.Add(_hex);

        // 下段：決定とキャンセル
        var ok = Ui.Button(Strings.T("ok"), 11, true, null, 100, 30);
        ok.Location = new Point(200, 378);
        ok.DialogResult = DialogResult.OK;
        var cancel = Ui.Button(Strings.T("cancel"), 11, true, Theme.ClearBtn, 100, 30);
        cancel.Location = new Point(308, 378);
        cancel.DialogResult = DialogResult.Cancel;
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;

        RebuildSv();
        ShowMode();
        RefreshAll();
    }

    MarqueeCheckBox ModeBox(string text, int x)
    {
        var box = new MarqueeCheckBox
        {
            Text = text,
            Font = Theme.Ui(10),
            ForeColor = Theme.Text,
            Location = new Point(x, 110),
            Size = new Size(124, 24),
        };
        Controls.Add(box);
        return box;
    }

    Label ChannelLabel(string text, int y)
    {
        var label = Ui.Label(text, 11, true);
        label.Location = new Point(20, y + 4);
        Controls.Add(label);
        return label;
    }

    NumericUpDown ChannelBox(int y)
    {
        var box = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 255,
            Location = new Point(56, y),
            Width = 80,
            Font = Theme.Ui(11),
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
        };
        Controls.Add(box);
        return box;
    }

    // 入力方式は必ず1つだけ選ばれた状態にする
    void SelectMode(MarqueeCheckBox chosen)
    {
        if (_modeBusy) return;
        _modeBusy = true;
        try
        {
            if (!chosen.Checked)
            {
                chosen.Checked = true;
            }
            else
            {
                foreach (var box in new[] { _modeHsv, _modeRgb, _modeHex })
                    if (box != chosen) box.Checked = false;
            }
            ShowMode();
        }
        finally
        {
            _modeBusy = false;
        }
    }

    // 選んだ入力方式の欄だけを表示する
    void ShowMode()
    {
        bool hsv = _modeHsv.Checked, rgb = _modeRgb.Checked, hex = _modeHex.Checked;
        _svPanel.Visible = hsv;
        _huePanel.Visible = hsv;
        _rLabel.Visible = _r.Visible = _gLabel.Visible = _g.Visible = _bLabel.Visible = _b.Visible = rgb;
        _hex.Visible = hex;
    }

    void SetColor(Color c)
    {
        ToHsv(c, out _h, out _s, out _v);
        RebuildSv();
        RefreshAll();
    }

    void PickSv(int x, int y)
    {
        x = Math.Max(0, Math.Min(Area - 1, x));
        y = Math.Max(0, Math.Min(Area - 1, y));
        _s = x / (double)(Area - 1);
        _v = 1 - y / (double)(Area - 1);
        RefreshAll();
    }

    void PickHue(int y)
    {
        y = Math.Max(0, Math.Min(Area - 1, y));
        _h = Math.Min(359.9, y / (double)(Area - 1) * 360);
        RebuildSv();
        RefreshAll();
    }

    void RgbChanged()
    {
        if (_syncing) return;
        SetColor(Color.FromArgb(0xFF, (int)_r.Value, (int)_g.Value, (int)_b.Value));
    }

    void ApplyHex()
    {
        if (_syncing) return;
        string text = _hex.Text.Trim().TrimStart('#');
        if (text.Length == 6 && int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            SetColor(Color.FromArgb(0xFF, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF));
        }
        else
        {
            RefreshAll();
        }
    }

    // 全部の表示を、今の色に合わせて更新する
    void RefreshAll()
    {
        _syncing = true;
        Color c = SelectedColor;
        _hex.Text = Hex(c);
        _r.Value = c.R;
        _g.Value = c.G;
        _b.Value = c.B;
        _syncing = false;

        _preview.Invalidate();
        _svPanel.Invalidate();
        _huePanel.Invalidate();
    }

    // 色相ごとに、彩度（横）と明度（縦）の色を作る
    void RebuildSv()
    {
        var bmp = new Bitmap(Area, Area);
        for (int y = 0; y < Area; y++)
        {
            for (int x = 0; x < Area; x++)
            {
                double s = x / (double)(Area - 1);
                double v = 1 - y / (double)(Area - 1);
                bmp.SetPixel(x, y, FromHsv(_h, s, v));
            }
        }
        _sv?.Dispose();
        _sv = bmp;
    }

    static Bitmap MakeHueBar()
    {
        var bmp = new Bitmap(24, Area);
        for (int y = 0; y < Area; y++)
        {
            Color c = FromHsv(y / (double)(Area - 1) * 360, 1, 1);
            for (int x = 0; x < 24; x++) bmp.SetPixel(x, y, c);
        }
        return bmp;
    }

    void PaintSv(Graphics g)
    {
        g.DrawImage(_sv, 0, 0);
        int cx = (int)Math.Round(_s * (Area - 1));
        int cy = (int)Math.Round((1 - _v) * (Area - 1));
        g.DrawEllipse(Pens.Black, cx - 7, cy - 7, 14, 14);
        g.DrawEllipse(Pens.White, cx - 6, cy - 6, 12, 12);
    }

    void PaintHue(Graphics g)
    {
        g.DrawImage(_hueBar, 0, 0);
        int my = (int)Math.Round(_h / 360 * (Area - 1));
        g.DrawLine(Pens.Black, 0, my, 23, my);
        g.DrawLine(Pens.White, 0, my + 1, 23, my + 1);
    }

    static string Hex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    // HSV（色相 0-360、彩度・明度 0-1）から色を作る
    static Color FromHsv(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        double c = v * s;
        double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
        double m = v - c;
        double r, g, b;
        if (h < 60) { r = c; g = x; b = 0; }
        else if (h < 120) { r = x; g = c; b = 0; }
        else if (h < 180) { r = 0; g = c; b = x; }
        else if (h < 240) { r = 0; g = x; b = c; }
        else if (h < 300) { r = x; g = 0; b = c; }
        else { r = c; g = 0; b = x; }
        return Color.FromArgb((int)Math.Round((r + m) * 255), (int)Math.Round((g + m) * 255), (int)Math.Round((b + m) * 255));
    }

    static void ToHsv(Color c, out double h, out double s, out double v)
    {
        double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
        double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
        double d = max - min;
        v = max;
        s = max == 0 ? 0 : d / max;
        if (d == 0) h = 0;
        else if (max == r) h = 60 * (((g - b) / d + 6) % 6);
        else if (max == g) h = 60 * ((b - r) / d + 2);
        else h = 60 * ((r - g) / d + 4);
    }
}
