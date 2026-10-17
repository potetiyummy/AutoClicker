using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// チェックボックス。文字がはみ出すときは流す（Windows 標準の部品では流せないため自前で描く）
sealed class MarqueeCheckBox : Control
{
    const int BoxSize = 16;
    const int TextLeft = 24;

    bool _checked;

    public event EventHandler CheckedChanged;

    public MarqueeCheckBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
        Cursor = Cursors.Hand;
        TabStop = false;
        Size = new Size(380, 24);
    }

    public bool Checked
    {
        get => _checked;
        set
        {
            if (_checked == value) return;
            _checked = value;
            Invalidate();
            CheckedChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(Parent?.BackColor ?? Theme.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        var box = new RectangleF(1, (Height - BoxSize) / 2f, BoxSize - 2, BoxSize - 2);
        using (var path = Shape.Path(box, 4))
        {
            using (var fill = new SolidBrush(_checked ? Theme.Accent : Theme.Entry))
                g.FillPath(fill, path);
            using (var pen = new Pen(_checked ? Theme.Accent : Theme.Border))
                g.DrawPath(pen, path);
        }

        if (_checked)
        {
            using (var pen = new Pen(Theme.OnAccent, 2f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float x = box.X, y = box.Y;
                g.DrawLines(pen, new[]
                {
                    new PointF(x + 3.5f, y + 8f),
                    new PointF(x + 6.8f, y + 11.5f),
                    new PointF(x + 12f, y + 4.5f),
                });
            }
        }

        var textArea = new Rectangle(TextLeft, 0, Math.Max(0, Width - TextLeft), Height);
        Marquee.Draw(g, this, textArea, Text, Font, ForeColor, StringAlignment.Near);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) Checked = !Checked;
    }
}
