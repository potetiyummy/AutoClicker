using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// アクションの内容を編集するウィンドウ。種類に合う入力欄だけを出す（入力欄は ActionFields）
sealed class EditActionForm : Form
{
    readonly MacroAction _source;
    readonly ActionFields _fields;
    readonly Panel _host;

    public MacroAction Result { get; private set; }

    public EditActionForm(MacroAction source, List<MacroAction> actions)
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

        _host = new Panel { Location = new Point(16, 16), Size = new Size(430, 300), BackColor = Theme.Window };
        Controls.Add(_host);
        _fields = new ActionFields(_host, () => ActionFields.ChoicesFrom(actions), () => ActionFields.VarNamesFrom(actions));
        _fields.Show(source.Type, source);

        int bottom = _fields.ContentHeight + 16;
        _host.Size = new Size(430, bottom);

        var ok = Ui.Button(Strings.T("ok"), 11, true, null, 100, 30);
        ok.Location = new Point(150, bottom + 16);
        ok.Click += (s, e) =>
        {
            Result = _fields.Read(_source);
            DialogResult = DialogResult.OK;
        };
        var cancel = Ui.Button(Strings.T("cancel"), 11, true, Theme.ClearBtn, 100, 30);
        cancel.Location = new Point(258, bottom + 16);
        cancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        Controls.Add(ok);
        Controls.Add(cancel);
        AcceptButton = ok;
        CancelButton = cancel;
        ClientSize = new Size(462, bottom + 62);
    }
}
