using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

// 起動用の exe。自分と同じフォルダにある AutoClicker.dll を読み込んで、本体を起動する
static class LauncherProgram
{
    [STAThread]
    static void Main()
    {
        string dll = Path.Combine(AppContext.BaseDirectory, "AutoClicker.dll");
        if (!File.Exists(dll))
        {
            MessageBox.Show(
                "AutoClicker.dll が見つかりません。\n\n" + dll + "\n\nexe と同じフォルダに AutoClicker.dll を置いてから起動してください。",
                "AutoClicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            Assembly assembly = Assembly.LoadFrom(dll);
            assembly.GetType("AutoClickerEntry").GetMethod("Run").Invoke(null, null);
        }
        catch (Exception ex)
        {
            MessageBox.Show("起動に失敗しました。\n\n" + ex.Message, "AutoClicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
