using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

// 起動用の exe。自分と同じフォルダにある AutoClicker.dll を読み込んで、本体を起動する
static class LauncherProgram
{
    static readonly string LogPath = Path.Combine(Path.GetTempPath(), "autoclicker_launch_error.log");

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
            Assembly assembly = Assembly.LoadFile(dll);
            assembly.GetType("AutoClickerEntry").GetMethod("Run").Invoke(null, null);
        }
        catch (Exception ex)
        {
            // 原因を調べられるように、詳しい内容をログに残す
            try
            {
                File.WriteAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + ex.ToString());
            }
            catch (Exception)
            {
            }
            MessageBox.Show("起動に失敗しました。\n\n" + ex.Message + "\n\n詳細：" + LogPath, "AutoClicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
