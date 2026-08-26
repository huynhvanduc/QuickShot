using System.Drawing.Drawing2D;

namespace QuickShot;

/// <summary>
/// Overlay phủ TOÀN BỘ các màn hình (virtual screen), mờ đi, cho user kéo
/// chuột chọn một khung chữ nhật. Trả về Rectangle theo tọa độ màn hình thật.
/// </summary>
public sealed class RegionSelector : Form
{
    private Point _start;
    private Rectangle _selection;
    private bool _dragging;

    // Kết quả: null nếu user hủy (Esc / click không kéo)
    public Rectangle? Result { get; private set; }

    public RegionSelector()
    {
        // Phủ hết mọi màn hình, kể cả tọa độ âm (màn bên trái màn chính)
        var vs = SystemInformation.VirtualScreen;
        StartPosition = FormStartPosition.Manual;
        Bounds = vs;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        Opacity = 0.30;              // lớp mờ
        BackColor = Color.Black;
        Cursor = Cursors.Cross;
        DoubleBuffered = true;

        KeyDown += (_, e) => { if (e.KeyCode == Keys.Escape) { Result = null; Close(); } };
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
    }

    private void OnMouseDown(object? s, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        _dragging = true;
        _start = e.Location;
        _selection = new Rectangle(e.Location, Size.Empty);
    }

    private void OnMouseMove(object? s, MouseEventArgs e)
    {
        if (!_dragging) return;
        // Chuẩn hóa để kéo được theo mọi hướng
        _selection = Rectangle.FromLTRB(
            Math.Min(_start.X, e.X), Math.Min(_start.Y, e.Y),
            Math.Max(_start.X, e.X), Math.Max(_start.Y, e.Y));
        Invalidate();
    }

    private void OnMouseUp(object? s, MouseEventArgs e)
    {
        if (!_dragging) return;
        _dragging = false;

        if (_selection.Width < 2 || _selection.Height < 2)
        {
            Result = null;   // click hụt, coi như hủy
        }
        else
        {
            // Đổi từ tọa độ client (trong form) sang tọa độ màn hình thật
            var screenPt = PointToScreen(_selection.Location);
            Result = new Rectangle(screenPt, _selection.Size);
        }
        Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (_selection.Width <= 0 || _selection.Height <= 0) return;

        // "Khoét" vùng chọn cho sáng rõ + viền
        using var brush = new SolidBrush(Color.FromArgb(60, Color.DeepSkyBlue));
        e.Graphics.FillRectangle(brush, _selection);

        using var pen = new Pen(Color.DeepSkyBlue, 2) { DashStyle = DashStyle.Dash };
        e.Graphics.DrawRectangle(pen, _selection);

        // Hiện kích thước đang kéo
        string label = $"{_selection.Width} x {_selection.Height}";
        using var font = new Font("Segoe UI", 10, FontStyle.Bold);
        e.Graphics.DrawString(label, font, Brushes.White,
            _selection.X, Math.Max(0, _selection.Y - 22));
    }

    // Ẩn khỏi Alt+Tab
    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_TOOLWINDOW = 0x80;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW;
            return cp;
        }
    }
}
