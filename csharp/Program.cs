using System;
using System.IO;
using System.Windows.Forms;

static class Program
{
    static readonly string LogPath = Path.Combine(Path.GetTempPath(), "autoclicker_error.log");

    [STAThread]
    static void Main()
    {
        // 予期しない例外はログに残す（落ちた原因を後から確認できるように）
        Application.ThreadException += (s, e) => Log(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (s, e) => Log(e.ExceptionObject);

        Strings.Init();
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    static void Log(object error)
    {
        try
        {
            File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + error + Environment.NewLine + Environment.NewLine);
        }
        catch (Exception)
        {
        }
    }
}
