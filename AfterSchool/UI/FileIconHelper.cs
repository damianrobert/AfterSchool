using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace AfterSchool.UI;

public static class FileIconHelper
{
    private static readonly Dictionary<string, Bitmap> _cache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, (Color bg, string label)> _extMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"]  = (Color.FromArgb(220,  38,  38), "PDF"),
            [".txt"]  = (Color.FromArgb(100, 116, 139), "TXT"),
            [".csv"]  = (Color.FromArgb( 22, 163,  74), "CSV"),
            [".doc"]  = (Color.FromArgb( 37,  99, 235), "DOC"),
            [".docx"] = (Color.FromArgb( 37,  99, 235), "DOC"),
            [".xls"]  = (Color.FromArgb( 22, 163,  74), "XLS"),
            [".xlsx"] = (Color.FromArgb( 22, 163,  74), "XLS"),
            [".ppt"]  = (Color.FromArgb(234,  88,  12), "PPT"),
            [".pptx"] = (Color.FromArgb(234,  88,  12), "PPT"),
            [".png"]  = (Color.FromArgb(139,  92, 246), "IMG"),
            [".jpg"]  = (Color.FromArgb(139,  92, 246), "IMG"),
            [".jpeg"] = (Color.FromArgb(139,  92, 246), "IMG"),
            [".gif"]  = (Color.FromArgb(139,  92, 246), "IMG"),
            [".bmp"]  = (Color.FromArgb(139,  92, 246), "IMG"),
            [".svg"]  = (Color.FromArgb(139,  92, 246), "IMG"),
            [".webp"] = (Color.FromArgb(139,  92, 246), "IMG"),
            [".mp4"]  = (Color.FromArgb(239,  68,  68), "VID"),
            [".avi"]  = (Color.FromArgb(239,  68,  68), "VID"),
            [".mov"]  = (Color.FromArgb(239,  68,  68), "VID"),
            [".mkv"]  = (Color.FromArgb(239,  68,  68), "VID"),
            [".mp3"]  = (Color.FromArgb( 20, 184, 166), "AUD"),
            [".wav"]  = (Color.FromArgb( 20, 184, 166), "AUD"),
            [".flac"] = (Color.FromArgb( 20, 184, 166), "AUD"),
            [".aac"]  = (Color.FromArgb( 20, 184, 166), "AUD"),
            [".zip"]  = (Color.FromArgb(245, 158,  11), "ZIP"),
            [".rar"]  = (Color.FromArgb(245, 158,  11), "ZIP"),
            [".7z"]   = (Color.FromArgb(245, 158,  11), "ZIP"),
            [".tar"]  = (Color.FromArgb(245, 158,  11), "ZIP"),
            [".gz"]   = (Color.FromArgb(245, 158,  11), "ZIP"),
            [".cs"]   = (Color.FromArgb( 16, 185, 129), "C#"),
            [".py"]   = (Color.FromArgb( 16, 185, 129), "PY"),
            [".js"]   = (Color.FromArgb( 16, 185, 129), "JS"),
            [".ts"]   = (Color.FromArgb( 16, 185, 129), "TS"),
            [".html"] = (Color.FromArgb( 16, 185, 129), "HTM"),
            [".json"] = (Color.FromArgb( 16, 185, 129), "JSN"),
            [".xml"]  = (Color.FromArgb( 16, 185, 129), "XML"),
        };

    // Returns a cached 28×28 badge bitmap for the given filename.
    public static Bitmap Get(string fileName)
    {
        var ext = Path.GetExtension(fileName);
        if (_cache.TryGetValue(ext, out var cached)) return cached;

        var icon = Draw(ext);
        _cache[ext] = icon;
        return icon;
    }

    private static Bitmap Draw(string ext)
    {
        const int size = 28;

        if (!_extMap.TryGetValue(ext, out var info))
        {
            var raw = ext.TrimStart('.').ToUpperInvariant();
            info = (Color.FromArgb(100, 116, 139), raw.Length > 3 ? raw[..3] : (raw.Length > 0 ? raw : "?"));
        }

        var (bg, label) = info;
        var bmp = new Bitmap(size, size);

        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode       = SmoothingMode.AntiAlias;
        g.TextRenderingHint   = TextRenderingHint.AntiAlias;

        const int r = 5;
        using var path = RoundedRect(new Rectangle(0, 0, size - 1, size - 1), r);
        g.FillPath(new SolidBrush(bg), path);

        var text     = label.Length > 3 ? label[..3] : label;
        var fontSize = text.Length <= 2 ? 8.5f : 6.5f;
        using var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
        var sf = new StringFormat
        {
            Alignment     = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString(text, font, Brushes.White, new RectangleF(0, 0, size, size), sf);

        return bmp;
    }

    private static GraphicsPath RoundedRect(Rectangle b, int r)
    {
        var p = new GraphicsPath();
        p.AddArc(b.X,             b.Y,              r * 2, r * 2, 180, 90);
        p.AddArc(b.Right - r * 2, b.Y,              r * 2, r * 2, 270, 90);
        p.AddArc(b.Right - r * 2, b.Bottom - r * 2, r * 2, r * 2,   0, 90);
        p.AddArc(b.X,             b.Bottom - r * 2, r * 2, r * 2,  90, 90);
        p.CloseFigure();
        return p;
    }
}
