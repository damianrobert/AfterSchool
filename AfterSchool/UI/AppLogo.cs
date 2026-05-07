using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace AfterSchool.UI;

public static class AppLogo
{
    private static readonly Color GradTop = Color.FromArgb(37, 99, 235);
    private static readonly Color GradBot = Color.FromArgb(29, 78, 216);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Bitmap CreateBadgeBitmap(int size)
    {
        var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        g.Clear(Color.Transparent);
        DrawBadge(g, size, 0, 0);
        return bmp;
    }

    public static Icon CreateWindowIcon()
    {
        using var bmp = CreateBadgeBitmap(32);
        var hIcon = bmp.GetHicon();
        try { return (Icon)Icon.FromHandle(hIcon).Clone(); }
        finally { DestroyIcon(hIcon); }
    }

    // Draw the logo badge at (x, y) with the given size.
    public static void DrawBadge(Graphics g, int size, int x, int y)
    {
        float radius = size * 0.2f;
        var rect = new RectangleF(x, y, size, size);

        // Rounded rectangle background with gradient
        using var path = RoundedPath(rect, radius);
        using var fill = new LinearGradientBrush(
            new PointF(x, y), new PointF(x, y + size), GradTop, GradBot);
        g.FillPath(fill, path);

        // Bold "A" centred, shifted slightly upward
        float fontSize = size * 0.52f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);
        var sz = g.MeasureString("A", font);
        float lx = x + (size - sz.Width) / 2f;
        float ly = y + (size - sz.Height) / 2f - size * 0.06f;
        using var white = new SolidBrush(Color.White);
        g.DrawString("A", font, white, lx, ly);

        // Three small dots below the letter (decorative)
        float dot = size * 0.075f;
        float dotY = y + size * 0.745f;
        float gap = size * 0.10f;
        float totalW = 3 * dot + 2 * gap;
        float startX = x + (size - totalW) / 2f;
        for (int i = 0; i < 3; i++)
            g.FillEllipse(white, startX + i * (dot + gap), dotY, dot, dot);
    }

    // Draw the full horizontal logo (badge + wordmark) into the given rectangle.
    public static void DrawHorizontalLogo(Graphics g, int badgeSize, int x, int y, int totalHeight)
    {
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;

        int badgeY = y + (totalHeight - badgeSize) / 2;
        DrawBadge(g, badgeSize, x, badgeY);

        using var font = new Font("Segoe UI Semibold", badgeSize * 0.33f, GraphicsUnit.Pixel);
        using var brush = new SolidBrush(Color.White);
        var sz = g.MeasureString("AfterSchool", font);
        float textY = y + (totalHeight - sz.Height) / 2f;
        g.DrawString("AfterSchool", font, brush, x + badgeSize + 10, textY);
    }

    private static GraphicsPath RoundedPath(RectangleF r, float radius)
    {
        var path = new GraphicsPath();
        float d = radius * 2;
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
