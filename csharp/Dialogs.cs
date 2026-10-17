using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

// MessageBox の代わり。アプリ全体と同じフォント（Yu Gothic UI）と配色で表示する
static class Dialogs
{
    public static DialogResult Show(IWin32Window owner, string text, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
    {
        using (var dlg = new Form
        {
            Text = title,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(440, 180),
            BackColor = Theme.Frame,
            Font = Theme.Ui(10),
        })
        {
            var symbol = Ui.Label(SymbolFor(icon), 26, true, ColorFor(icon));
            dlg.Controls.Add(symbol);
            symbol.Location = new Point(24, 26);

            var message = new Label
            {
                Text = text,
                Font = Theme.Ui(11),
                ForeColor = Theme.Text,
                MaximumSize = new Size(330, 0),
                AutoSize = true,
            };
            dlg.Controls.Add(message);
            message.Location = new Point(86, 26);

            var buttonRow = new System.Collections.Generic.List<Button>();
            if (buttons == MessageBoxButtons.YesNo)
            {
                buttonRow.Add(Ui.Button(Strings.T("yes"), 11, true, null, 110, 30));
                buttonRow.Last().DialogResult = DialogResult.Yes;
                buttonRow.Add(Ui.Button(Strings.T("no"), 11, true, Theme.ClearBtn, 110, 30));
                buttonRow.Last().DialogResult = DialogResult.No;
                dlg.AcceptButton = buttonRow[0];
                dlg.CancelButton = buttonRow[1];
            }
            else
            {
                buttonRow.Add(Ui.Button(Strings.T("ok"), 11, true, null, 110, 30));
                buttonRow.Last().DialogResult = DialogResult.OK;
                dlg.AcceptButton = buttonRow[0];
            }

            int buttonY = Math.Max(message.Bottom + 20, 110);
            int totalWidth = buttonRow.Sum(b => b.Width) + 12 * (buttonRow.Count - 1);
            int x = (dlg.ClientSize.Width - totalWidth) / 2;
            foreach (var b in buttonRow)
            {
                b.Location = new Point(x, buttonY);
                dlg.Controls.Add(b);
                x += b.Width + 12;
            }

            dlg.HandleCreated += (s, e) => Native.SetDarkTitleBar(dlg.Handle, Theme.IsDark);
            return dlg.ShowDialog(owner);
        }
    }

    static string SymbolFor(MessageBoxIcon icon)
    {
        switch (icon)
        {
            case MessageBoxIcon.Warning: return "⚠";
            case MessageBoxIcon.Error: return "✖";
            default: return "ℹ";
        }
    }

    static Color ColorFor(MessageBoxIcon icon)
    {
        switch (icon)
        {
            case MessageBoxIcon.Warning: return Color.FromArgb(0xF5, 0xC5, 0x42);
            case MessageBoxIcon.Error: return Color.FromArgb(0xE0, 0x55, 0x55);
            default: return Theme.Accent;
        }
    }
}
