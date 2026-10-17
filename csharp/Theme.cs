using System;
using System.Drawing;
using Microsoft.Win32;

// 配色とフォント。既定はライト。IsDark を切り替えたら画面を組み直す
static class Theme
{
    public static bool IsDark { get; set; }
    // アクセントカラー。既定はシステムの規定（Windows のアクセント色）。カスタムのときは選んだ色を使う
    public static bool CustomAccent { get; set; }
    public static Color CustomAccentColor { get; set; } = Color.FromArgb(0x1F, 0x6A, 0xA5);

    static Color? _systemAccent;

    static Color SystemAccent()
    {
        if (_systemAccent == null)
        {
            Color c = Color.FromArgb(0x1F, 0x6A, 0xA5);
            try
            {
                // Windows のアクセント色は ABGR の順で保存されている
                if (Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\DWM", "AccentColor", null) is int abgr)
                    c = Color.FromArgb(0xFF, abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);
            }
            catch (Exception)
            {
            }
            _systemAccent = c;
        }
        return _systemAccent.Value;
    }

    public static Color Accent => CustomAccent ? CustomAccentColor : SystemAccent();

    static float Luminance(Color c) => (0.299f * c.R + 0.587f * c.G + 0.114f * c.B) / 255f;

    static Color Darken(Color c, float factor) =>
        Color.FromArgb(c.A, (int)(c.R * factor), (int)(c.G * factor), (int)(c.B * factor));


    static Color Pick(int dark, int light)
    {
        int rgb = IsDark ? dark : light;
        return Color.FromArgb(0xFF, (rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
    }

    public static Color Window => Pick(0x333333, 0xF3F3F3);
    public static Color Frame => Pick(0x3C3C3C, 0xFFFFFF);
    public static Color AccentHover => Darken(Accent, 0.8f);
    public static Color Text => Pick(0xDCE4EE, 0x1A1A1A);
    public static Color OnAccent => Luminance(Accent) > 0.6f ? Color.Black : Color.White;
    public static Color Track => Pick(0x6A7077, 0xD0D5DB);
    public static Color Entry => Pick(0x4B4B4B, 0xEEF1F4);
    public static Color Border => Pick(0x5A5A5A, 0xC4CAD1);
    public static Color Gray => Pick(0xBEBEBE, 0x6B6B6B);
    public static Color Dark => Pick(0x2C2C2C, 0xF0F2F4);
    public static Color PickBtn => Pick(0x505050, 0xDDE1E6);
    public static Color ClearBtn => Pick(0x6A6A6A, 0x8A9099);
    public static Color Countdown => Color.FromArgb(0xA8, 0x00, 0x00);
    public static Color Green => Color.FromArgb(0x10, 0x7C, 0x41);
    public static Color Guide => Pick(0xA8A8A8, 0x6B6B6B);
    public static Color Item => Pick(0x444444, 0xFFFFFF);
    public static Color ItemDragging => Pick(0x505050, 0xE3EEF9);
    public static Color WaypointItem => Pick(0x3C3C3C, 0xFFFFFF);

    public static Font Ui(float size, bool bold = false) =>
        new Font("Yu Gothic UI", size, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Point);
}
