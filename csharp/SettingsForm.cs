using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// 設定ウィンドウ。言語とテーマを切り替える。
// ShowDialog で開くので、開いている間はメインウィンドウは操作できない
sealed class SettingsForm : Form
{
    readonly MainForm _owner;
    ComboBox _langBox;
    MarqueeCheckBox _darkBox;

    public SettingsForm(MainForm owner)
    {
        _owner = owner;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(360, 336);
        HandleCreated += (s, e) => Native.SetDarkTitleBar(Handle, Theme.IsDark);
        Build();
    }

    // 言語やテーマを変えたときは、文言と配色を組み直す
    void Build()
    {
        SuspendLayout();
        var old = Controls.Cast<Control>().ToArray();
        Controls.Clear();
        foreach (var c in old) c.Dispose();

        BackColor = Theme.Window;
        Font = Theme.Ui(9);
        Text = Strings.T("settings");

        var langLabel = Ui.Label(Strings.T("settings_language"), 12, true);
        langLabel.Location = new Point(24, 28);
        Controls.Add(langLabel);

        var options = Strings.LanguageOptions();
        _langBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(10),
            Width = 180,
            Location = new Point(156, 24),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22,
        };
        _langBox.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(selected ? Theme.Track : Theme.Entry))
                e.Graphics.FillRectangle(b, e.Bounds);
            Marquee.Draw(e.Graphics, _langBox, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                (string)_langBox.Items[e.Index], Theme.Ui(10), Theme.Text, StringAlignment.Near);
        };
        foreach (var o in options) _langBox.Items.Add(o.Label);
        _langBox.SelectedItem = options.First(o => o.Code == Strings.CurrentCode()).Label;
        _langBox.SelectedIndexChanged += (s, e) =>
        {
            string label = (string)_langBox.SelectedItem;
            string code = options.First(o => o.Label == label).Code;
            BeginInvoke(new Action(() => ChangeLanguage(code)));
        };
        Controls.Add(_langBox);

        _darkBox = new MarqueeCheckBox
        {
            Text = Strings.T("dark_mode"),
            Font = Theme.Ui(12),
            ForeColor = Theme.Text,
            Location = new Point(24, 80),
            Size = new Size(312, 24),
            Checked = Theme.IsDark,
        };
        _darkBox.CheckedChanged += (s, e) => BeginInvoke(new Action(() => ChangeTheme(_darkBox.Checked)));

        var speedLabel = Ui.Label(Strings.T("scroll_speed"), 12);
        speedLabel.Location = new Point(24, 136);
        Controls.Add(speedLabel);

        var speedBox = new NumericUpDown
        {
            Minimum = 10,
            Maximum = 200,
            Increment = 10,
            Value = (decimal)Marquee.Speed,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(11),
            Location = new Point(200, 132),
            Width = 100,
        };
        speedBox.ValueChanged += (s, e) => Strings.SaveScrollSpeed((int)speedBox.Value);
        Controls.Add(speedBox);

        // スクロール速度の確認用。速度を変えると、この文字の流れ方も変わる
        var preview = new MarqueeLabel
        {
            Text = Strings.T("preview_text"),
            Font = Theme.Ui(11),
            ForeColor = Theme.Text,
            BackColor = Theme.Entry,
            Location = new Point(24, 172),
            Size = new Size(312, 28),
        };
        Controls.Add(preview);

        // アクセントカラー：システムの規定かカスタムかを選ぶ。カスタムのときは色を選ぶボタンを出す
        var accentLabel = Ui.Label(Strings.T("accent_color"), 12);
        accentLabel.Location = new Point(24, 212);
        Controls.Add(accentLabel);

        var accentBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            Font = Theme.Ui(10),
            Width = 140,
            Location = new Point(200, 208),
            DrawMode = DrawMode.OwnerDrawFixed,
            ItemHeight = 22,
        };
        accentBox.DrawItem += (s, e) =>
        {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(selected ? Theme.Track : Theme.Entry))
                e.Graphics.FillRectangle(b, e.Bounds);
            Marquee.Draw(e.Graphics, accentBox, new Rectangle(e.Bounds.X + 6, e.Bounds.Y, e.Bounds.Width - 8, e.Bounds.Height),
                (string)accentBox.Items[e.Index], Theme.Ui(10), Theme.Text, StringAlignment.Near);
        };
        accentBox.Items.Add(Strings.T("accent_system"));
        accentBox.Items.Add(Strings.T("accent_custom"));
        accentBox.SelectedIndex = Theme.CustomAccent ? 1 : 0;
        accentBox.SelectedIndexChanged += (s, e) =>
        {
            bool custom = accentBox.SelectedIndex == 1;
            if (custom == Theme.CustomAccent) return;
            BeginInvoke(new Action(() =>
            {
                if (custom) PickCustomColor();
                else ApplyAccent(false, Theme.CustomAccentColor);
            }));
        };
        Controls.Add(accentBox);

        if (Theme.CustomAccent)
        {
            var pick = Ui.Button(Strings.T("accent_pick"), 11, false, null, 140, 28);
            pick.Location = new Point(200, 248);
            pick.Click += (s, e) => BeginInvoke(new Action(PickCustomColor));
            Controls.Add(pick);
        }
        Controls.Add(_darkBox);

        var close = Ui.Button(Strings.T("close"), 12, true, null, 120, 30);
        close.Location = new Point((ClientSize.Width - 120) / 2, 292);
        close.Click += (s, e) => Close();
        Controls.Add(close);
        CancelButton = close;

        ResumeLayout(false);
    }

    // カスタム色を選ぶ。キャンセルしたときは、アクセントの選択を元に戻す
    void PickCustomColor()
    {
        using (var dlg = new ColorDialog { Color = Theme.CustomAccentColor, FullOpen = true, AnyColor = true })
        {
            if (dlg.ShowDialog(this) == DialogResult.OK) ApplyAccent(true, dlg.Color);
            else Build();
        }
    }

    void ApplyAccent(bool custom, Color color)
    {
        Strings.SaveAccent(custom, color);
        _owner.Rebuild();
        Build();
    }

    void ChangeLanguage(string code)
    {
        // 対応外の既定言語を選んだ場合などは、メイン側で確認してから切り替える
        _owner.ChangeLanguage(code);
        Build();
    }

    void ChangeTheme(bool dark)
    {
        if (Theme.IsDark != dark)
        {
            Strings.SaveTheme(dark);
            _owner.Rebuild();
            Native.SetDarkTitleBar(Handle, dark);
        }
        Build();
    }
}
