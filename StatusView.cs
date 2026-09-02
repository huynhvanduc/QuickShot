using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace QuickShot;

/// <summary>
/// Panel tự vẽ nội dung cửa sổ trạng thái (MainWindow): tiêu đề gradient,
/// danh sách phím tắt dạng pill, và trạng thái vùng đã lưu — thay cho Label
/// đơn thuần trước đây.
/// </summary>
internal sealed class StatusView : Panel
{
    private AppSettings? _settings;
    private Rectangle? _savedRegion;

    public StatusView()
    {
        DoubleBuffered = true;
        BackColor = Theme.PanelBackground;
    }

    public void Refresh(AppSettings settings, Rectangle? savedRegion)
    {
        _settings = settings;
        _savedRegion = savedRegion;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_settings == null) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using var titleFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var labelFont = new Font("Segoe UI", 9.5f);
        using var monoFont = new Font("Consolas", 9.5f, FontStyle.Bold);

        var titleRect = new RectangleF(16, 14, ClientSize.Width - 32, 26);
        using (var titleBrush = Theme.AccentBrush(titleRect))
            g.DrawString("QuickShot", titleFont, titleBrush, titleRect.Location);

        float y = 52;
        (string label, string key)[] rows =
        {
            ("Định nghĩa vùng", _settings.DefineRegionHotkey),
            ("Chụp vùng đã lưu", _settings.CaptureRegionHotkey),
            ("Chụp toàn màn hình", _settings.FullScreenHotkey),
            ("Chụp cửa sổ hiện tại", _settings.ActiveWindowHotkey),
        };

        foreach (var (label, key) in rows)
        {
            using (var textBrush = new SolidBrush(Theme.TextSecondary))
                g.DrawString(label, labelFont, textBrush, 16, y);

            var keySize = g.MeasureString(key, monoFont);
            var pillRect = new RectangleF(ClientSize.Width - keySize.Width - 30, y - 3, keySize.Width + 16, keySize.Height + 4);
            using (var pillPath = Theme.RoundedRect(pillRect, pillRect.Height / 2f))
            using (var pillBrush = new SolidBrush(Color.FromArgb(40, Theme.AccentStart)))
                g.FillPath(pillBrush, pillPath);

            using (var keyBrush = new SolidBrush(Theme.TextPrimary))
                g.DrawString(key, monoFont, keyBrush, pillRect.X + 8, pillRect.Y + 2);

            y += 30;
        }

        y += 8;
        using (var linePen = new Pen(Color.FromArgb(35, Theme.TextSecondary.R, Theme.TextSecondary.G, Theme.TextSecondary.B)))
            g.DrawLine(linePen, 16, y, ClientSize.Width - 16, y);
        y += 16;

        bool hasRegion = _savedRegion.HasValue;
        string statusText = hasRegion
            ? $"Đã lưu vùng: {_savedRegion!.Value.Width} x {_savedRegion.Value.Height}"
            : "Chưa định nghĩa vùng";

        var dotColor = hasRegion ? Color.FromArgb(74, 222, 128) : Color.FromArgb(148, 152, 164);
        using (var dotBrush = new SolidBrush(dotColor))
            g.FillEllipse(dotBrush, 16, y + 3, 8, 8);

        using (var statusBrush = new SolidBrush(Theme.TextPrimary))
            g.DrawString(statusText, labelFont, statusBrush, 32, y);
    }
}
