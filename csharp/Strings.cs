using System.Drawing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

// i18n.py の移植。languages/*.lang（埋め込みリソース）と config.py（exe の隣）を扱う
static class Strings
{
    public const string DefaultLang = "en-US";
    public const string SystemValue = "system";

    public static readonly string[] SupportedLangs = { "ja-JP", "en-US", "ko-KR", "zh-CN", "zh-TW", "es-ES", "de-DE", "fr-FR", "pt-BR", "ru-RU", "it-IT", "nl-NL", "pl-PL", "ar-SA", "hi-IN", "id-ID", "tr-TR", "vi-VN", "th-TH", "ms-MY", "sv-SE", "da-DK", "nb-NO", "fi-FI", "cs-CZ", "hu-HU", "el-GR", "uk-UA", "ro-RO", "ja-HR", "ja-KS" };

    // メニューに表示する言語名（各言語の表記のまま）
    static readonly (string Label, string Code)[] LanguageNames =
    {
        ("日本語", "ja-JP"), ("にほんご", "ja-HR"), ("日本語（関西弁）", "ja-KS"), ("English", "en-US"), ("한국어", "ko-KR"), ("简体中文", "zh-CN"), ("繁體中文", "zh-TW"),
        ("Español", "es-ES"), ("Deutsch", "de-DE"), ("Français", "fr-FR"), ("Português", "pt-BR"), ("Русский", "ru-RU"),
        ("Italiano", "it-IT"), ("Nederlands", "nl-NL"), ("Polski", "pl-PL"), ("العربية", "ar-SA"), ("हिन्दी", "hi-IN"),
        ("Bahasa Indonesia", "id-ID"), ("Türkçe", "tr-TR"), ("Tiếng Việt", "vi-VN"), ("ไทย", "th-TH"), ("Bahasa Melayu", "ms-MY"),
        ("Svenska", "sv-SE"), ("Dansk", "da-DK"), ("Norsk bokmål", "nb-NO"), ("Suomi", "fi-FI"), ("Čeština", "cs-CZ"),
        ("Magyar", "hu-HU"), ("Ελληνικά", "el-GR"), ("Українська", "uk-UA"), ("Română", "ro-RO"),
    };

    // 日本語版で保存された .acf の値を内部キーに変換する表
    public static readonly Dictionary<string, string> LegacyValues = new Dictionary<string, string>
    {
        ["左クリック"] = "left_click", ["右クリック"] = "right_click",
        ["左ドラッグ"] = "left_drag", ["右ドラッグ"] = "right_drag",
        ["左長押し"] = "left_hold", ["右長押し"] = "right_hold",
        ["入力モード"] = "text", ["ホットキー"] = "hotkey",
    };

    static Dictionary<string, string> _fallback;
    static Dictionary<string, string> _table;
    static readonly Dictionary<string, Dictionary<string, string>> _tables = new Dictionary<string, Dictionary<string, string>>();

    public static string Lang { get; private set; }
    public static bool SystemMode { get; private set; }
    // 設定ファイルは TEMP フォルダ内の autoclicker フォルダに保存する
    public static string ConfigPath => Path.Combine(Path.GetTempPath(), "autoclicker", "config.py");

    public static void Init()
    {
        _fallback = LoadTable(DefaultLang);
        string saved = ReadSavedLanguage();
        SystemMode = saved == null || !SupportedLangs.Contains(saved);
        Lang = SystemMode ? (SystemLang() ?? DefaultLang) : saved;
        _table = LoadTable(Lang);
        Theme.IsDark = ReadConfigValue("THEME") == "dark";
        Marquee.Speed = ReadScrollSpeed();
        LoadAccent();
        LoadExecution();
    }

