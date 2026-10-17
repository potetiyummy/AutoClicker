// autoclicker.exe の起動ランチャー。
// Python 本体は同梱しない。exe と同じフォルダの main.py を pythonw で起動する。
// Python が無い場合は確認のうえ、python.org の 3.13 系最新版を DL してインストールする。
//
// ビルド方法は build_exe.bat を参照。
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Forms;

static class Launcher
{
    const string PythonIndex = "https://www.python.org/ftp/python/";
    const string DefaultPythonDir = @"%LOCALAPPDATA%\Programs\Python\Python313";

    [STAThread]
    static void Main()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string script = Path.Combine(baseDir, "main.py");

        if (!File.Exists(script))
        {
            MessageBox.Show("main.py が見つかりません。\n" + baseDir, "autoclicker",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string pythonw = FindPythonw();
        if (pythonw == null)
        {
            var answer = MessageBox.Show(
                "Python が見つかりません。\n\n" +
                "python.org の Python 3.13 系最新版をダウンロードしてインストールしますか？\n" +
                "（インストール後、pynput も自動で導入します）",
                "autoclicker", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (answer != DialogResult.Yes)
                return;

            try
            {
                pythonw = InstallPython();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Python のインストールに失敗しました。\n\n" + ex.Message,
                    "autoclicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (pythonw == null)
            {
                MessageBox.Show("Python のインストールが完了しませんでした。",
                    "autoclicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = pythonw,
                Arguments = "\"" + script + "\"",
                WorkingDirectory = baseDir,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show("起動に失敗しました。\n\n" + ex.Message,
                "autoclicker", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // 実行可能な Python（任意のバージョン）を探す。pynput が無ければ入れてから採用する。
    // 見つからなければ null を返す
    static string FindPythonw()
    {
        var candidates = new System.Collections.Generic.List<string> { "pythonw" };
        foreach (string root in new[]
        {
            Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\Python"),
            Environment.ExpandEnvironmentVariables(@"%ProgramFiles%"),
        })
        {
            if (!Directory.Exists(root))
                continue;
            foreach (string dir in Directory.GetDirectories(root, "Python3*"))
            {
                string candidate = Path.Combine(dir, "pythonw.exe");
                if (File.Exists(candidate))
                    candidates.Add(candidate);
            }
        }

        foreach (string pythonw in candidates)
        {
            string python = pythonw == "pythonw"
                ? "python"
                : Path.Combine(Path.GetDirectoryName(pythonw), "python.exe");

            if (!IsRunnable(python, "-c \"\""))
                continue;

            if (IsRunnable(python, "-c \"import pynput\""))
                return pythonw;

            // Python は動くが pynput が無い場合は、この環境に入れてから使う
            if (RunAndWait(python, "-m pip install --user pynput") == 0 &&
                IsRunnable(python, "-c \"import pynput\""))
                return pythonw;
        }
        return null;
    }

    static bool IsRunnable(string exe, string args)
    {
        return RunAndWait(exe, args, 10000) == 0;
    }

    // 終了コードを返す。起動できなければ -1
    static int RunAndWait(string exe, string args, int timeoutMs = -1)
    {
        try
        {
            using (var p = Process.Start(new ProcessStartInfo(exe, args)
            {
                UseShellExecute = false,
                CreateNoWindow = true
            }))
            {
                if (timeoutMs < 0)
                    p.WaitForExit();
                else if (!p.WaitForExit(timeoutMs))
                {
                    p.Kill();
                    return -1;
                }
                return p.ExitCode;
            }
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return -1;
        }
    }

    // python.org の一覧から 3.13.x の最新版を選び、インストーラを DL して実行する
    static string InstallPython()
    {
        ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
        var client = new WebClient();

        string index = client.DownloadString(PythonIndex);
        var versions = Regex.Matches(index, @"href=""(3\.13\.\d+)/""")
            .Cast<Match>()
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .OrderBy(v => new Version(v))
            .ToList();
        if (versions.Count == 0)
            throw new InvalidOperationException("3.13 系のバージョン一覧を取得できませんでした。");

        string version = versions.Last();
        string url = PythonIndex + version + "/python-" + version + "-amd64.exe";
        string installer = Path.Combine(Path.GetTempPath(), "python-" + version + "-amd64.exe");

        client.DownloadFile(url, installer);

        // 現在のユーザー専用・PATH 追加・ユーザーの確認なしで静かにインストール
        var setup = Process.Start(new ProcessStartInfo(installer,
            "/passive InstallAllUsers=0 PrependPath=1 Include_launcher=0 Include_test=0")
        {
            UseShellExecute = false
        });
        setup.WaitForExit();
        if (setup.ExitCode != 0)
            throw new InvalidOperationException("インストーラが終了コード " + setup.ExitCode + " で終了しました。");

        string pythonw = Environment.ExpandEnvironmentVariables(
            Path.Combine(DefaultPythonDir, "pythonw.exe"));
        string python = Environment.ExpandEnvironmentVariables(
            Path.Combine(DefaultPythonDir, "python.exe"));
        if (!File.Exists(pythonw))
            return null;

        // 必要ライブラリを導入
        var pip = Process.Start(new ProcessStartInfo(python, "-m pip install --user pynput")
        {
            UseShellExecute = false,
            CreateNoWindow = true
        });
        pip.WaitForExit();
        if (pip.ExitCode != 0)
            throw new InvalidOperationException("pynput のインストールに失敗しました。");

        return pythonw;
    }
}
