using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

// 枠に収まらない文字を流すための仕組み。
// 右から左へ流れ、左端に戻らず端で止まる。端で2秒止まり、2秒おきに流れる（ループは「1周期」単位）
static class Marquee
{
    const int HoldMs = 2000;      // 端で止まる時間
    const float SpeedPxPerSec = 40f;
    const int TickMs = 50;

    static readonly HashSet<Control> _owners = new HashSet<Control>();
    static readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();
    static readonly Timer _timer = new Timer { Interval = TickMs };

    static Marquee()
    {
        _timer.Tick += (s, e) =>
        {
            _owners.RemoveWhere(c => c.IsDisposed);
            if (_owners.Count == 0)
            {
                _timer.Stop();
                return;
            }
            foreach (var c in _owners) c.Invalidate();
        };
    }

    static void Register(Control owner)
    {
        _owners.Add(owner);
        if (!_timer.Enabled) _timer.Start();
    }

    // 現在の横方向のずれ（px）。overflow は文字の超過分
    static float Offset(float overflow)
    {
        float scrollMs = overflow / SpeedPxPerSec * 1000f;
        float cycle = HoldMs + scrollMs + HoldMs;
        float t = _clock.ElapsedMilliseconds % cycle;
        if (t < HoldMs) return 0;
        if (t < HoldMs + scrollMs) return (t - HoldMs) / 1000f * SpeedPxPerSec;
        if (t < HoldMs + scrollMs + HoldMs) return overflow;
        return 0;
    }

    // 文字を描く。枠に収まるなら align で描き、収まらないなら流す
    public static void Draw(Graphics g, Control owner, Rectangle area, string text, Font font, Color color, StringAlignment align)
    {
        text = text ?? "";
        int textWidth = TextRenderer.MeasureText(text, font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        float overflow = textWidth - area.Width;

        using (var brush = new SolidBrush(color))
        using (var sf = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap })
        {
            if (overflow <= 0)
            {
                sf.Alignment = align;
                g.DrawString(text, font, brush, area, sf);
                return;
            }

            Register(owner);
            sf.Alignment = StringAlignment.Near;
            var state = g.Save();
            g.IntersectClip(area);
            g.DrawString(text, font, brush, new RectangleF(area.X - Offset(overflow), area.Y, textWidth + 4, area.Height), sf);
            g.Restore(state);
        }
    }
}

// 文字が収まらないときだけ流すボタン。収まるときは通常のボタンと同じ見た目
sealed class MarqueeButton : Button
{
    bool _hover;

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        _hover = true;
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        int textWidth = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        if (textWidth <= ClientSize.Width - 8)
        {
            base.OnPaint(e);
            return;
        }

        var g = e.Graphics;
        g.Clear(_hover ? FlatAppearance.MouseOverBackColor : BackColor);
        Marquee.Draw(g, this, new Rectangle(4, 0, ClientSize.Width - 8, ClientSize.Height), Text, Font, ForeColor, StringAlignment.Center);
    }
}

// 枠に収まらないときだけ流すラベル
sealed class MarqueeLabel : Label
{
    protected override void OnPaint(PaintEventArgs e)
    {
        int textWidth = TextRenderer.MeasureText(Text, Font, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.SingleLine).Width;
        if (textWidth <= ClientSize.Width)
        {
            base.OnPaint(e);
            return;
        }

        e.Graphics.Clear(Parent?.BackColor ?? Theme.Window);
        Marquee.Draw(e.Graphics, this, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height), Text, Font, ForeColor, StringAlignment.Near);
    }
}
