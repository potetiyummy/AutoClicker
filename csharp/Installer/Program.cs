using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

// インストーラー。Program Files\AutoClicker\dll に本体の dll を、Program Files\AutoClicker に exe を配置する
static class InstallerProgram
{
    [STAThread]
    static void Main()
    {
        string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "AutoClicker");
        string dllDir = Path.Combine(root, "dll");
        try
        {
            Directory.CreateDirectory(dllDir);
            WriteResource("AutoClicker.dll", Path.Combine(dllDir, "AutoClicker.dll"));
            WriteResource("autoclicker.exe", Path.Combine(root, "autoclicker.exe"));
            MessageBox.Show("インストールが完了しました。\n\n" + root, "AutoClicker", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("インストールに失敗しました。\n\n" + ex.Message, "AutoClicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    static void WriteResource(string name, string destination)
    {
        using (Stream source = Assembly.GetExecutingAssembly().GetManifestResourceStream(name))
        using (FileStream target = File.Create(destination))
        {
            source.CopyTo(target);
        }
    }
}