    // Windows の表示言語に対応する言語コード。対応していない場合は null
    public static string SystemLang()
    {
        try
        {
            ushort langid = Native.GetUserDefaultUILanguage();
            int primary = langid & 0x3FF, sub = langid >> 10;
            if (primary == 0x11) return "ja-JP";
            if (primary == 0x09) return "en-US";
            if (primary == 0x12) return "ko-KR";
            if (primary == 0x04) return (sub == 2 || sub == 4) ? "zh-CN" : "zh-TW";
            if (primary == 0x0A) return "es-ES";
            if (primary == 0x07) return "de-DE";
            if (primary == 0x0C) return "fr-FR";
            if (primary == 0x16) return "pt-BR";
            if (primary == 0x19) return "ru-RU";
            if (primary == 0x10) return "it-IT";
            if (primary == 0x13) return "nl-NL";
            if (primary == 0x15) return "pl-PL";
            if (primary == 0x01) return "ar-SA";
            if (primary == 0x39) return "hi-IN";
            if (primary == 0x21) return "id-ID";
            if (primary == 0x1F) return "tr-TR";
            if (primary == 0x2A) return "vi-VN";
            if (primary == 0x1E) return "th-TH";
            if (primary == 0x3E) return "ms-MY";
            if (primary == 0x1D) return "sv-SE";
            if (primary == 0x06) return "da-DK";
            if (primary == 0x14) return "nb-NO";
            if (primary == 0x0B) return "fi-FI";
            if (primary == 0x05) return "cs-CZ";
            if (primary == 0x0E) return "hu-HU";
            if (primary == 0x08) return "el-GR";
            if (primary == 0x22) return "uk-UA";
            if (primary == 0x18) return "ro-RO";
        }
        catch (Exception)
        {
        }
        return null;
    }

