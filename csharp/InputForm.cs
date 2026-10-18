using System.Drawing;
using System.Windows.Forms;

// 実行中に出す入力ダイアログ。OK なら入力された文字、キャンセルなら null を返す
sealed class InputForm : Form
{
    readonly TextBox _box;

    InputForm(string prompt, bool multiline, string defaultText)
    {
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(420, multiline ? 260 : 150);
        BackColor = Theme.Window;
        Font = Theme.Ui(10);
        Text = Strings.T("input_title");
        HandleCreated += (s, e) => Native.SetDarkTitleBar(Handle, Theme.IsDark);

        var label = Ui.Label(prompt, 11);
        label.Location = new Point(20, 18);
        Controls.Add(label);

        _box = new TextBox
        {
            Text = defaultText,
            Multiline = multiline,
            BackColor = Theme.Entry,
            ForeColor = Theme.Text,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Theme.Ui(12),
            Location = new Point(20, 50),
            Size = new Size(380, multiline ? 150 : 28),
        };
        Controls.Add(_box);

        var ok = Ui.Button(Strings.T("ok"), 11, true, null, 100, 30);
        ok.Location = new Point(200, ClientSize.Height - 46);
        ok.DialogResult = DialogResult.OK;
        var cancel = Ui.Button(Strings.T("cancel"), 11, true, Theme.ClearBtn, 100, 30);
        cancel.Location = new Point(308, ClientSize.Height - 46);
        cancel.DialogResult = DialogResult.Cancel;
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = multiline ? null : ok;
        CancelButton = cancel;
    }

    // 入力を求める。OK なら入力された文字、キャンセルなら null
    public static string Ask(IWin32Window owner, string prompt, bool multiline, string defaultText)
    {
        using (var dlg = new InputForm(prompt, multiline, defaultText))
        {
            return dlg.ShowDialog(owner) == DialogResult.OK ? dlg._box.Text : null;
        }
    }
}