    static Dictionary<string, string> LoadTable(string lang)
    {
        var table = new Dictionary<string, string>();
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("languages." + lang + ".lang"))
        {
            if (s == null) return table;
            using (var reader = new StreamReader(s, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || !line.Contains("=")) continue;
                    int idx = line.IndexOf('=');
                    table[line.Substring(0, idx).Trim()] = line.Substring(idx + 1).Trim().Replace("\\n", "\n");
                }
            }
        }
        return table;
    }

    // config.py の「NAME = 値」を読む。文字列は引用符を外して返す。無い場合や None は null
    static string ReadConfigValue(string name)
    {
        try
        {
            if (!File.Exists(ConfigPath)) return null;
            var m = Regex.Match(File.ReadAllText(ConfigPath, Encoding.UTF8), "^" + name + @" = (.*)$", RegexOptions.Multiline);
            if (!m.Success) return null;
            var q = Regex.Match(m.Groups[1].Value.Trim(), "^\"(.*)\"$");
            return q.Success ? q.Groups[1].Value : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    // config.py の「NAME = 値」を書く。rawValue が null の場合は行ごと削除する（既定値のときに容量を節約するため）
    static void WriteConfigValue(string name, string rawValue)
    {
        string text = File.Exists(ConfigPath) ? File.ReadAllText(ConfigPath, Encoding.UTF8) : "";
        var rx = new Regex("^" + name + @" = .*\r?\n?", RegexOptions.Multiline);
        string newText;
        if (rawValue == null)
        {
            newText = rx.Replace(text, "");
        }
        else
        {
            string line = $"{name} = {rawValue}";
            if (rx.IsMatch(text))
                newText = rx.Replace(text, line + "\n", 1);
            else
                newText = (text.Length > 0 ? text.TrimEnd('\n') + "\n" : "") + line + "\n";
        }
        Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
        File.WriteAllText(ConfigPath, newText, new UTF8Encoding(false));
    }

    static string ReadSavedLanguage() => ReadConfigValue("LANGUAGE");

    // 選択された言語を config.py の LANGUAGE に書き込む（null はシステムの規定）
    static void SaveLanguage(string lang)
    {
        WriteConfigValue("LANGUAGE", lang == null ? "None" : $"\"{lang}\"");
    }

    // スクロール速度（px/秒）。既定は 40。10〜200 の範囲に収める
    const int DefaultScrollSpeed = 40;

    public static int ReadScrollSpeed()
    {
        return int.TryParse(ReadConfigValue("SCROLL_SPEED"), out int speed)
            ? Math.Max(10, Math.Min(200, speed))
            : DefaultScrollSpeed;
    }

    // 既定の速度のときは設定行を消す（ダークモードと同じ扱い）
    public static void SaveScrollSpeed(int speed)
    {
        Marquee.Speed = speed;
        WriteConfigValue("SCROLL_SPEED", speed == DefaultScrollSpeed ? null : $"\"{speed}\"");
    }

    // アクセントカラーを設定ファイルから読む。ACCENT_MODE が custom のときだけカスタム色を使う
    public static void LoadAccent()
    {
        Theme.CustomAccent = ReadConfigValue("ACCENT_MODE") == "custom";
        string hex = ReadConfigValue("ACCENT_COLOR");
        if (hex != null && hex.Length == 7 && hex[0] == '#')
        {
            try
            {
                Theme.CustomAccentColor = ColorTranslator.FromHtml(hex);
            }
            catch (Exception)
            {
            }
        }
    }

    // システムの規定のときは設定行を消す（ダークモードなどと同じ扱い）
    public static void SaveAccent(bool custom, Color color)
    {
        Theme.CustomAccent = custom;
        Theme.CustomAccentColor = color;
        WriteConfigValue("ACCENT_MODE", custom ? "\"custom\"" : null);
        WriteConfigValue("ACCENT_COLOR", custom ? $"\"#{color.R:X2}{color.G:X2}{color.B:X2}\"" : null);
    }

    // 実行速度（%）。既定は 100。50〜200 の範囲
    const int DefaultSpeedPercent = 100;

    public static int SpeedPercent { get; private set; } = DefaultSpeedPercent;

    // 開始・停止のキー（仮想キーコード）。既定は F8（119）
    const int DefaultStartKey = 0x77;

    public static int StartKey { get; private set; } = DefaultStartKey;

    static void LoadExecution()
    {
        SpeedPercent = int.TryParse(ReadConfigValue("SPEED_PERCENT"), out int p) ? Math.Max(50, Math.Min(200, p)) : DefaultSpeedPercent;
        MacroEngine.SpeedFactor = SpeedPercent / 100.0;
        StartKey = int.TryParse(ReadConfigValue("START_KEY"), out int k) && k > 0 && k < 256 ? k : DefaultStartKey;
    }

    // 既定の値のときは設定行を消す（ダークモードなどと同じ扱い）
    public static void SaveSpeedPercent(int percent)
    {
        SpeedPercent = percent;
        MacroEngine.SpeedFactor = percent / 100.0;
        WriteConfigValue("SPEED_PERCENT", percent == DefaultSpeedPercent ? null : $"\"{percent}\"");
    }

    public static void SaveStartKey(int vk)
    {
        StartKey = vk;
        WriteConfigValue("START_KEY", vk == DefaultStartKey ? null : $"\"{vk}\"");
    }

    // ダークテーマを保存する。ライト（既定）のときは設定行を消す
    public static void SaveTheme(bool dark)
    {
        Theme.IsDark = dark;
        WriteConfigValue("THEME", dark ? "\"dark\"" : null);
    }

    static void SetLanguage(string lang)
    {
        Lang = lang;
        _table = LoadTable(lang);
    }

    // 選択を反映して保存する。choice は言語コードか SystemValue
    public static void ApplyLanguage(string choice)
    {
        if (choice == SystemValue)
        {
            SystemMode = true;
            SaveLanguage(null);
            SetLanguage(SystemLang() ?? DefaultLang);
        }
        else
        {
            SystemMode = false;
            SaveLanguage(choice);
            SetLanguage(choice);
        }
    }

    // 言語メニューの項目 (表示名, 言語コード)。先頭はシステムの規定
    public static List<(string Label, string Code)> LanguageOptions()
    {
        var list = new List<(string, string)> { (T("system_default"), SystemValue) };
        list.AddRange(LanguageNames);
        return list;
    }

    public static string CurrentCode() => SystemMode ? SystemValue : Lang;

    public static string T(string key, params (string Name, object Value)[] args)
    {
        return Format(Lookup(_table, key), args);
    }

    // 指定した言語で文言を返す（現在の表示言語とは無関係に使う）
    public static string TIn(string lang, string key, params (string Name, object Value)[] args)
    {
        if (!_tables.TryGetValue(lang, out var table))
        {
            table = LoadTable(lang);
            _tables[lang] = table;
        }
        return Format(Lookup(table, key), args);
    }

    static string Lookup(Dictionary<string, string> table, string key)
    {
        if (table.TryGetValue(key, out var text) && !string.IsNullOrEmpty(text)) return text;
        if (_fallback.TryGetValue(key, out text) && !string.IsNullOrEmpty(text)) return text;
        return key;
    }

    static string Format(string text, (string Name, object Value)[] args)
    {
        foreach (var (name, value) in args)
            text = text.Replace("{" + name + "}", Convert.ToString(value));
        return text;
    }
}
